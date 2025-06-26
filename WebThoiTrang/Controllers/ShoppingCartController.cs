using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebThoiTrang.Models;
using WebThoiTrang.Models.Entity;
using WebThoiTrang.Repositories.Interface;
using WebThoiTrang.Extensions;

namespace WebThoiTrang.Controllers
{
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IProductRepository productRepository)
        {
            _context = context;
            _userManager = userManager;
            _productRepository = productRepository;
        }

        /*--------------------------------------------------------------
         * 1. ADD TO CART – lấy tên & ảnh từ DB, gộp item nếu trùng
        ----------------------------------------------------------------*/
        /*────────────── 1. POST: /ShoppingCart/AddToCart ──────────────*/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId,
                                                  int quantity = 1,
                                                  string? size = null,
                                                  string? color = null)
        {
            await AddToCartCore(productId, quantity, size, color);
            return RedirectToAction("Index", "ShoppingCart");
        }

        /*────────────── 2. GET:  /ShoppingCart/AddToCart ──────────────*/
        [HttpGet, ActionName("AddToCart")]
        [Authorize]              // chỉ xử lý nếu user đã login
        public async Task<IActionResult> AddToCartGet(int productId,
                                              int quantity = 1,
                                              string? size = null,
                                              string? color = null)
        {
            await AddToCartCore(productId, quantity, size, color);
            return RedirectToAction("Index", "ShoppingCart");
        }


        /*────────────── 3. Logic dùng chung ──────────────*/
        private async Task AddToCartCore(int productId, int quantity,
                                 string? size, string? color)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return;
            }

            // ✅ Kiểm tra nếu sản phẩm yêu cầu chọn size mà chưa chọn
            if (string.IsNullOrEmpty(size))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn kích thước (size) trước khi thêm vào giỏ hàng.";
                return;
            }

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();

            var item = cart.Items.FirstOrDefault(i =>
                i.ProductId == productId &&
                i.Size == (size ?? string.Empty) &&
                i.Color == (color ?? string.Empty));

            if (item != null)
                item.Quantity += quantity;
            else
                cart.Items.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    MainImage = product.MainImage,
                    Price = product.Price,
                    Quantity = quantity,
                    Size = size ?? string.Empty,
                    Color = color ?? string.Empty
                });

            HttpContext.Session.SetObjectAsJson("Cart", cart);
        }



        /*--------------------------------------------------------------
         * 2. HIỆN GIỎ HÀNG
        ----------------------------------------------------------------*/
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            return View(cart);
        }

        /*--------------------------------------------------------------
         * 3. CHECKOUT  (giữ nguyên logic cũ)
        ----------------------------------------------------------------*/
        /* ---------- GET: Checkout ----------*/
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new();

            var vm = new CheckoutViewModel
            {
                CartItems = cart.Items,
                SubTotal = cart.Items.Sum(i => i.Price * i.Quantity),
                ShippingFee = 30000
            };
            return View(vm);
        }

        // ---------- POST: Checkout ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel vm)
        {
            if (!ModelState.IsValid || vm.CartItems == null || !vm.CartItems.Any())
                return View(vm);

            var user = await _userManager.GetUserAsync(User);
            var order = new Order
            {
                UserId = user!.Id,
                OrderDate = DateTime.UtcNow,
                TotalPrice = vm.Total,           // đã gồm GiftWrapFee
                IsGiftWrapped = vm.IsGiftWrap,      // ✅ lưu lại
                ShippingInfo = vm.ShippingInfo,
                Notes = vm.PaymentInfo.Note,
                OrderDetails = vm.CartItems.Select(i => new OrderDetail
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Price,
                    Size = i.Size,
                    Color = i.Color
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("Cart");
            return View("OrderCompleted", order.Id);
        }


        /*--------------------------------------------------------------
         * 4. XOÁ ITEM
        ----------------------------------------------------------------*/
        public IActionResult RemoveFromCart(int productId, string? size = null, string? color = null)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart != null)
            {
                cart.RemoveItem(productId, size, color);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
            return RedirectToAction("Index");
        }

        [HttpPost] // Phương thức này phải là POST vì nó thay đổi dữ liệu
        //[ValidateAntiForgeryToken] // Đảm bảo an toàn, chống tấn công CSRF
        public IActionResult UpdateQuantity(int productId, string color, string size, int newQuantity)
        {
            // Lấy giỏ hàng từ Session
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");

            // Kiểm tra xem giỏ hàng có tồn tại không
            if (cart == null)
            {
                TempData["ErrorMessage"] = "Giỏ hàng không tồn tại.";
                return RedirectToAction("Index");
            }

            // Đảm bảo số lượng mới không âm. Nếu số lượng là 0, sản phẩm sẽ bị xóa.
            if (newQuantity < 0)
            {
                newQuantity = 0;
            }

            // Gọi phương thức UpdateItemQuantity trong model ShoppingCart
            // để cập nhật số lượng hoặc xóa sản phẩm nếu newQuantity là 0
            cart.UpdateItemQuantity(productId, color ?? string.Empty, size ?? string.Empty, newQuantity);

            // Lưu giỏ hàng đã cập nhật trở lại Session
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            // Tùy chọn: Thêm thông báo thành công
            TempData["SuccessMessage"] = "Số lượng sản phẩm đã được cập nhật.";

            TempData["ErrorMessage"] = "Model không hợp lệ: " + string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction("Index");

            // Chuyển hướng về trang Index để hiển thị giỏ hàng đã cập nhật
            return RedirectToAction("Index");

            /*
             * Hoặc, nếu bạn muốn cập nhật bằng AJAX (không load lại toàn bộ trang),
             * bạn có thể trả về JSON. Điều này yêu cầu logic JavaScript phức tạp hơn ở client-side.
             * Ví dụ:
             * return Json(new { success = true, newCartTotal = cart?.TotalAmount.ToString("N0") + " VNĐ" });
             */
        }
    }
}
