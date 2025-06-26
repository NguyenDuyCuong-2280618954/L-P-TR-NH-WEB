// Trong Services/TryOnJobService.cs

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using WebThoiTrang.Models.Entity;
using WebThoiTrang;
using Microsoft.EntityFrameworkCore;

namespace WebThoiTrang.Services
{
    public class TryOnJobService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<TryOnJobService> _logger;
        private readonly ImageProcessingService _imageProcessingService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TryOnJobService(ApplicationDbContext dbContext,
                                ILogger<TryOnJobService> logger,
                                ImageProcessingService imageProcessingService,
                                IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _logger = logger;
            _imageProcessingService = imageProcessingService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task ProcessTryOnJob(int productId, string userImageRelativePath)
        {
            string userImageFullPath = Path.Combine(_webHostEnvironment.ContentRootPath, userImageRelativePath.Replace("/", "\\"));
            if (string.IsNullOrEmpty(userImageRelativePath) || !File.Exists(userImageFullPath))
            {
                _logger.LogError("[TryOnJobService] Ảnh người dùng không tồn tại tại đường dẫn: {Path}", userImageFullPath);
                return;
            }

            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                _logger.LogError("[TryOnJobService] Sản phẩm không tồn tại với ID: {ProductId}", productId);
                return;
            }

            var productMainImageRelativePath = product.MainImage;
            var productOriginalFullPath = Path.Combine(_webHostEnvironment.WebRootPath, productMainImageRelativePath.TrimStart('/'));
            if (!File.Exists(productOriginalFullPath))
            {
                _logger.LogError("[TryOnJobService] Ảnh sản phẩm không tồn tại tại đường dẫn: {Path}", productOriginalFullPath);
                return;
            }

            var aiProductFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "TryOn_Images", "product");
            if (!Directory.Exists(aiProductFolder))
            {
                Directory.CreateDirectory(aiProductFolder);
            }

            string productFileName = Path.GetFileName(productMainImageRelativePath);
            string uniqueProductFileName = Guid.NewGuid().ToString() + "_" + productFileName;
            string productCopyPath = Path.Combine(aiProductFolder, uniqueProductFileName);
            File.Copy(productOriginalFullPath, productCopyPath, true);
            string productMainImageForAIBasePath = Path.Combine("TryOn_Images", "product", uniqueProductFileName).Replace("\\", "/");

            var aiResult = await _imageProcessingService.GenerateAIImage(userImageRelativePath, productMainImageForAIBasePath);

            string resultImageStoredPath = aiResult.ResultImagePath ?? string.Empty;

            // Lưu ảnh kết quả về máy nếu có URL
            if (aiResult.IsSuccess && !string.IsNullOrEmpty(aiResult.ResultImagePath))
            {
                try
                {
                    var resultFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "TryOn_Images", "result");
                    if (!Directory.Exists(resultFolder))
                        Directory.CreateDirectory(resultFolder);

                    // NEW - đã có file local, không cần tải từ URL
                    string savedFullPath = Path.Combine(_webHostEnvironment.ContentRootPath, aiResult.ResultImagePath.Replace("/", "\\"));

                    if (File.Exists(savedFullPath))
                    {
                        resultImageStoredPath = aiResult.ResultImagePath; // dùng luôn path tương đối đã trả về từ ImageProcessingService
                    }
                    else
                    {
                        _logger.LogError("[TryOnJobService] File ảnh kết quả không tồn tại tại: {Path}", savedFullPath);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TryOnJobService] Lỗi tải ảnh kết quả từ URL: {Url}", aiResult.ResultImagePath);
                }
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

            _logger.LogInformation("[TryOnJobService] Đã xử lý AI thành công cho ProductId = {ProductId}, ResultImage = {ResultImage}", productId, resultImageStoredPath);
        }
    }
}
