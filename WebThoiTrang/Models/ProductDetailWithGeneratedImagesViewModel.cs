// Models/ProductDetailWithGeneratedImagesViewModel.cs
using WebThoiTrang.Models.Entity;
using System.Collections.Generic;

namespace WebThoiTrang.Models
{
    public class ProductDetailWithGeneratedImagesViewModel
    {
        public Product Product { get; set; }
        public List<GeneratedImage> GeneratedImages { get; set; } = new List<GeneratedImage>();
        // Bạn có thể thêm các thuộc tính khác nếu cần, ví dụ: RelatedProducts
    }
}