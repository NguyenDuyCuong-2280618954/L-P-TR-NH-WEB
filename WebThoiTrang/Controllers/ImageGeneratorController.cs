using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using WebThoiTrang.Services;
using WebThoiTrang.Models.Entity;
using WebThoiTrang.Models; // Đảm bảo đúng namespace của DbContext nếu bạn dùng WebThoiTrang.Data.ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Để sử dụng Include
using Microsoft.AspNetCore.Hosting; // Để sử dụng IWebHostEnvironment
using Microsoft.Extensions.Logging; // Để sử dụng ILogger
using System.Net.Http; // Thêm để sử dụng HttpClient trong ProcessAI khi tải ảnh AI về

namespace WebThoiTrang.Controllers
{
    public class ImageGeneratorController : Controller
    {
        private readonly ImageProcessingService _imageProcessingService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ImageGeneratorController> _logger;

        public ImageGeneratorController(
            ImageProcessingService imageProcessingService,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext dbContext,
            ILogger<ImageGeneratorController> logger)
        {
            _imageProcessingService = imageProcessingService;
            _webHostEnvironment = webHostEnvironment;
            _dbContext = dbContext;
            _logger = logger;
        }

        // Action: Index (GET) - Hiển thị trang chọn ảnh và preview
        [HttpGet]
        public async Task<IActionResult> Index(int? productId)
        {
            if (productId == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy ID sản phẩm.";
                return RedirectToAction("Index", "Home");
            }

            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId.Value);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ProductId = productId.Value;
            ViewBag.ProductMainImage = product.MainImage; // Đường dẫn tương đối của ảnh sản phẩm (từ wwwroot)
            ViewBag.ProductName = product.Name;

            // Lấy đường dẫn ảnh người dùng đã upload từ TempData (nếu có từ lần POST trước đó)
            if (TempData["UploadedUserImagePath"] != null)
            {
                ViewBag.UploadedUserImagePath = TempData["UploadedUserImagePath"].ToString();
                ViewBag.UploadedUserImageMessage = "Ảnh của bạn đã được tải lên thành công!";
            }
            // Đảm bảo TempData["Message"] và TempData["ErrorMessage"] cũng được chuyển tiếp
            ViewBag.Message = TempData["Message"];
            ViewBag.ErrorMessage = TempData["ErrorMessage"];
            System.Console.WriteLine(product.Id + productId);

            return View();
        }

        // Action: Generate (POST) - Xử lý upload ảnh người dùng vào thư mục TryOn_Images/user
        [HttpPost]
        public async Task<IActionResult> Generate(int productId, IFormFile userImage)
        {
            if (userImage == null || userImage.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ảnh người dùng để tải lên.";
                return RedirectToAction("Index", new { productId = productId });
            }

            // Lấy thông tin sản phẩm (chỉ để kiểm tra sự tồn tại của sản phẩm)
            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Home");
            }

            string userImageRelativePath = string.Empty;

            try
            {
                // 1. Lưu ảnh người dùng vào thư mục TryOn_Images/user
                var uploadsFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "TryOn_Images", "user");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Tạo tên file duy nhất
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(userImage.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await userImage.CopyToAsync(fileStream);
                }
                // Đường dẫn tương đối sẽ là /TryOn_Images/user/uniqueFileName.ext để khớp với RequestPath
                userImageRelativePath = Path.Combine("TryOn_Images", "user", uniqueFileName).Replace("\\", "/");
                _logger.LogInformation($"Ảnh người dùng đã lưu: {userImageRelativePath}");

                // Đặt đường dẫn ảnh người dùng đã upload vào TempData để chuyển về View
                TempData["UploadedUserImagePath"] = userImageRelativePath;
                TempData["Message"] = "Ảnh của bạn đã được tải lên thành công! Bây giờ bạn có thể bắt đầu thử đồ AI.";

                // Chuyển hướng lại về trang Index để hiển thị ảnh vừa upload
                return RedirectToAction("Index", new { productId = productId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải lên ảnh người dùng.");
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi không mong muốn khi tải ảnh lên: {ex.Message}";
                return RedirectToAction("Index", new { productId = productId });
            }
        }

        // Action: ProcessAI (POST) - Xử lý gọi AI API và lưu kết quả vào TryOn_Images/result

        public async Task ProcessAIAsync(int productId, string userImageRelativePath)
        {
            string userImageFullPath = Path.Combine(_webHostEnvironment.ContentRootPath, userImageRelativePath.Replace("/", "\\"));
            if (string.IsNullOrEmpty(userImageRelativePath) || !System.IO.File.Exists(userImageFullPath))
            {
                _logger.LogWarning("[TryOnJobService] Ảnh người dùng không tồn tại: " + userImageRelativePath);
                return;
            }

            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                _logger.LogWarning("[TryOnJobService] Sản phẩm không tồn tại với ID: " + productId);
                return;
            }

