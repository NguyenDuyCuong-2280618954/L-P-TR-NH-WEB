using Microsoft.Extensions.Logging.Abstractions;
using WebThoiTrang.Models.Entity;

namespace WebThoiTrang.Models
{
    public class ProductListVm
    {
         
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();

        public int? CategoryId { get; set; }
        public string? SortOrder { get; set; }
        public string? PriceRange { get; set; }
        // Phân trang (tuỳ chọn)
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public string? Search { get; set; } = null;
    }
}
