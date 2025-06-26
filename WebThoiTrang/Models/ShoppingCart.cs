// WebThoiTrang.Models/ShoppingCart.cs
using Microsoft.AspNetCore.Mvc; // Giữ lại nếu bạn có lý do gì đó dùng HttpPost ở đây,
                                // nhưng thường nó không thuộc về model thuần túy.
                                // Tôi sẽ loại bỏ HttpPost vì nó không hợp lý ở đây.
using System.Collections.Generic;
using System.Linq;

namespace WebThoiTrang.Models
{
    public class ShoppingCart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        // Đây là thuộc tính TotalAmount mà view đang gọi
        // Nó sẽ tự động tính toán tổng giá trị của tất cả các CartItem trong giỏ hàng
        public decimal TotalAmount => Items.Sum(item => item.TotalPrice); // Sử dụng TotalPrice từ CartItem

        // Phương thức để thêm một CartItem vào giỏ hàng
        // Đã sửa để so sánh cả ProductId, Size và Color
        public void AddItem(CartItem newItem)
        {
            var existingItem = Items.FirstOrDefault(i =>
                i.ProductId == newItem.ProductId &&
                i.Size == newItem.Size &&
                i.Color == newItem.Color);

            if (existingItem != null)
            {
                existingItem.Quantity += newItem.Quantity;
            }
            else
            {
                Items.Add(newItem);
            }
        }

        // Phương thức để xóa một CartItem khỏi giỏ hàng
        // Đã đảm bảo so sánh đúng ProductId, Size và Color
        public void RemoveItem(int productId, string? size = null, string? color = null)
        {
            size ??= string.Empty;
            color ??= string.Empty;

            Items.RemoveAll(i =>
                i.ProductId == productId &&
                (i.Size ?? string.Empty) == size && // So sánh an toàn với null
                (i.Color ?? string.Empty) == color); // So sánh an toàn với null
        }

        // Phương thức mới: Cập nhật số lượng của một sản phẩm trong giỏ hàng
        // Phương thức này được ShoppingCartController sử dụng
        public void UpdateItemQuantity(int productId, string? size, string? color, int newQuantity)
        {
            size ??= string.Empty;
            color ??= string.Empty;

            var itemToUpdate = Items.FirstOrDefault(i =>
                i.ProductId == productId &&
                (i.Size ?? string.Empty) == size &&
                (i.Color ?? string.Empty) == color);

            if (itemToUpdate != null)
            {
                if (newQuantity > 0)
                {
                    itemToUpdate.Quantity = newQuantity;
                }
                else
                {
                    // Nếu số lượng mới là 0 hoặc âm, xóa sản phẩm
                    Items.Remove(itemToUpdate);
                }
            }
        }

        // Phương thức để xóa toàn bộ giỏ hàng
        public void Clear()
        {
            Items.Clear();
        }
    }
}