            string productMainImageRelativePath = product.MainImage;
            string productMainImageForAIBasePath = string.Empty;
            string resultImageStoredPath = string.Empty;

            try
            {
                var productOriginalFullPath = Path.Combine(_webHostEnvironment.WebRootPath, productMainImageRelativePath.TrimStart('/'));

                if (!System.IO.File.Exists(productOriginalFullPath))
                {
                    _logger.LogWarning("[TryOnJobService] Ảnh sản phẩm gốc không tồn tại: " + productOriginalFullPath);
                    return;
                }

                var aiProductFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "TryOn_Images", "product");
                Directory.CreateDirectory(aiProductFolder);

                string productFileName = Path.GetFileName(productMainImageRelativePath);
                string uniqueProductFileName = Guid.NewGuid().ToString() + "_" + productFileName;
                string productCopyPath = Path.Combine(aiProductFolder, uniqueProductFileName);
                System.IO.File.Copy(productOriginalFullPath, productCopyPath, true);

                productMainImageForAIBasePath = Path.Combine("TryOn_Images", "product", uniqueProductFileName).Replace("\\", "/");

                _logger.LogInformation("[TryOnJobService] Đang gọi AI với user: {0}, product: {1}", userImageRelativePath, productMainImageForAIBasePath);

                var aiResult = await _imageProcessingService.GenerateAIImage(userImageRelativePath, productMainImageForAIBasePath);

                if (aiResult.IsSuccess && !string.IsNullOrEmpty(aiResult.ResultImagePath))
                {
                    var savedFullPath = Path.Combine(_webHostEnvironment.ContentRootPath, aiResult.ResultImagePath.Replace("/", "\\"));
                    var resultFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "TryOn_Images", "result");
                    Directory.CreateDirectory(resultFolder);

                    if (System.IO.File.Exists(savedFullPath))
                    {
                        string uniqueResultFileName = "ai_result_" + Guid.NewGuid().ToString() + ".png";
                        string resultFilePath = Path.Combine(resultFolder, uniqueResultFileName);

                        byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(savedFullPath);
                        await System.IO.File.WriteAllBytesAsync(resultFilePath, imageBytes);

                        resultImageStoredPath = Path.Combine("TryOn_Images", "result", uniqueResultFileName).Replace("\\", "/");
                        _logger.LogInformation("[TryOnJobService] Đã lưu ảnh kết quả AI tại: {0}", resultImageStoredPath);
                    }
                    else
                    {
                        _logger.LogError("[TryOnJobService] File ảnh kết quả không tồn tại tại: {0}", savedFullPath);
                        resultImageStoredPath = aiResult.ResultImagePath;
                    }
                }
                else
                {
                    resultImageStoredPath = aiResult.ResultImagePath;
                }

                var generatedImage = new GeneratedImage
                {
                    ProductId = productId,
                    UserImagePath = userImageRelativePath,
                    ProductImagePath = productMainImageRelativePath,
                    GeneratedDate = DateTime.Now,
                    IsSuccess = aiResult.IsSuccess,
                    ErrorMessage = aiResult.ErrorMessage,
                    ResultImagePath = resultImageStoredPath
                };

                _dbContext.GeneratedImages.Add(generatedImage);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("[TryOnJobService] Đã lưu GeneratedImage ID = {0}", generatedImage.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TryOnJobService] Lỗi khi xử lý ảnh AI");
            }
        }

        // Action: Result (GET) - Hiển thị trang kết quả
        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var generatedImage = await _dbContext.GeneratedImages
                                                    .Include(gi => gi.Product) // Đảm bảo Product đã được load
                                                    .FirstOrDefaultAsync(gi => gi.Id == id);

            if (generatedImage == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy kết quả thử đồ.";
                return RedirectToAction("Index", "Home");
            }

            return View(generatedImage);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessAndShowResult(int productId, string userImageRelativePath)
        {
            await ProcessAIAsync(productId, userImageRelativePath);

            // Tìm kết quả mới nhất của user với sản phẩm này
            var generated = await _dbContext.GeneratedImages
                .OrderByDescending(g => g.GeneratedDate)
                .FirstOrDefaultAsync(g => g.ProductId == productId && g.UserImagePath == userImageRelativePath);

            if (generated != null)
            {
                return RedirectToAction("Result", new { id = generated.Id });
            }

            TempData["ErrorMessage"] = "Không thể tìm thấy ảnh kết quả sau khi xử lý AI.";
            return RedirectToAction("Index", new { productId });
        }


    }
}