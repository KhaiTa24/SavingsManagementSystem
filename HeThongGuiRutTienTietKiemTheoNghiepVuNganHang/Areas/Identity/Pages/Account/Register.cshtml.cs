// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Extensions;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public RegisterModel(
            UserManager<User> userManager,
            IUserStore<User> userStore,
            SignInManager<User> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
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
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Required(ErrorMessage = "Họ tên là bắt buộc")]
            [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
            [Display(Name = "Họ và Tên")]
            public string HoTen { get; set; }

            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Số CCCD/CMND là bắt buộc")]
            [StringLength(12, MinimumLength = 12, ErrorMessage = "CCCD/CMND phải đúng 12 số")]
            [RegularExpression(@"^[0-9]{12}$", ErrorMessage = "CCCD/CMND phải đúng 12 chữ số")]
            [Display(Name = "Số CCCD/CMND")]
            public string CCCD { get; set; }

            [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            [RegularExpression(@"^(0)[0-9]{9}$", ErrorMessage = "Số điện thoại phải có 10 số và bắt đầu bằng 0")]
            [Display(Name = "Số Điện Thoại")]
            public string SDT { get; set; }

            [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
            [DataType(DataType.Date)]
            [Display(Name = "Ngày Sinh")]
            [CustomAgeValidation(15, ErrorMessage = "Bạn phải đủ 15 tuổi để đăng ký tài khoản")]
            public DateTime NgaySinh { get; set; }

            [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
            [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
            [Display(Name = "Địa Chỉ")]
            public string DiaChi { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập nghề nghiệp")]
            [Display(Name = "Nghề nghiệp")]
            public string NgheNghiep { get; set; }
     

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự và tối đa {1} ký tự", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật Khẩu")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$", 
                ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
            [DataType(DataType.Password)]
            [Display(Name = "Xác Nhận Mật Khẩu")]
            [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp")]
            public string ConfirmPassword { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            
            // Validate age (must be at least 15 years old)
            var age = DateTime.Today.Year - Input.NgaySinh.Year;
            if (Input.NgaySinh.Date > DateTime.Today.AddYears(-age)) age--;
            
            if (age < 15)
            {
                ModelState.AddModelError(string.Empty, "Bạn phải đủ 15 tuổi để đăng ký tài khoản");
            }
            
            // Validate CCCD/CMND format and rules
            if (!string.IsNullOrEmpty(Input.CCCD) && Input.CCCD.Length == 12)
            {
                // Check province code (first 3 digits)
                string provinceCode = Input.CCCD.Substring(0, 3);
                bool isProvinceValid = IsValidProvinceCode(provinceCode);
                
                // Check gender/year of birth (4th digit)
                char fourthDigit = Input.CCCD[3];
                int birthYear = Input.NgaySinh.Year;
                
                bool isBirthYearValid = true;
                if (birthYear >= 1900 && birthYear < 2000 && (fourthDigit != '0' && fourthDigit != '1'))
                {
                    isBirthYearValid = false;
                }
                else if (birthYear >= 2000 && birthYear <= 2099 && fourthDigit != '2')
                {
                    isBirthYearValid = false;
                }
                
                // If any validation fails, show general error
                if (!isProvinceValid || !isBirthYearValid)
                {
                    ModelState.AddModelError(string.Empty, "CCCD không hợp lệ.");
                }
            }
            
            if (ModelState.IsValid)
            {
                var user = CreateUser();
                
                // Set customer information
                user.HoTen = Input.HoTen;
                user.CCCD = Input.CCCD;
                user.SDT = Input.SDT;
                user.NgaySinh = Input.NgaySinh;
                user.DiaChi = Input.DiaChi;
                user.NgheNghiep = Input.NgheNghiep;
                user.Role = "KhachHang"; // Default role for new registrations
                user.PhoneNumber = Input.SDT;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                
                // Password is automatically hashed by Identity
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Add user to role KhachHang (Identity roles)
                    await _userManager.AddToRoleAsync(user, "KhachHang");

                    // Auto-create bank account for the new user
                    var rand = new Random();
                    string soTK = null;
                    for (int i = 0; i < 10; i++)
                    {
                        var candidate = $"{rand.Next(100000, 999999)}{rand.Next(100000, 999999)}"; // 12 digits
                        if (!_context.TaiKhoanNganHangs.Any(x => x.SoTaiKhoan == candidate))
                        {
                            soTK = candidate;
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(soTK))
                    {
                        // fallback in extremely rare case
                        soTK = DateTime.UtcNow.Ticks.ToString().Substring(0, 12);
                    }

                    var newAccount = new TaiKhoanNganHang
                    {
                        SoTaiKhoan = soTK,
                        MaKH = user.Id,
                        SoDu = 0M,
                        TrangThai = "Hoạt động",
                        NgayMoTaiKhoan = DateTime.Now
                    };
                    _context.TaiKhoanNganHangs.Add(newAccount);
                    await _context.SaveChangesAsync();

                    // Success message with account number and redirect to login
                    TempData["SuccessMessage"] = $"Đăng ký tài khoản thành công! Số tài khoản của bạn: {soTK}. Chào mừng {Input.HoTen}. Vui lòng đăng nhập để tiếp tục.";
                    return RedirectToPage("./Login", new { returnUrl = returnUrl });
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private User CreateUser()
        {
            try
            {
                return Activator.CreateInstance<User>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(User)}'. " +
                    $"Ensure that '{nameof(User)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<User>)_userStore;
        }
        
        private bool IsValidProvinceCode(string provinceCode)
        {
            // List of valid province codes in Vietnam
            var validProvinces = new HashSet<string>
            {
                "001", "002", "004", "006", "008", "010", "011", "012", "014", "015", "017", "019",
                "020", "022", "024", "025", "026", "027", "028", "029", "030", "031", "033", "034",
                "035", "036", "037", "038", "040", "042", "044", "045", "046", "048", "049", "051",
                "052", "054", "056", "058", "060", "062", "064", "066", "067", "068", "070", "072",
                "074", "075", "077", "079", "080", "082", "083", "084", "086", "087", "089", "091",
                "092", "093", "094", "095", "096", "097", "098", "099"
            };
            
            return validProvinces.Contains(provinceCode);
        }
    }
}
