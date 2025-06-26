// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
// Có thể không cần nếu không dùng IEmailSender mặc định của Identity UI
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using WebThoiTrang.Models.Entity; // Đảm bảo đúng namespace cho ApplicationUser và ApplicationDbContext
using WebThoiTrang.Services; // <-- Thêm dòng này để sử dụng IEmailSender tùy chỉnh
using Hangfire; // <-- THÊM DÒNG NÀY để sử dụng Hangfire

namespace WebThoiTrang.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender; // <-- Đảm bảo đã inject EmailSender tùy chỉnh của bạn
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient; // <-- THÊM: Inject Hangfire Background Job Client


        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender, // <-- THÊM: IEmailSender
            ApplicationDbContext context,
            IBackgroundJobClient backgroundJobClient) // <-- THÊM: IBackgroundJobClient
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender; // <-- KHỞI TẠO: IEmailSender
            _context = context;
            _backgroundJobClient = backgroundJobClient; // <-- KHỞI TẠO: IBackgroundJobClient
        }

        /// <summary>
        ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///       directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///       directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///       directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///       directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Required(ErrorMessage = "Họ và Tên là bắt buộc.")]
            [Display(Name = "Họ và Tên")]
            public string FullName { get; set; }

            /// <summary>
            ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///       directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Email là bắt buộc.")]
            [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///       directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
            [StringLength(100, ErrorMessage = "Mật khẩu {0} phải có ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }

            /// <summary>
            ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///       directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu")]
            [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
            public string ConfirmPassword { get; set; }

            public string? Role { set; get; }
            [ValidateNever]
            public IEnumerable<SelectListItem> RoleList { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            // Kiểm tra và tạo các vai trò nếu chúng chưa tồn tại
            if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();
            }

            // Khởi tạo đối tượng Input và populate RoleList
            Input = new()
            {
                RoleList = _roleManager.Roles.Select(x => x.Name).Select(i => new SelectListItem
                {
                    Text = i,
                    Value = i
                })
            };
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = CreateUser();
                user.FullName = Input.FullName;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // === ĐÃ SỬA: GÁN ConfirmationCodeSentTime TRƯỚC CreateAsync ===
                // Đặt thời gian tạo tài khoản ban đầu cho mục đích dọn dẹp bởi Background Service
                user.ConfirmationCodeSentTime = DateTime.UtcNow;
                // === KẾT THÚC SỬA ===

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Logic thêm vai trò nếu có
                    if (!string.IsNullOrEmpty(Input.Role))
                    {
                        await _userManager.AddToRoleAsync(user, Input.Role);
                    }
                    else
                    {
                        // Nếu không chọn vai trò, mặc định là Customer
                        await _userManager.AddToRoleAsync(user, SD.Role_Customer);
                    }

                    // Tạo mã xác nhận 6 số
                    var code = new Random().Next(100000, 999999).ToString();
                    Console.WriteLine($"[DEBUG] Verification code for {Input.Email}: {code}");

                    // Lưu mã xác nhận vào bảng EmailConfirmations
                    var emailConfirm = new EmailConfirmation
                    {
                        UserId = await _userManager.GetUserIdAsync(user),
                        Email = Input.Email,
                        Code = code,
                        Expiration = DateTime.UtcNow.AddSeconds(60), // Mã này chỉ có hiệu lực 60 giây
                        IsUsed = false // Đảm bảo thuộc tính này được đặt
                    };

                    _context.EmailConfirmations.Add(emailConfirm);
                    // Luôn lưu vào DB TRƯỚC KHI enqueue job, để đảm bảo mã đã có trong DB
                    await _context.SaveChangesAsync();

                    // === SỬ DỤNG HANGFIRE ĐỂ GỬI EMAIL ===
                    var subject = "Mã xác nhận đăng ký tài khoản của bạn";
                    var message = $"Xin chào {Input.FullName},<br/><br/>" +
                                  $"Mã xác nhận đăng ký tài khoản của bạn là: <strong>{code}</strong>.<br/><br/>" +
                                  $"Mã này sẽ hết hạn trong 60 giây. Vui lòng nhập mã này trên trang xác nhận để hoàn tất đăng ký.";

                    try
                    {
                        // Enqueue job để gửi email. Hangfire sẽ gọi _emailSender.SendEmailAsync trong nền.
                        _backgroundJobClient.Enqueue(() => _emailSender.SendEmailAsync(Input.Email, subject, message));
                        _logger.LogInformation($"Verification email sending job enqueued for {Input.Email}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to enqueue verification email job for {Input.Email}. Error: {ex.Message}");
                        // Nếu việc enqueue thất bại, tài khoản và mã xác nhận vẫn được tạo.
                        // Thông báo cho người dùng rằng email có thể bị chậm trễ
                        // hoặc cung cấp tùy chọn gửi lại mã.
                        TempData["WarningMessage"] = "Đăng ký thành công nhưng không thể gửi email xác nhận ngay lập tức. Vui lòng kiểm tra hộp thư spam hoặc yêu cầu gửi lại mã sau ít phút.";
                        // Không xóa user hay emailConfirm ở đây vì lỗi này không phải lỗi tạo tài khoản hay gửi email, mà là lỗi đưa vào hàng đợi.
                    }
                    // === KẾT THÚC SỬ DỤNG HANGFIRE ===

                    // Chuyển hướng tới trang nhập mã xác nhận
                    TempData["Email"] = Input.Email; // Lưu email để chuyển sang trang xác nhận (dự phòng)

                    // === ĐÃ SỬA LỖI QUAN TRỌNG: THÊM THAM SỐ EMAIL VÀO REDIRECTTO PAGE ===
                    return RedirectToPage("/Account/VerifyCode", new { email = Input.Email });
                    // === KẾT THÚC SỬA LỖI ===
                }

                // Nếu lỗi tạo user (result.Succeeded là false)
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Nếu form bị lỗi (ModelState.IsValid là false)
            // Cần khởi tạo lại RoleList nếu ModelState không hợp lệ để tránh lỗi null reference trên Razor Page
            // Đảm bảo _roleManager đã được inject nếu bạn đang sử dụng RoleList
            if (Input != null && _roleManager != null)
            {
                Input.RoleList = _roleManager.Roles.Select(x => x.Name).Select(i => new SelectListItem
                {
                    Text = i,
                    Value = i
                });
            }
            return Page();
        }


        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }

    }
}