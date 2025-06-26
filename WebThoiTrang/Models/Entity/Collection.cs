using System.ComponentModel.DataAnnotations;


namespace WebThoiTrang.Models.Entity
{
    public class Collection
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        // Add other properties relevant to your Collection
        public List<Product>? Products { get; set; }
    }
}
