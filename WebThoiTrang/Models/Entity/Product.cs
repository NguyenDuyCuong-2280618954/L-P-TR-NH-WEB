using System.ComponentModel.DataAnnotations;

namespace WebThoiTrang.Models.Entity
{
    public class Product
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; }
        [Range(0, 10000000, ErrorMessage = "Giá phải >= 0")]
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;

        public string MainImage { get; set; } = string.Empty;
        public List<ProductImage>? Images { get; set; }
        public string Color { get; set; } = string.Empty;
        public List<string> Sizes { get; set; } = new List<string>();
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public int? CollectionId { get; set; } // Foreign key for Collection
        public Collection? Collection { get; set; } // Navigation property to Collection
        public bool IsAvailable { get; set; } = true;
        public int Rating { get; set; } = 5;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

}
