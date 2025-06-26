using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebThoiTrang.Models.Entity
{
    public enum OrderStatus
    {
        Pending, Processing, Shipped, Completed, Cancelled
    }
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        /*------------- GỘP ShippingInfo -------------*/
        public ShippingInfo ShippingInfo { get; set; } = new();
        public string? Notes { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public bool IsGiftWrapped { get; set; } = false;

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
