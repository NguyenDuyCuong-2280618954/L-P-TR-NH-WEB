using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WebThoiTrang.Models;

namespace WebThoiTrang.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager,
                           ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// Input posted from login form.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; } = new();

        /// <summary>
        /// List of external providers (Google, Facebook...).
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; private set; } = new List<AuthenticationScheme>();

        /// <summary>
        /// ReturnUrl is preserved across GET → POST so we can redirect after successful login.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        /* ----------------------------------------------------
         * GET: /Identity/Account/Login
         * --------------------------------------------------*/
        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);

            ReturnUrl = returnUrl ?? Url.Content("~/");

            // Đảm bảo cookie ngoài bị xoá để quá trình login sạch sẽ.
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        /* ----------------------------------------------------
         * POST: /Identity/Account/Login
         * --------------------------------------------------*/
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            _logger.LogInformation(">>> Login POST hit <<<");

            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
                return Page();

            var result = await _signInManager.PasswordSignInAsync(Input.Email,
                                                                  Input.Password,
                                                                  Input.RememberMe,
                                                                  lockoutOnFailure: false);
            // 2) Ghi log kết quả NGAY SAU khi nhận được 'result'
            _logger.LogInformation("Sign-in Result → Succeeded: {Succeeded}, LockedOut: {LockedOut}, NotAllowed: {NotAllowed}, Requires2FA: {Requires2FA}",
                                   result.Succeeded,
                                   result.IsLockedOut,
                                   result.IsNotAllowed,
                                   result.RequiresTwoFactor);


            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");

                // Chỉ redirect tới URL cục bộ để tránh open redirect.
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                return RedirectToPage("/Index");
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return RedirectToPage("./Lockout");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }
}
