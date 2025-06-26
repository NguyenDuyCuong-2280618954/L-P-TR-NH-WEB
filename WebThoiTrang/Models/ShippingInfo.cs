using Microsoft.EntityFrameworkCore;

namespace WebThoiTrang.Models
{
    [Owned]
    public class ShippingInfo
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}
