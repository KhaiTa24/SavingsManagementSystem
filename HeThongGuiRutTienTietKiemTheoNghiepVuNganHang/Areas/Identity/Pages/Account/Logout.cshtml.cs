// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Services;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<LogoutModel> _logger;
        private readonly ILoginTrackingService _loginTrackingService;

        public LogoutModel(SignInManager<User> signInManager, ILogger<LogoutModel> logger, ILoginTrackingService loginTrackingService)
        {
            _signInManager = signInManager;
            _logger = logger;
            _loginTrackingService = loginTrackingService;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            // Lấy thông tin user trước khi đăng xuất
            var user = await _signInManager.UserManager.GetUserAsync(User);
            
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            
            // Theo dõi đăng xuất
            if (user != null)
            {
                await _loginTrackingService.TrackLogoutAsync(user.Id);
            }
            
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be a redirect so that the browser performs a new
                // request and the identity for the user gets updated.
                return RedirectToPage();
            }
        }
    }
}
