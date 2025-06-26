using WebThoiTrang.Models.Entity;

namespace WebThoiTrang.Models
{
    public class ProductViewModel
    {
        public Product Product { get; set; } = new Product();
        public List<Product> RelatedProducts { get; set; } = new List<Product>();
        public int SelectedQuantity { get; set; } = 1;
        public string SelectedSize { get; set; } = string.Empty;
        public List<WebThoiTrang.Models.Entity.Category> Categories { get; set; } // Thêm thuộc tính này
        public string SelectedCategory { get; set; }
    }
}
