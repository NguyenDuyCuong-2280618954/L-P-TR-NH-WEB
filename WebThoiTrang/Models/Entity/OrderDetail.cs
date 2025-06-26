using System.ComponentModel.DataAnnotations;

namespace WebThoiTrang.Models.Entity
{
    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // tuỳ chọn
        [MaxLength(10)]
        public string Size { get; set; } = string.Empty;
        [MaxLength(20)]
        public string Color { get; set; } = string.Empty;
    }
}
