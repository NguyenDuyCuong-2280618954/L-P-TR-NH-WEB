// Services/ImageProcessingService.cs
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using System;
using System.Collections.Generic;
using System.IO; // Thêm thư viện này để đọc file

namespace WebThoiTrang.Services
{
    public class ImageProcessingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ImageProcessingService> _logger;
        private readonly string _rapidApiKey;
        private readonly string _contentRootPath; // Thêm biến để truy cập thư mục gốc của ứng dụng

        public ImageProcessingService(HttpClient httpClient, IConfiguration configuration, ILogger<ImageProcessingService> logger, Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostEnvironment)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _contentRootPath = webHostEnvironment.ContentRootPath; // Khởi tạo contentRootPath

            _rapidApiKey = _configuration["RapidApiSettings:ApiKey"]
                         ?? throw new InvalidOperationException("RapidApiSettings:ApiKey not found in configuration.");

            if (!_httpClient.DefaultRequestHeaders.Contains("X-RapidAPI-Key"))
            {
                _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Key", _rapidApiKey);
            }
        }

        public async Task<AIResult> GenerateAIImage(string userImageRelativePath, string productMainImageForAIBasePath)
        {
            try
            {
                // Bước 1: Xác định lại endpoint chính xác
                const string apiEndpoint = "try-on-file"; // <-- THAY ĐỔI TẠI ĐÂY!
                                                          // Base URL đã được thiết lập trong Program.cs

                // Bước 2: Đọc file ảnh thành byte array
                string userImageFullPath = Path.Combine(_contentRootPath, userImageRelativePath.Replace("/", "\\"));
                string productImageFullPath = Path.Combine(_contentRootPath, productMainImageForAIBasePath.Replace("/", "\\")); // Đường dẫn của ảnh sản phẩm đã copy vào TryOn_Images/product

                if (!File.Exists(userImageFullPath))
                {
                    _logger.LogError($"File người dùng không tồn tại: {userImageFullPath}");
                    return new AIResult { IsSuccess = false, ErrorMessage = "File ảnh người dùng không tồn tại." };
                }
                if (!File.Exists(productImageFullPath))
                {
                    _logger.LogError($"File sản phẩm không tồn tại: {productImageFullPath}");
                    return new AIResult { IsSuccess = false, ErrorMessage = "File ảnh sản phẩm không tồn tại." };
                }

                byte[] userImageBytes = await File.ReadAllBytesAsync(userImageFullPath);
                byte[] clothingImageBytes = await File.ReadAllBytesAsync(productImageFullPath); // Đây là ảnh quần áo


                // Bước 3: Tạo MultipartFormDataContent
                using (var formData = new MultipartFormDataContent())
                {
                    // Thêm ảnh người dùng (avatar_image)
                    var userImageContent = new ByteArrayContent(userImageBytes);
                    // Có thể thêm Content-Type cho từng file nếu API yêu cầu cụ thể (ví dụ: image/jpeg, image/png)
                    userImageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg"); // Hoặc image/png tùy loại ảnh
                    formData.Add(userImageContent, "avatar_image", Path.GetFileName(userImageFullPath)); // "avatar_image" là tên trường form

                    // Thêm ảnh quần áo (clothing_image)
                    var clothingImageContent = new ByteArrayContent(clothingImageBytes);
                    clothingImageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg"); // Hoặc image/png
                    formData.Add(clothingImageContent, "clothing_image", Path.GetFileName(productImageFullPath)); // "clothing_image" là tên trường form

                    // Bước 4: Gửi yêu cầu POST với MultipartFormDataContent
                    _logger.LogInformation($"[ImageProcessingService] Sending images to API endpoint: {apiEndpoint}");
                    var response = await _httpClient.PostAsync(apiEndpoint, formData); // <-- Thay đổi ở đây
                    _logger.LogInformation($"[DEBUG] Content-Type: {response.Content.Headers.ContentType}");

                    _logger.LogInformation($"[DEBUG] Response StatusCode: {response.StatusCode}");
                    _logger.LogInformation($"[DEBUG] Response Headers: {response.Headers}");


                    if (response.IsSuccessStatusCode)
                    {
                        // Đọc dữ liệu nhị phân từ API (ảnh)
                        byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                        // Tạo thư mục lưu ảnh nếu chưa có
                        var resultDir = Path.Combine(_contentRootPath, "TryOn_Images", "result");
                        if (!Directory.Exists(resultDir))
                            Directory.CreateDirectory(resultDir);

                        // Đặt tên file ảnh kết quả
                        string uniqueFileName = $"ai_result_{Guid.NewGuid()}.jpg";
                        string savedImagePath = Path.Combine(resultDir, uniqueFileName);

                        // Lưu ảnh vào file
                        await File.WriteAllBytesAsync(savedImagePath, imageBytes);

                        // Trả về đường dẫn tương đối để lưu DB hoặc dùng trong view
                        string relativePath = Path.Combine("TryOn_Images", "result", uniqueFileName).Replace("\\", "/");

                        _logger.LogInformation($"[ImageProcessingService] AI result image saved to: {savedImagePath}");

                        return new AIResult
                        {
                            IsSuccess = true,
                            ResultImagePath = relativePath,
                            ErrorMessage = null
                        };
                    }

                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"[ImageProcessingService] API AI returned error status code: {response.StatusCode}. Content: {errorContent}");

                        string errorMessage = $"Lỗi từ API AI: {response.StatusCode}";
                        try
                        {
                            var errorApiResponse = JsonSerializer.Deserialize<AIAPIResponse>(errorContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (!string.IsNullOrEmpty(errorApiResponse?.Message))
                            {
                                errorMessage += $" - {errorApiResponse.Message}";
                            }
                            if (!string.IsNullOrEmpty(errorApiResponse?.Detail))
                            {
                                errorMessage += $" - {errorApiResponse.Detail}";
                            }
                        }
                        catch (JsonException)
                        {
                            errorMessage += $" - {errorContent}";
                        }

                        return new AIResult
                        {
                            IsSuccess = false,
                            ResultImagePath = null,
                            ErrorMessage = errorMessage
                        };
                    }
                } // using (var formData = ...)
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "[ImageProcessingService] Lỗi kết nối HTTP khi gọi API AI.");
                return new AIResult
                {
                    IsSuccess = false,
                    ResultImagePath = null,
                    ErrorMessage = $"Lỗi kết nối đến API AI: {httpEx.Message}. Đảm bảo API AI đang chạy và có thể truy cập."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ImageProcessingService] Lỗi không xác định khi gọi API AI.");
                return new AIResult
                {
                    IsSuccess = false,
                    ResultImagePath = null,
                    ErrorMessage = $"Lỗi không mong muốn: {ex.Message}"
                };
            }
        }

        private class AIAPIResponse
        {
            public string? ResultImageUrl { get; set; }
            public string? Message { get; set; }
            public string? Detail { get; set; }
        }
    }

    public class AIResult
    {
        public bool IsSuccess { get; set; }
        public string? ResultImagePath { get; set; }
        public string? ErrorMessage { get; set; }
    }
}