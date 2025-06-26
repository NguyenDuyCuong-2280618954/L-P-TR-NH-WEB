using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebThoiTrang;
using WebThoiTrang.Models.Entity;
using Microsoft.AspNetCore.Mvc.Rendering; // Để sử dụng SelectList

namespace WebThoiTrang.Areas.Admin.Controllers
{
    [Area("Admin")] // Đảm bảo controller thuộc về Area "Admin"
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // Để xử lý upload file

        public ProductController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Product
        // Hiển thị danh sách sản phẩm
        public async Task<IActionResult> Index()
        {
            // Bao gồm Category để hiển thị tên danh mục
            var products = await _context.Products
                                       .Include(p => p.Category)
                                       .ToListAsync();
            return View(products);
        }

        // GET: Admin/Product/Details/5
        // Hiển thị chi tiết sản phẩm
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.Images) // Bao gồm cả ProductImages
                                        .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Admin/Product/Create
        // Hiển thị form tạo sản phẩm mới
        public IActionResult Create()
        {
            // Lấy danh sách Categories để hiển thị trong dropdown
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Admin/Product/Create
        // Xử lý tạo sản phẩm mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Price,Description,Color,CategoryId,IsAvailable,Rating,Sizes")] Product product, IFormFile? mainImageFile, List<IFormFile>? otherImageFiles)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh chính (MainImage)
                if (mainImageFile != null)
                {
                    product.MainImage = await UploadFile(mainImageFile);
                }

                // Xử lý upload các ảnh khác (Images)
                if (otherImageFiles != null && otherImageFiles.Any())
                {
                    product.Images = new List<ProductImage>();
                    foreach (var file in otherImageFiles)
                    {
                        var imageUrl = await UploadFile(file);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            product.Images.Add(new ProductImage { Url = imageUrl });
                        }
                    }
                }

                // Khởi tạo CreatedDate
                product.CreatedDate = DateTime.Now;

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // Nếu có lỗi, load lại danh sách Categories
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Admin/Product/Edit/5
        // Hiển thị form chỉnh sửa sản phẩm
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Bao gồm ProductImages để có thể xóa/thêm ảnh khi chỉnh sửa
            var product = await _context.Products
                                        .Include(p => p.Images)
                                        .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Admin/Product/Edit/5
        // Xử lý chỉnh sửa sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,Description,MainImage,Color,CategoryId,IsAvailable,Rating,Sizes,CreatedDate")] Product product, IFormFile? mainImageFile, List<IFormFile>? otherImageFiles, List<int>? currentImageIdsToDelete)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy sản phẩm hiện có từ DB để xử lý các thuộc tính không có trong Bind
                    var existingProduct = await _context.Products
                                                        .Include(p => p.Images)
                                                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật các thuộc tính từ form vào existingProduct
                    existingProduct.Name = product.Name;
                    existingProduct.Price = product.Price;
                    existingProduct.Description = product.Description;
                    existingProduct.Color = product.Color;
                    existingProduct.CategoryId = product.CategoryId;
                    existingProduct.IsAvailable = product.IsAvailable;
                    existingProduct.Rating = product.Rating;
                    existingProduct.Sizes = product.Sizes; // Cần xử lý cẩn thận nếu Sizes phức tạp

                    // Xử lý upload ảnh chính mới
                    if (mainImageFile != null)
                    {
                        // Xóa ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(existingProduct.MainImage))
                        {
                            DeleteFile(existingProduct.MainImage);
                        }
                        existingProduct.MainImage = await UploadFile(mainImageFile);
                    }

                    // Xử lý xóa ảnh phụ
                    if (currentImageIdsToDelete != null && currentImageIdsToDelete.Any())
                    {
                        foreach (var imgId in currentImageIdsToDelete)
                        {
                            var imgToDelete = existingProduct.Images?.FirstOrDefault(img => img.Id == imgId);
                            if (imgToDelete != null)
                            {
                                DeleteFile(imgToDelete.Url);
                                _context.ProductImages.Remove(imgToDelete);
                            }
                        }
                    }

                    // Xử lý upload ảnh phụ mới
                    if (otherImageFiles != null && otherImageFiles.Any())
                    {
                        if (existingProduct.Images == null)
                        {
                            existingProduct.Images = new List<ProductImage>();
                        }
                        foreach (var file in otherImageFiles)
                        {
                            var imageUrl = await UploadFile(file);
                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                existingProduct.Images.Add(new ProductImage { Url = imageUrl });
                            }
                        }
                    }

                    _context.Update(existingProduct);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Admin/Product/Delete/5
        // Hiển thị xác nhận xóa sản phẩm
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.Images)
                                        .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Admin/Product/Delete/5
        // Xử lý xóa sản phẩm
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                                        .Include(p => p.Images) // Đảm bảo tải các ảnh liên quan
                                        .FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                // Xóa ảnh chính
                if (!string.IsNullOrEmpty(product.MainImage))
                {
                    DeleteFile(product.MainImage);
                }

                // Xóa các ảnh phụ
                if (product.Images != null)
                {
                    foreach (var image in product.Images)
                    {
                        DeleteFile(image.Url);
                    }
                }
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        // Phương thức hỗ trợ upload file
        private async Task<string> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return string.Empty;
            }

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Trả về đường dẫn tương đối để lưu vào database
            return "/images/products/" + uniqueFileName;
        }

        // Phương thức hỗ trợ xóa file
        private void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            // Chuyển đổi đường dẫn tương đối thành đường dẫn vật lý
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, filePath.TrimStart('/'));

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}