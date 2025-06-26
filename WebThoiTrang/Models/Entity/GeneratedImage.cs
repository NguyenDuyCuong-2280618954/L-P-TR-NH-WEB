// Models/Entity/GeneratedImage.cs

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebThoiTrang.Models.Entity
{
    public class GeneratedImage
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; } // Navigation property

        [Required] // Giả sử UserImagePath luôn cần có
        public string UserImagePath { get; set; }

        [Required] // Giả sử ProductImagePath luôn cần có
        public string ProductImagePath { get; set; }

        // THAY ĐỔI TẠI ĐÂY: Thêm dấu '?' để cho phép null
        public string? ResultImagePath { get; set; }

        public DateTime GeneratedDate { get; set; }

        public bool IsSuccess { get; set; }

        // Cũng nên cho phép null cho ErrorMessage nếu không phải lúc nào cũng có lỗi
        public string? ErrorMessage { get; set; }
    }
}