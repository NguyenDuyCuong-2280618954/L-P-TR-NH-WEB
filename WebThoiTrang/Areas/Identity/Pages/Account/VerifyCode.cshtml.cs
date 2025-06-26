// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebThoiTrang.Models.Entity; // Đảm bảo đúng namespace cho ApplicationUser và ApplicationDbContext
using WebThoiTrang.Services; // Đảm bảo dòng này CÒN LẠI để sử dụng IEmailSender tùy chỉnh
using Hangfire; // Thêm dòng này để sử dụng Hangfire

namespace WebThoiTrang.Areas.Identity.Pages.Account
{
    public class VerifyCodeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<VerifyCodeModel> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient; // THÊM: Inject Hangfire Background Job Client

        public VerifyCodeModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<VerifyCodeModel> logger,
            IBackgroundJobClient backgroundJobClient) // THÊM: Inject Hangfire Background Job Client vào constructor
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient; // KHỞI TẠO: Gán backgroundJobClient
        }

        [BindProperty(SupportsGet = true)]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập mã xác nhận.")]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã xác nhận phải có 6 chữ số.")]
            [DataType(DataType.Text)]
            [Display(Name = "Mã xác nhận")]
            public string Code { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; } // Email của người dùng cần xác nhận

            public string ReturnUrl { get; set; }
        }

        public async Task<IActionResult> OnGet(string email, string returnUrl = null)
        {
            // === ĐÃ SỬA: Ưu tiên lấy email từ TempData nếu tham số route không có ===
            // Nếu tham số 'email' từ URL (route parameter) bị null hoặc rỗng,
            // hãy thử lấy nó từ TempData (được RegisterModel gửi đến).
            if (string.IsNullOrEmpty(email))
            {
                if (TempData.ContainsKey("Email"))
                {
                    email = TempData["Email"] as string;
                    // TempData là đọc-một-lần. Nếu trang có thể được tải lại, cần giữ nó lại.
                    // Điều này đặc biệt hữu ích nếu OnGet được gọi lại sau một thao tác nào đó.
                    TempData.Keep("Email");
                }
            }

            // Dù đã thử TempData, vẫn kiểm tra lại một lần nữa.
            if (string.IsNullOrEmpty(email))
            {
                // Nếu không tìm thấy email sau tất cả các phương pháp, thì mới báo lỗi và chuyển hướng
                TempData["ErrorMessage"] = "Không tìm thấy thông tin email. Vui lòng đăng ký lại.";
                return RedirectToPage("/Account/Register");
            }

            // Gán email đã tìm thấy vào InputModel để hiển thị trên trang Razor Page
            Input = new InputModel { Email = email, ReturnUrl = returnUrl };

            // Kiểm tra xem có mã nào còn hạn và chưa sử dụng không
            var activeCode = await _context.EmailConfirmations
                                            .FirstOrDefaultAsync(ec => ec.Email == email && !ec.IsUsed && ec.Expiration > DateTime.UtcNow);
            if (activeCode == null)
            {
                TempData["InfoMessage"] = "Không tìm thấy mã xác nhận đang hoạt động. Vui lòng yêu cầu gửi lại mã.";
            }

            // Để các thông báo từ RegisterModel (ví dụ: WarningMessage nếu gửi email lỗi) có thể hiển thị
            if (TempData["WarningMessage"] != null)
            {
                TempData.Keep("WarningMessage");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            Console.WriteLine("[DEBUG] OnPostAsync bắt đầu");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("[DEBUG] ModelState không hợp lệ");

                if (string.IsNullOrEmpty(Input.Email) && TempData.ContainsKey("Email"))
                {
                    Input.Email = TempData["Email"] as string;
                    TempData.Keep("Email");
                }

                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                Console.WriteLine($"[DEBUG] Không tìm thấy user với email: {Input.Email}");
                ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại.");
                return Page();
            }

            var emailConfirmation = await _context.EmailConfirmations
                                                .FirstOrDefaultAsync(ec => ec.Email == Input.Email && !ec.IsUsed && ec.Code == Input.Code && ec.Expiration > DateTime.UtcNow);

            if (emailConfirmation != null)
            {
                Console.WriteLine("[DEBUG] Mã xác nhận hợp lệ");

                emailConfirmation.IsUsed = true;
                await _context.SaveChangesAsync();

                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = "Email của bạn đã được xác nhận thành công!";
                _logger.LogInformation($"User {Input.Email} email confirmed successfully.");

                Console.WriteLine("[DEBUG] Chuyển hướng đến trang đăng nhập");
                return RedirectToPage("/Account/Login", new { area = "Identity" }); // <-- Sửa ở đây
            }
            else
            {
                Console.WriteLine("[DEBUG] Mã xác nhận không hợp lệ hoặc đã hết hạn");
                ModelState.AddModelError(string.Empty, "Mã xác nhận không hợp lệ hoặc đã hết hạn.");
                return Page();
            }
        }


        public async Task<IActionResult> OnGetResendCodeAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return new JsonResult(new { success = false, message = "Email không được cung cấp." });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy tài khoản với email này." });
            }

            // Xóa tất cả các mã xác nhận cũ còn hoạt động của email này
            var existingConfirmations = await _context.EmailConfirmations
                                                     .Where(ec => ec.Email == email && !ec.IsUsed && ec.Expiration > DateTime.UtcNow)
                                                     .ToListAsync();
            if (existingConfirmations.Any())
            {
                _context.EmailConfirmations.RemoveRange(existingConfirmations);
            }

            // Tạo mã mới
            var newCode = new Random().Next(100000, 999999).ToString();
            Console.WriteLine($"[DEBUG] New verification code for {email}: {newCode}");

            var newEmailConfirm = new EmailConfirmation
            {
                UserId = user.Id, // Đảm bảo gán UserId
                Email = email,
                Code = newCode,
                Expiration = DateTime.UtcNow.AddSeconds(60),
                IsUsed = false
            };
            _context.EmailConfirmations.Add(newEmailConfirm);
            // Luôn lưu vào DB TRƯỚC KHI enqueue job
            await _context.SaveChangesAsync();

            // === BẮT ĐẦU SỬ DỤNG HANGFIRE ĐỂ GỬI EMAIL ===
            var subject = "Mã xác nhận mới cho tài khoản của bạn";
            var message = $"Xin chào {user.FullName ?? user.UserName},<br/><br/>" + // Sử dụng FullName nếu có, nếu không thì UserName
                          $"Mã xác nhận mới của bạn là: <strong>{newCode}</strong>.<br/><br/>" +
                          $"Mã này sẽ hết hạn trong 60 giây. Vui lòng nhập mã này trên trang xác nhận.";

            try
            {
                // Enqueue job để gửi email
                _backgroundJobClient.Enqueue(() => _emailSender.SendEmailAsync(email, subject, message));
                _logger.LogInformation($"New verification email sending job enqueued for {email}");

                return new JsonResult(new { success = true, message = "Mã mới đã được gửi thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to enqueue new verification email job for {email}. Error: {ex.Message}");
                // Nếu không thể enqueue job, bạn có thể xóa bản ghi mã xác nhận vừa tạo
                _context.EmailConfirmations.Remove(newEmailConfirm);
                await _context.SaveChangesAsync(); // Lưu lại thay đổi (xóa bản ghi lỗi)

                return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi gửi mã xác nhận. Vui lòng thử lại!" });
            }
            // === KẾT THÚC SỬ DỤNG HANGFIRE ===
        }
    }
}