﻿﻿﻿﻿﻿﻿﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Services;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ILoginTrackingService _loginTrackingService;

        public LoginModel(SignInManager<User> signInManager, UserManager<User> userManager, ILogger<LoginModel> logger, ILoginTrackingService loginTrackingService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _loginTrackingService = loginTrackingService;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật Khẩu")]
            public string Password { get; set; }

            [Display(Name = "Ghi nhớ tôi?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // Enable password failures to trigger account lockout
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    
                    // Lấy thông tin user để hiển thị thông báo
                    var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
                    
                    // Theo dõi đăng nhập thành công
                    await _loginTrackingService.TrackSuccessfulLoginAsync(user);
                    
                    // Kiểm tra xem người dùng đã thiết lập PIN chưa
                    if (user.Role == "KhachHang" && !user.IsPinSetup)
                    {
                        // Nếu là khách hàng và chưa thiết lập PIN, chuyển hướng đến trang thiết lập PIN
                        TempData["InfoMessage"] = "Vui lòng thiết lập Mã PIN Digital OTP để tiếp tục sử dụng dịch vụ.";
                        return RedirectToAction("SetupPin", "Customer");
                    }
                    
                    TempData["LoginSuccess"] = $"Chào mừng {user.HoTen}! Bạn đã đăng nhập thành công.";

                    // Điều hướng theo vai trò
                    if (await _signInManager.UserManager.IsInRoleAsync(user, "NhanVienGiaoDich"))
                    {
                        return RedirectToAction("Overview", "Transaction");
                    }
                    else if (await _signInManager.UserManager.IsInRoleAsync(user, "NhanVienQuanLy"))
                    {
                        return RedirectToAction("Overview", "Management");
                    }
                    else if (await _signInManager.UserManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Overview", "Admin");
                    }
                    else
                    {
                        // Mặc định là Khách hàng
                        return RedirectToAction("Dashboard", "Customer");
                    }
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa do đăng nhập thất bại quá 5 lần. Vui lòng thử lại sau 15 phút hoặc liên hệ quản trị viên.");
                    return Page();
                }
                else
                {
                    // Theo dõi đăng nhập thất bại
                    var httpContext = HttpContext;
                    var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                    await _loginTrackingService.TrackFailedLoginAsync(Input.Email, ipAddress);
                    
                    // Check how many failed attempts the user has made
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        var accessFailedCount = await _userManager.GetAccessFailedCountAsync(user);
                        var remainingAttempts = 5 - accessFailedCount;
                        
                        if (remainingAttempts > 0)
                        {
                            ModelState.AddModelError(string.Empty, $"Email hoặc mật khẩu không chính xác. Bạn còn {remainingAttempts} lần thử trước khi tài khoản bị khóa.");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa do đăng nhập thất bại quá 5 lần. Vui lòng thử lại sau 15 phút hoặc liên hệ quản trị viên.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                    }
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
