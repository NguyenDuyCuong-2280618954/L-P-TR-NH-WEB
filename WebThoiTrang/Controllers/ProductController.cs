using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebThoiTrang; // Thêm dòng này để import DbContext
using WebThoiTrang.Models;
using WebThoiTrang.Models.Entity;
using WebThoiTrang.Repositories.Interface;

namespace WebThoiTrang.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly ApplicationDbContext _dbContext; // Thêm injection này
        private const int PageSize = 20;

        public ProductController(IProductRepository productRepo, ICategoryRepository categoryRepository, ApplicationDbContext dbContext) // Cập nhật constructor
        {
            _productRepo = productRepo;
            _categoryRepo = categoryRepository;
            _dbContext = dbContext; // Gán DbContext
        }

        /*--------------------------------------------------------------
         * 1. GET /Product/Detail/{id}
         ----------------------------------------------------------------*/
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var viewModel = await BuildProductViewModel(id);
            if (viewModel == null) return NotFound();

            return View(viewModel);      // trả về Views/Product/Detail.cshtml
        }

        /*--------------------------------------------------------------
         * 2. POST /Product/Detail  (nhận productId qua form)
         ----------------------------------------------------------------*/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(int productId, int quantity = 1, string? size = null)
        {
            var viewModel = await BuildProductViewModel(productId);
            if (viewModel == null) return NotFound();

            // Ghi lại lựa chọn của user (nếu muốn render lại size/qty đã chọn)
            viewModel.SelectedQuantity = quantity;
            viewModel.SelectedSize = size ?? string.Empty;

            return View(viewModel);
        }

        /*--------------------------------------------------------------
         * 3. Hàm dựng ViewModel (dùng lại cho GET & POST)
         ----------------------------------------------------------------*/
        private async Task<ProductViewModel?> BuildProductViewModel(int id)
        {
            // Lấy sản phẩm + ảnh + danh mục
            var product = await _productRepo.GetQueryable()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return null;

            // Sản phẩm liên quan (cùng Category, loại trừ chính nó)
            var related = await _productRepo.GetQueryable()
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .Take(4)
                .ToListAsync();

            return new ProductViewModel
            {
                Product = product,
                RelatedProducts = related
            };
        }


        /*--------------------------------------------------------------
         * GET  /Product       ?categoryId=2&sortOrder=price-desc&page=1
         ----------------------------------------------------------------*/
        public async Task<IActionResult> Index(int? categoryId,
                                                string? sortOrder,
                                                string? priceRange,
                                                string? search,          // 👈 Thêm tham số tìm kiếm
                                                int page = 1)
        {
            // 1️⃣ Danh mục cho combobox
            var categories = await _categoryRepo.GetAllAsync();

            // 2️⃣ Truy vấn sản phẩm
            var query = _productRepo.GetQueryable()
                                           .Include(p => p.Category)
                                           .Where(p => p.IsAvailable); // Chỉ lấy sản phẩm còn bán

            // 🔍 Tìm kiếm theo tên sản phẩm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));
            }

            // 🔎 Lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // 💰 Lọc theo khoảng giá
            if (!string.IsNullOrEmpty(priceRange))
            {
                query = priceRange switch
                {
                    "lt100" => query.Where(p => p.Price < 100_000),
                    "100-300" => query.Where(p => p.Price >= 100_000 && p.Price <= 300_000),
                    "300-600" => query.Where(p => p.Price >= 300_000 && p.Price <= 600_000),
                    "gt600" => query.Where(p => p.Price > 600_000),
                    _ => query
                };
            }

            // 3️⃣ Sắp xếp
            query = sortOrder switch
            {
                "price-asc" => query.OrderBy(p => p.Price),
                "price-desc" => query.OrderByDescending(p => p.Price),
                "date-asc" => query.OrderBy(p => p.CreatedDate),
                "date-desc" => query.OrderByDescending(p => p.CreatedDate),
                _ => query.OrderByDescending(p => p.CreatedDate) // mặc định mới nhất
            };

            // 4️⃣ Phân trang
            var totalItems = await query.CountAsync();
            var products = await query.Skip((page - 1) * PageSize)
                                           .Take(PageSize)
                                           .AsNoTracking()
                                           .ToListAsync();

            // 5️⃣ Đưa vào ViewModel
            var vm = new ProductListVm
            {
                Products = products,
                Categories = categories.ToList(), // Gửi danh sách categories để hiển thị bộ lọc
                CategoryId = categoryId,         // Giữ trạng thái category đã chọn
                SortOrder = sortOrder,
                PriceRange = priceRange,         // Giữ trạng thái priceRange đã chọn
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize),
                Search = search // 👈 Thêm nếu bạn dùng trong view
            };

            return View(vm); // → Views/Product/Index.cshtml
        }

        [HttpGet]
        public async Task<IActionResult> Suggest(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            term = term.ToLower().Trim();

            var results = await _productRepo.GetQueryable()
                .Include(p => p.Category) // Đảm bảo có Category
                .Where(p => p.IsAvailable &&
                       (
                             p.Name.ToLower().Contains(term) ||
                             (p.Category != null && p.Category.Name.ToLower().Contains(term)) // Kiểm tra p.Category có null không
                        ))
                .Select(p => new
                {
                    id = p.Id,
                    name = $"{p.Name} ({(p.Category != null ? p.Category.Name : "N/A")})" // Xử lý Category.Name có thể null
                })
                .Take(5)
                .ToListAsync();

            return Json(results);
        }

      

        
        // Action hiển thị chi tiết sản phẩm và các ảnh AI đã tạo liên quan
        public async Task<IActionResult> Details_GeneratedImages(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _productRepo.GetQueryable() // Sử dụng _productRepo để lấy Product
                                            .Include(p => p.Images)
                                            .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // Lấy các ảnh AI đã tạo liên quan đến MainImage của sản phẩm này
            // Giả định bạn lưu ProductImagePath trong GeneratedImage là MainImage của Product
            var generatedImages = await _dbContext.GeneratedImages
                                                  .Where(gi => gi.ProductImagePath == product.MainImage && gi.IsSuccess)
                                                  .OrderByDescending(gi => gi.GeneratedDate)
                                                  .ToListAsync();

            // Tạo một ViewModel để truyền cả Product và danh sách GeneratedImages tới View
            var viewModel = new ProductDetailWithGeneratedImagesViewModel // Cần tạo ViewModel này
            {
                Product = product,
                GeneratedImages = generatedImages
            };

            return View(viewModel);
        }

        public IActionResult Collections()
        {
            // Bạn cần lấy dữ liệu cho ViewModel ở đây
            // Ví dụ:
            var collections = _dbContext.Collections.Include(c => c.Products).ToList();
            var viewModel = new CollectionListVm { Collections = collections };

            // Trả về view collections.cshtml trong thư mục Products
            return View("collections", viewModel);
        }
    }
}