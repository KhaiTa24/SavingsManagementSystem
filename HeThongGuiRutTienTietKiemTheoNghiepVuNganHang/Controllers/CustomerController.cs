using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Services;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Extensions;
using System.Net.Mail;
using System.Net;
using MailKit.Net.Smtp;
using MimeKit;
using Newtonsoft.Json;
using System.Security.Cryptography;


namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Controllers
{
    [Authorize(Roles = "KhachHang")]
    public class CustomerController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerController> _logger;
        private readonly ChatbotService _chatbotService;
        private readonly IConfiguration _configuration;

        public CustomerController(
            UserManager<User> userManager,
            ApplicationDbContext context,
            ILogger<CustomerController> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _chatbotService = new ChatbotService();
            _configuration = configuration;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy tài khoản ngân hàng của khách hàng cùng với giao dịch ngân hàng
            var bankAccounts = await _context.TaiKhoanNganHangs
                .Where(t => t.MaKH == user.Id)
                .Include(t => t.SoTietKiems)
                .Include(t => t.GiaoDichNganHangs)
                .ToListAsync();

            // Lấy giao dịch tiết kiệm gần đây
            var soTietKiemIds = bankAccounts.SelectMany(b => b.SoTietKiems.Select(s => s.MaSTK)).ToList();
            var recentSavingsTransactions = await _context.GiaoDichTietKiems
                .Where(g => g.MaSTK.HasValue && soTietKiemIds.Contains(g.MaSTK.Value))
                .OrderByDescending(g => g.NgayGD)
                .Take(5)
                .Include(g => g.SoTietKiem)
                .ToListAsync();

            // Lấy thông báo
            var notifications = await _context.ThongBaos
                .Where(t => t.MaKH == user.Id)
                .OrderByDescending(t => t.NgayGui)
                .Take(5)
                .ToListAsync();

            // Tính tổng tiền tiết kiệm
            decimal totalSavings = bankAccounts.Sum(b => b.SoTietKiems.Sum(s => s.SoTienGui));

            ViewBag.User = user;
            ViewBag.BankAccounts = bankAccounts;
            ViewBag.RecentSavingsTransactions = recentSavingsTransactions;
            ViewBag.Notifications = notifications;
            ViewBag.TotalSavings = totalSavings;
            ViewBag.TotalAccounts = bankAccounts.Count;
            ViewBag.TotalSavingsBooks = bankAccounts.Sum(b => b.SoTietKiems.Count);

            // Set thông báo đăng nhập thành công
            if (TempData["LoginSuccess"] != null)
            {
                ViewBag.LoginSuccess = TempData["LoginSuccess"];
            }

            return View();
        }

        [HttpPost]
        public IActionResult ProcessChatMessage(string message)
        {
            // Log để debug
            _logger.LogInformation($"Received message: '{message}'");
            
            // Kiểm tra null để tránh NullReferenceException
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogInformation("Message is null or whitespace");
                return Json(new { response = "Xin vui lòng nhập câu hỏi." });
            }
            
            string response = _chatbotService.ProcessUserMessage(message);
            _logger.LogInformation($"Response: '{response}'");
            return Json(new { response = response });
        }

        // Trang hồ sơ người dùng
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy tài khoản ngân hàng và sổ tiết kiệm của khách hàng
            var bankAccounts = await _context.TaiKhoanNganHangs
                .Where(t => t.MaKH == user.Id)
                .Include(t => t.SoTietKiems)
                .ToListAsync();

            // Tính tổng tiền tiết kiệm
            decimal totalSavings = bankAccounts.Sum(b => b.SoTietKiems.Sum(s => s.SoTienGui));

            // Xác định hạng khách hàng dựa trên tổng số tiền tiết kiệm
            string customerRank = DetermineCustomerRank(totalSavings);

            // Lấy danh sách lịch hẹn của khách hàng
            var appointments = await _context.LichHens
                .Include(l => l.LoaiDichVu)
                .Include(l => l.ChiNhanh)
                .Where(l => l.MaKH == user.Id)
                .OrderByDescending(l => l.ThoiGianTao)
                .ToListAsync();

            ViewBag.User = user;
            ViewBag.BankAccounts = bankAccounts;
            ViewBag.TotalSavings = totalSavings;
            ViewBag.CustomerRank = customerRank;
            ViewBag.Appointments = appointments;

            return View();
        }

        // Phương thức xác định hạng khách hàng
        private string DetermineCustomerRank(decimal totalSavings)
        {
            if (totalSavings >= 500000000) // 500 triệu
                return "Kim cương";
            else if (totalSavings >= 1000000000) // 100 triệu
                return "Vàng";
            else if (totalSavings >= 50000000) // 50 triệu
                return "Bạc";
            else if (totalSavings >= 10000000) // 10 triệu
                return "Đồng";
            else
                return "Thành viên";
        }

        // Chi tiết sổ tiết kiệm
        public async Task<IActionResult> SavingsBookDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy thông tin sổ tiết kiệm với các giao dịch liên quan
            var savingsBook = await _context.SoTietKiems
                .Include(s => s.TaiKhoanNganHang)
                .Include(s => s.GiaoDichTietKiems)
                .FirstOrDefaultAsync(s => s.MaSTK == id && s.TaiKhoanNganHang.MaKH == user.Id);

            if (savingsBook == null)
            {
                TempData["ErrorMessage"] = "Sổ tiết kiệm không tồn tại hoặc không thuộc về bạn.";
                return RedirectToAction("Profile");
            }

            // Tính tổng tiền hiện tại trong sổ
            decimal currentAmount = savingsBook.SoTienGui + savingsBook.GiaoDichTietKiems
                .Where(g => g.LoaiGD == "Gửi" && g.TrangThaiGD == "DaDuyet")
                .Sum(g => g.SoTien) -
                savingsBook.GiaoDichTietKiems
                .Where(g => g.LoaiGD == "Rút" && g.TrangThaiGD == "DaDuyet")
                .Sum(g => g.SoTien);

            ViewBag.SavingsBook = savingsBook;
            ViewBag.CurrentAmount = currentAmount;

            return View();
        }

        // Xử lý đáo hạn/tất toán sổ tiết kiệm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseSavingsBook(int id, bool earlyWithdrawal = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy thông tin sổ tiết kiệm
            var savingsBook = await _context.SoTietKiems
                .Include(s => s.TaiKhoanNganHang)
                .FirstOrDefaultAsync(s => s.MaSTK == id && s.TaiKhoanNganHang.MaKH == user.Id);

            if (savingsBook == null)
            {
                TempData["ErrorMessage"] = "Sổ tiết kiệm không tồn tại hoặc không thuộc về bạn.";
                return RedirectToAction("Profile");
            }

            // Kiểm tra trạng thái sổ tiết kiệm
            if (savingsBook.TrangThai != "Đang hoạt động")
            {
                TempData["ErrorMessage"] = "Sổ tiết kiệm không ở trạng thái hoạt động.";
                return RedirectToAction("SavingsBookDetails", new { id = id });
            }

            // Kiểm tra ngày đáo hạn nếu không phải tất toán trước hạn
            bool isMatured = DateTime.Now >= savingsBook.NgayDaoHan;
            if (!earlyWithdrawal && !isMatured)
            {
                TempData["ErrorMessage"] = "Chưa đến ngày đáo hạn. Nếu muốn tất toán trước hạn, vui lòng chọn tùy chọn tất toán trước hạn.";
                return RedirectToAction("SavingsBookDetails", new { id = id });
            }

            // Lưu thông tin vào session để sử dụng sau khi xác thực OTP
            HttpContext.Session.SetInt32("CloseSavingsBookId", id);
            HttpContext.Session.SetBool("CloseSavingsBookEarlyWithdrawal", earlyWithdrawal);

            // Chuyển hướng đến trang yêu cầu nhập PIN
            return RedirectToAction("RequestPin", new { purpose = "CloseSavingsAccount" });
        }

        // Trang đổi mật khẩu
        public IActionResult ChangePassword()
        {
            // Chuyển hướng đến trang yêu cầu nhập PIN trước khi đổi mật khẩu
            return RedirectToAction("RequestPinForPasswordChange");
        }
        
        // GET: Yêu cầu nhập PIN trước khi đổi mật khẩu
        public IActionResult RequestPinForPasswordChange()
        {
            return View();
        }
        
        // POST: Xác minh PIN trước khi đổi mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPinForPasswordChange(string pin)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            // Kiểm tra PIN
            if (string.IsNullOrEmpty(pin) || pin.Length != 6 || !pin.All(char.IsDigit))
            {
                TempData["ErrorMessage"] = "Mã PIN không hợp lệ. Vui lòng nhập mã PIN 6 chữ số.";
                return View();
            }
            
            // Xác minh PIN
            if (!VerifyPin(pin, user.DigitalPin))
            {
                TempData["ErrorMessage"] = "Mã PIN không chính xác. Vui lòng thử lại.";
                return View();
            }
            
            // Nếu PIN đúng, chuyển hướng đến trang đổi mật khẩu
            return RedirectToAction("ChangePasswordForm");
        }
        
        // GET: Form đổi mật khẩu (sau khi đã xác minh PIN)
        public IActionResult ChangePasswordForm()
        {
            return View();
        }
        
        // POST: Đổi mật khẩu (sau khi đã xác minh PIN)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePasswordForm(string currentPassword, string newPassword, string confirmNewPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Kiểm tra các trường nhập
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmNewPassword))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin.";
                return View();
            }

            // Kiểm tra mật khẩu mới và xác nhận mật khẩu mới
            if (newPassword != confirmNewPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới và xác nhận mật khẩu không khớp.";
                return View();
            }

            // Kiểm tra độ phức tạp của mật khẩu mới
            if (newPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return View();
            }

            // Kiểm tra mật khẩu hiện tại
            var result = await _userManager.CheckPasswordAsync(user, currentPassword);
            if (!result)
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng.";
                return View();
            }

            // Đổi mật khẩu
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (changePasswordResult.Succeeded)
            {
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
                return RedirectToAction("Profile");
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đổi mật khẩu.";
                return View();
            }
        }

        // Giới thiệu các gói tiết kiệm
        public async Task<IActionResult> SavingsPackages()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            // Lấy danh sách gói tiết kiệm đang hoạt động
            var packages = await _context.GoiTietKiems
                .Where(g => g.TrangThai == "Active")
                .OrderBy(g => g.KyHanThang)
                .ToListAsync();

            ViewBag.User = user;
            ViewBag.Packages = packages;
            return View();
        }

        // Mở sổ tiết kiệm
        public async Task<IActionResult> OpenSavingsBook(int? packageId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            // Lấy danh sách tài khoản ngân hàng
            var bankAccounts = await _context.TaiKhoanNganHangs
                .Where(t => t.MaKH == user.Id && t.TrangThai == "Hoạt động")
                .ToListAsync();

            // Lấy tất cả các gói tiết kiệm active
            var packages = await _context.GoiTietKiems
                .Where(g => g.TrangThai == "Active")
                .OrderBy(g => g.KyHanThang)
                .ToListAsync();

            ViewBag.BankAccounts = bankAccounts;
            ViewBag.Packages = packages;
            ViewBag.SelectedPackageId = packageId; // Nếu có packageId từ trang trước
            ViewBag.User = user;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestOpenSavingsBook(int maGoiTietKiem, string soTaiKhoan, decimal soTienGui, string loaiTaiTuc)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Kiểm tra loại tái tục
            if (string.IsNullOrEmpty(loaiTaiTuc))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn loại tái tục.";
                return RedirectToAction("OpenSavingsBook", new { packageId = maGoiTietKiem });
            }

            // Kiểm tra số tiền tối thiểu và bội số
            if (soTienGui < 200000)
            {
                TempData["ErrorMessage"] = "Số tiền gửi tối thiểu là 200,000 VNĐ.";
                return RedirectToAction("OpenSavingsBook", new { packageId = maGoiTietKiem });
            }

            if (soTienGui % 100000 != 0)
            {
                TempData["ErrorMessage"] = "Số tiền gửi phải là bội số của 100,000 VNĐ.";
                return RedirectToAction("OpenSavingsBook", new { packageId = maGoiTietKiem });
            }

            // Kiểm tra tài khoản ngân hàng
            var bankAccount = await _context.TaiKhoanNganHangs
                .FirstOrDefaultAsync(t => t.SoTaiKhoan == soTaiKhoan && t.MaKH == user.Id);

            if (bankAccount == null)
            {
                TempData["ErrorMessage"] = "Tài khoản ngân hàng không tồn tại hoặc không thuộc về bạn.";
                return RedirectToAction("OpenSavingsBook", new { packageId = maGoiTietKiem });
            }

            // Kiểm tra gói tiết kiệm
            var package = await _context.GoiTietKiems.FindAsync(maGoiTietKiem);
            if (package == null || package.TrangThai != "Active")
            {
                TempData["ErrorMessage"] = "Gói tiết kiệm không tồn tại hoặc không còn áp dụng.";
                return RedirectToAction("SavingsPackages");
            }

            // Kiểm tra số tiền tối thiểu theo gói
            if (soTienGui < package.SoTienToiThieu)
            {
                TempData["ErrorMessage"] = $"Số tiền gửi phải lớn hơn hoặc bằng {string.Format("{0:N0}", package.SoTienToiThieu)} đ.";
                return RedirectToAction("OpenSavingsBook", new { packageId = maGoiTietKiem });
            }

            // Kiểm tra số dư tài khoản
            if (bankAccount.SoDu < soTienGui)
            {
                TempData["ErrorMessage"] = "Số dư tài khoản không đủ để thực hiện giao dịch.";
                return RedirectToAction("OpenSavingsBook", new { packageId = maGoiTietKiem });
            }

            // Tính ngày đáo hạn
            var ngayDaoHan = DateTime.Now.AddMonths(package.KyHanThang);

            // Lưu thông tin giao dịch vào Session để xử lý tiếp
            var transactionData = new
            {
                MaGoiTietKiem = maGoiTietKiem,
                SoTaiKhoan = soTaiKhoan,
                SoTienGui = soTienGui,
                LoaiTaiTuc = loaiTaiTuc,  // Lưu loại tái tục được chọn bởi khách hàng
                NgayDaoHan = ngayDaoHan
            };

            HttpContext.Session.SetString("OpenSavingsTransactionData", JsonConvert.SerializeObject(transactionData));

            // Tạo mã OTP và lưu vào Session
            string otpCode = GenerateOTP();
            // Chuyển hướng đến trang yêu cầu PIN trước khi nhận OTP
            return RedirectToAction("RequestPin", new { purpose = "OpenSavingsAccount" });
        }

        // GET: Lịch sử giao dịch ngân hàng
        public async Task<IActionResult> BankTransactionHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy tài khoản ngân hàng của khách hàng
            var bankAccounts = await _context.TaiKhoanNganHangs
                .Where(t => t.MaKH == user.Id)
                .ToListAsync();

            if (!bankAccounts.Any())
            {
                TempData["ErrorMessage"] = "Bạn chưa có tài khoản ngân hàng nào.";
                return RedirectToAction("Dashboard");
            }

            // Lấy số tài khoản
            var accountNumbers = bankAccounts.Select(t => t.SoTaiKhoan).ToList();

            // Lấy lịch sử giao dịch ngân hàng
            var bankTransactions = await _context.GiaoDichNganHangs
                .Where(g => accountNumbers.Contains(g.SoTaiKhoan))
                .OrderByDescending(g => g.NgayGD)
                .Take(50)
                .ToListAsync();

            ViewBag.BankAccounts = bankAccounts;
            ViewBag.BankTransactions = bankTransactions;
            return View();
        }

        // GET: Quản lý giao dịch tại quầy
        public async Task<IActionResult> CounterTransactions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy tài khoản ngân hàng của khách hàng
            var bankAccounts = await _context.TaiKhoanNganHangs
                .Where(t => t.MaKH == user.Id)
                .Include(t => t.SoTietKiems)
                .ToListAsync();

            if (!bankAccounts.Any())
            {
                TempData["ErrorMessage"] = "Bạn chưa có tài khoản ngân hàng nào.";
                return RedirectToAction("Dashboard");
            }

            // Lấy số tài khoản
            var accountNumbers = bankAccounts.Select(t => t.SoTaiKhoan).ToList();

            // Lấy danh sách mã sổ tiết kiệm
            var savingsBookIds = bankAccounts.SelectMany(b => b.SoTietKiems.Select(s => s.MaSTK)).ToList();

            // Lấy lịch sử giao dịch ngân hàng
            var bankTransactions = await _context.GiaoDichNganHangs
                .Where(g => accountNumbers.Contains(g.SoTaiKhoan))
                .OrderByDescending(g => g.NgayGD)
                .ToListAsync();

            // Lấy lịch sử giao dịch sổ tiết kiệm
            var savingsTransactions = await _context.GiaoDichTietKiems
                .Where(g => g.MaSTK.HasValue && savingsBookIds.Contains(g.MaSTK.Value))
                .OrderByDescending(g => g.NgayGD)
                .Include(g => g.SoTietKiem)
                    .ThenInclude(s => s.TaiKhoanNganHang)
                .ToListAsync();

            // Lấy danh sách sổ tiết kiệm (để hiển thị thông tin mở sổ)
            var savingsBooks = bankAccounts.SelectMany(b => b.SoTietKiems).ToList();

            ViewBag.BankAccounts = bankAccounts;
            ViewBag.BankTransactions = bankTransactions;
            ViewBag.SavingsTransactions = savingsTransactions;
            ViewBag.SavingsBooks = savingsBooks;
            return View();
        }

        // GET: Chi tiết giao dịch ngân hàng
        public async Task<IActionResult> BankTransactionDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy giao dịch ngân hàng với id cụ thể và kiểm tra quyền truy cập
            var transaction = await _context.GiaoDichNganHangs
                .Include(g => g.TaiKhoanNganHang)
                    .ThenInclude(t => t.KhachHang)
                .Include(g => g.NhanVien)
                .FirstOrDefaultAsync(g => g.MaGD == id && g.TaiKhoanNganHang.MaKH == user.Id);

            if (transaction == null)
            {
                TempData["ErrorMessage"] = "Giao dịch không tồn tại hoặc bạn không có quyền truy cập.";
                return RedirectToAction("CounterTransactions");
            }

            return View(transaction);
        }

        // GET: Chi tiết giao dịch tiết kiệm
        public async Task<IActionResult> SavingsTransactionDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy giao dịch tiết kiệm với id cụ thể và kiểm tra quyền truy cập
            var transaction = await _context.GiaoDichTietKiems
                .Include(g => g.SoTietKiem)
                    .ThenInclude(s => s.TaiKhoanNganHang)
                        .ThenInclude(t => t.KhachHang)
                .Include(g => g.NhanVien)
                .FirstOrDefaultAsync(g => g.MaGD == id && g.SoTietKiem.TaiKhoanNganHang.MaKH == user.Id);

            if (transaction == null)
            {
                TempData["ErrorMessage"] = "Giao dịch không tồn tại hoặc bạn không có quyền truy cập.";
                return RedirectToAction("CounterTransactions");
            }

            return View(transaction);
        }

        // GET: Chi tiết sổ tiết kiệm
        public async Task<IActionResult> SavingsBookDetailsForCounter(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy sổ tiết kiệm với id cụ thể và kiểm tra quyền truy cập
            var savingsBook = await _context.SoTietKiems
                .Include(s => s.TaiKhoanNganHang)
                    .ThenInclude(t => t.KhachHang)
                .Include(s => s.GiaoDichTietKiems)
                .FirstOrDefaultAsync(s => s.MaSTK == id && s.TaiKhoanNganHang.MaKH == user.Id);

            if (savingsBook == null)
            {
                TempData["ErrorMessage"] = "Sổ tiết kiệm không tồn tại hoặc bạn không có quyền truy cập.";
                return RedirectToAction("CounterTransactions");
            }

            return View("~/Views/Customer/SavingsBookDetails.cshtml", savingsBook);
        }

        // GET: Lịch sử giao dịch sổ tiết kiệm
        public async Task<IActionResult> SavingsTransactionHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy tài khoản ngân hàng của khách hàng
            var bankAccounts = await _context.TaiKhoanNganHangs
                .Where(t => t.MaKH == user.Id)
                .Include(t => t.SoTietKiems)
                .ToListAsync();

            if (!bankAccounts.Any())
            {
                TempData["ErrorMessage"] = "Bạn chưa có tài khoản ngân hàng nào.";
                return RedirectToAction("Dashboard");
            }

            // Lấy danh sách mã sổ tiết kiệm
            var savingsBookIds = bankAccounts.SelectMany(b => b.SoTietKiems.Select(s => s.MaSTK)).ToList();

            // Lấy lịch sử giao dịch sổ tiết kiệm
            var savingsTransactions = await _context.GiaoDichTietKiems
                .Where(g => g.MaSTK.HasValue && savingsBookIds.Contains(g.MaSTK.Value))
                .OrderByDescending(g => g.NgayGD)
                .Take(100)
                .Include(g => g.SoTietKiem)
                    .ThenInclude(s => s.TaiKhoanNganHang)
                .ToListAsync();

            ViewBag.BankAccounts = bankAccounts;
            ViewBag.SavingsTransactions = savingsTransactions;
            return View();
        }
        
        // Phương thức tạo và gửi OTP
        private async Task<string> GenerateAndSendOTP(string email, string userId, string purpose)
        {
            // Tạo mã OTP 6 chữ số
            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();
            
            // Lưu thông tin OTP vào database
            var otp = new OTPVerification
            {
                UserId = userId,
                Email = email,
                OTPCode = otpCode,
                CreatedAt = DateTime.Now,
                ExpiryTime = DateTime.Now.AddSeconds(90), // Hết hạn sau 90 giây (1.5 phút)
                IsUsed = false,
                Purpose = purpose
            };
            
            _context.OTPVerifications.Add(otp);
            await _context.SaveChangesAsync();
            
            // Gửi email chứa mã OTP
            await SendOTPEmail(email, otpCode);
            
            return otpCode;
        }
        
        // Phương thức tạo mã OTP
        private string GenerateOTP()
        {
            // Tạo mã OTP 6 chữ số
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
        
        // Phương thức gửi email chứa mã OTP
        private async Task SendOTPEmail(string email, string otpCode)
        {
            // Kiểm tra cấu hình SMTP
            var smtpHost = _configuration["Smtp:Host"];
            var smtpPort = _configuration["Smtp:Port"];
            var smtpEnableSsl = _configuration["Smtp:EnableSsl"];
            var smtpUsername = _configuration["Smtp:Username"];
            var smtpPassword = _configuration["Smtp:Password"];
            
            try
            {
                
                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpPort) || 
                    string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("SMTP configuration is incomplete. Falling back to logging OTP.");
                    _logger.LogInformation($"SMTP Config - Host: {smtpHost}, Port: {smtpPort}, Username: {smtpUsername}");
                    _logger.LogInformation($"OTP Code for {email}: {otpCode}");
                    return;
                }
                
                _logger.LogInformation($"Attempting to connect to SMTP server: {smtpHost}:{smtpPort} with SSL: {smtpEnableSsl}");
                _logger.LogInformation($"SMTP Username: {smtpUsername}");
                
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_configuration["Smtp:FromName"] ?? "BankSave", _configuration["Smtp:FromAddress"] ?? smtpUsername));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = "Mã xác thực OTP của bạn";
                
                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = $"<h2>Mã xác thực OTP</h2>" +
                                  $"<p>Mã OTP của bạn là: <strong>{otpCode}</strong></p>" +
                                  $"<p>Mã này sẽ hết hạn sau 90 giây (1.5 phút).</p>" +
                                  "<p>Xin vui lòng không chia sẻ mã này với bất kỳ ai.</p>";
                message.Body = bodyBuilder.ToMessageBody();
                
                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    // Xác định loại kết nối dựa trên cổng
                    int port = int.Parse(smtpPort);
                    MailKit.Security.SecureSocketOptions options;
                    
                    if (port == 465)
                    {
                        // Sử dụng SslOnConnect cho cổng 465
                        options = MailKit.Security.SecureSocketOptions.SslOnConnect;
                    }
                    else if (port == 587)
                    {
                        // Sử dụng StartTls cho cổng 587
                        options = MailKit.Security.SecureSocketOptions.StartTls;
                    }
                    else
                    {
                        // Mặc định
                        options = MailKit.Security.SecureSocketOptions.Auto;
                    }
                    
                    _logger.LogInformation($"Connecting with options: {options}");
                    
                    // Kết nối đến máy chủ SMTP
                    await client.ConnectAsync(smtpHost, port, options);
                    
                    _logger.LogInformation("Connected to SMTP server successfully");
                    
                    // Xác thực
                    _logger.LogInformation("Attempting authentication...");
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);
                    _logger.LogInformation("Authentication successful");
                    
                    // Gửi email
                    await client.SendAsync(message);
                    
                    // Ngắt kết nối
                    await client.DisconnectAsync(true);
                    
                    _logger.LogInformation($"OTP email sent to {email}");
                }
            }
            catch (MailKit.Security.AuthenticationException authEx)
            {
                _logger.LogError(authEx, $"Failed to authenticate with SMTP server when sending OTP email to {email}. Please check your SMTP username and password configuration. For Gmail, ensure you're using an App Password if 2FA is enabled, or that 'Less secure app access' is enabled. Error: {authEx.Message}");
                _logger.LogError($"SMTP Configuration - Host: {smtpHost}, Port: {smtpPort}, Username: {smtpUsername}");
                // Vẫn log mã OTP để phòng trường hợp gửi email thất bại
                _logger.LogInformation($"OTP Code for {email}: {otpCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send OTP email to {email}: {ex.Message}");
                // Vẫn log mã OTP để phòng trường hợp gửi email thất bại
                _logger.LogInformation($"OTP Code for {email}: {otpCode}");
            }
        }
        
        // GET: Trang xác thực OTP
        public async Task<IActionResult> VerifyOTP(string purpose)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            
            ViewBag.Purpose = purpose;
            return View();
        }
        
        // POST: Xác thực OTP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(string otpCode, string purpose)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            
            // Kiểm tra mã OTP
            var otp = await _context.OTPVerifications
                .Where(o => o.Email == user.Email && o.OTPCode == otpCode && o.Purpose == purpose && !o.IsUsed && o.ExpiryTime > DateTime.Now)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
                
            if (otp == null)
            {
                TempData["ErrorMessage"] = "Mã OTP không hợp lệ hoặc đã hết hạn.";
                return View();
            }
            
            // Đánh dấu OTP đã sử dụng
            otp.IsUsed = true;
            _context.OTPVerifications.Update(otp);
            await _context.SaveChangesAsync();
            
            // Thực hiện hành động tương ứng với mục đích
            if (purpose == "OpenSavingsBook")
            {
                // Lấy thông tin giao dịch từ session
                var transactionDataJson = HttpContext.Session.GetString("OpenSavingsTransactionData");
                
                if (string.IsNullOrEmpty(transactionDataJson))
                {
                    TempData["ErrorMessage"] = "Thông tin giao dịch không hợp lệ.";
                    return RedirectToAction("OpenSavingsBook");
                }
                
                var transactionData = JsonConvert.DeserializeObject<dynamic>(transactionDataJson);
                
                var packageId = (int)transactionData.MaGoiTietKiem;
                var accountNumber = (string)transactionData.SoTaiKhoan;
                var amount = (decimal)transactionData.SoTienGui;
                var loaiTaiTuc = (string)transactionData.LoaiTaiTuc;
                var ngayDaoHan = (DateTime)transactionData.NgayDaoHan;
                
                // Kiểm tra lại thông tin (phòng trường hợp session bị thay đổi)
                var bankAccount = await _context.TaiKhoanNganHangs
                    .FirstOrDefaultAsync(t => t.SoTaiKhoan == accountNumber && t.MaKH == user.Id);
                    
                if (bankAccount == null)
                {
                    TempData["ErrorMessage"] = "Tài khoản ngân hàng không hợp lệ.";
                    return RedirectToAction("OpenSavingsBook");
                }
                
                var package = await _context.GoiTietKiems.FindAsync(packageId);
                if (package == null || package.TrangThai != "Active")
                {
                    TempData["ErrorMessage"] = "Gói tiết kiệm không hợp lệ.";
                    return RedirectToAction("OpenSavingsBook");
                }
                
                try
                {
                    // Sử dụng transaction để đảm bảo toàn vẹn dữ liệu
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            // Tạo sổ tiết kiệm mới - KHÔNG CẦN CHỜ DUYỆT KHI KHÁCH HÀNG TỰ MỞ
                            var soTietKiem = new SoTietKiem
                            {
                                SoTaiKhoan = accountNumber,
                                SoTienGui = amount,
                                KyHan = package.KyHanThang,
                                LaiSuat = (double)package.LaiSuat,
                                NgayMoSo = DateTime.Now,
                                NgayDaoHan = ngayDaoHan,
                                TrangThai = "Đang hoạt động",
                                LoaiTaiTuc = loaiTaiTuc  // Lưu loại tái tục được chọn bởi khách hàng
                            };
                            
                            _context.SoTietKiems.Add(soTietKiem);
                            await _context.SaveChangesAsync();
                            
                            // Tạo giao dịch gửi tiền
                            var giaoDich = new GiaoDichTietKiem
                            {
                                MaSTK = soTietKiem.MaSTK,
                                LoaiGD = "Gửi",
                                SoTien = amount,
                                NgayGD = DateTime.Now,
                                MaNV = null, // Khách hàng tự mở sổ
                                TrangThaiGD = "DaDuyet"
                            };
                            _context.GiaoDichTietKiems.Add(giaoDich);
                            
                            // Trừ tiền từ tài khoản ngân hàng
                            bankAccount.SoDu -= amount;
                            _context.TaiKhoanNganHangs.Update(bankAccount);
                            
                            // Tạo thông báo
                            _context.ThongBaos.Add(new ThongBao
                            {
                                MaKH = user.Id,
                                TieuDe = "Mở sổ tiết kiệm thành công",
                                NoiDung = $"Bạn đã mở sổ tiết kiệm #{soTietKiem.MaSTK} với số tiền {string.Format("{0:N0}", amount)} đ, kỳ hạn {package.KyHanThang} tháng, lãi suất {package.LaiSuat}%/năm.",
                                TrangThai = "Chưa đọc",
                                NgayGui = DateTime.Now
                            });
                            
                            // Lưu giao dịch và thông báo
                            await _context.SaveChangesAsync();
                            
                            // Commit transaction
                            await transaction.CommitAsync();
                            
                            _logger.LogInformation($"Customer {user.Email} opened savings book #{soTietKiem.MaSTK} with amount {amount}");
                            
                            TempData["SuccessMessage"] = $"Mở sổ tiết kiệm thành công! Mã sổ: #{soTietKiem.MaSTK}";
                            return RedirectToAction("Dashboard");
                        }
                        catch (Exception innerEx)
                        {
                            // Rollback nếu có lỗi
                            await transaction.RollbackAsync();
                            _logger.LogError(innerEx, $"Inner error opening savings book: {innerEx.Message}");
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMsg += " | Inner: " + ex.InnerException.Message;
                        if (ex.InnerException.InnerException != null)
                        {
                            errorMsg += " | Inner2: " + ex.InnerException.InnerException.Message;
                        }
                    }
                    _logger.LogError(ex, $"Error opening savings book: {errorMsg}");
                    TempData["ErrorMessage"] = $"Có lỗi xảy ra khi mở sổ tiết kiệm: {errorMsg}";
                    return RedirectToAction("OpenSavingsBook");
                }
            }
            else if (purpose == "CloseSavingsAccount")
            {
                // Lấy thông tin từ session
                var id = HttpContext.Session.GetInt32("CloseSavingsBookId");
                var earlyWithdrawalObj = HttpContext.Session.GetBool("CloseSavingsBookEarlyWithdrawal");
                
                if (!id.HasValue || !earlyWithdrawalObj.HasValue)
                {
                    TempData["ErrorMessage"] = "Thông tin không hợp lệ.";
                    return RedirectToAction("Profile");
                }
                
                var savingsBookId = id.Value;
                var earlyWithdrawal = earlyWithdrawalObj.Value;
                
                // Lấy thông tin sổ tiết kiệm
                var savingsBook = await _context.SoTietKiems
                    .Include(s => s.TaiKhoanNganHang)
                    .FirstOrDefaultAsync(s => s.MaSTK == savingsBookId && s.TaiKhoanNganHang.MaKH == user.Id);

                if (savingsBook == null)
                {
                    TempData["ErrorMessage"] = "Sổ tiết kiệm không tồn tại hoặc không thuộc về bạn.";
                    return RedirectToAction("Profile");
                }

                // Kiểm tra trạng thái sổ tiết kiệm
                if (savingsBook.TrangThai != "Đang hoạt động")
                {
                    TempData["ErrorMessage"] = "Sổ tiết kiệm không ở trạng thái hoạt động.";
                    return RedirectToAction("SavingsBookDetails", new { id = savingsBookId });
                }

                // Kiểm tra ngày đáo hạn nếu không phải tất toán trước hạn
                bool isMatured = DateTime.Now >= savingsBook.NgayDaoHan;
                if (!earlyWithdrawal && !isMatured)
                {
                    TempData["ErrorMessage"] = "Chưa đến ngày đáo hạn. Nếu muốn tất toán trước hạn, vui lòng chọn tùy chọn tất toán trước hạn.";
                    return RedirectToAction("SavingsBookDetails", new { id = savingsBookId });
                }

                try
                {
                    // Sử dụng transaction để đảm bảo toàn vẹn dữ liệu
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            decimal withdrawalAmount = savingsBook.SoTienGui;

                            // Nếu là đáo hạn đúng hạn, tính thêm lãi suất
                            if (!earlyWithdrawal && isMatured)
                            {
                                // Tính số ngày gửi thực tế
                                var days = (savingsBook.NgayDaoHan - savingsBook.NgayMoSo).Days;
                                
                                // Tính lãi suất theo ngày
                                var dailyInterestRate = savingsBook.LaiSuat / 100 / 365;
                                
                                // Tính tiền lãi
                                var interest = savingsBook.SoTienGui * (decimal)(days * dailyInterestRate);
                                
                                // Cộng lãi vào số tiền rút
                                withdrawalAmount += interest;
                            }
                            // Nếu tất toán trước hạn, không tính lãi suất

                            // Tạo giao dịch rút tiền chờ duyệt
                            var transactionRecord = new GiaoDichTietKiem
                            {
                                MaSTK = savingsBook.MaSTK,
                                LoaiGD = "Rút",
                                SoTien = withdrawalAmount,
                                NgayGD = DateTime.Now,
                                MaNV = null, // Giao dịch được khởi tạo bởi khách hàng
                                TrangThaiGD = "ChoDuyet" // Thay đổi trạng thái thành chờ duyệt
                            };
                            _context.GiaoDichTietKiems.Add(transactionRecord);

                            // Cập nhật trạng thái sổ tiết kiệm thành chờ duyệt
                            savingsBook.TrangThai = "Chờ duyệt tất toán";

                            // Tạo thông báo cho khách hàng
                            string notificationMessage = earlyWithdrawal 
                                ? $"Yêu cầu tất toán trước hạn sổ tiết kiệm #{savingsBook.MaSTK} đang chờ duyệt bởi quản lý." 
                                : $"Yêu cầu đáo hạn sổ tiết kiệm #{savingsBook.MaSTK} đang chờ duyệt bởi quản lý.";

                            _context.ThongBaos.Add(new ThongBao
                            {
                                MaKH = user.Id,
                                TieuDe = earlyWithdrawal ? "Yêu cầu tất toán trước hạn đang chờ duyệt" : "Yêu cầu đáo hạn đang chờ duyệt",
                                NoiDung = notificationMessage,
                                TrangThai = "Chưa đọc",
                                NgayGui = DateTime.Now
                            });

                            // Lưu thay đổi
                            await _context.SaveChangesAsync();

                            // Commit transaction
                            await transaction.CommitAsync();

                            _logger.LogInformation($"Customer {user.Email} submitted close savings book request #{savingsBook.MaSTK} for approval");

                            string successMessage = earlyWithdrawal 
                                ? $"Đã gửi yêu cầu tất toán trước hạn sổ tiết kiệm #{savingsBook.MaSTK}. Đang chờ quản lý duyệt." 
                                : $"Đã gửi yêu cầu đáo hạn sổ tiết kiệm #{savingsBook.MaSTK}. Đang chờ quản lý duyệt.";

                            TempData["SuccessMessage"] = successMessage;
                            return RedirectToAction("Profile");
                        }
                        catch (Exception innerEx)
                        {
                            // Rollback nếu có lỗi
                            await transaction.RollbackAsync();
                            _logger.LogError(innerEx, $"Inner error closing savings book: {innerEx.Message}");
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMsg += " | Inner: " + ex.InnerException.Message;
                        if (ex.InnerException.InnerException != null)
                        {
                            errorMsg += " | Inner2: " + ex.InnerException.InnerException.Message;
                        }
                    }
                    _logger.LogError(ex, $"Error closing savings book: {errorMsg}");
                    TempData["ErrorMessage"] = $"Có lỗi xảy ra khi đóng sổ tiết kiệm: {errorMsg}";
                    return RedirectToAction("SavingsBookDetails", new { id = savingsBookId });
                }
            }
            
            TempData["ErrorMessage"] = "Mục đích không hợp lệ.";
            return RedirectToAction("Dashboard");
        }
        
        // GET: Hiển thị OTP trực tiếp trên màn hình
        public async Task<IActionResult> DisplayOTP(string purpose)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            // Kiểm tra xem người dùng đã thiết lập PIN chưa
            if (!user.IsPinSetup)
            {
                TempData["ErrorMessage"] = "Vui lòng thiết lập Mã PIN Digital OTP trước khi thực hiện giao dịch.";
                return RedirectToAction("SetupPin");
            }
            
            // Kiểm tra mục đích
            if (string.IsNullOrEmpty(purpose) || (purpose != "OpenSavingsBook" && purpose != "CloseSavingsAccount"))
            {
                TempData["ErrorMessage"] = "Mục đích không hợp lệ.";
                return RedirectToAction("Dashboard");
            }
            
            // Tạo mã OTP và lưu vào database
            var otpCode = await GenerateAndSendOTP(user.Email, user.Id, purpose);
            
            // Truyền thông tin đến view
            ViewBag.Purpose = purpose;
            ViewBag.OTPCode = otpCode;
            ViewBag.PurposeText = purpose == "OpenSavingsAccount" ? "mở sổ tiết kiệm" : "đóng sổ tiết kiệm";
            
            return View();
        }
        
        // GET: Thiết lập PIN
        public IActionResult SetupPin()
        {
            return View();
        }
        
        // POST: Thiết lập PIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetupPin(string pin, string confirmPin)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            // Kiểm tra độ dài PIN
            if (string.IsNullOrEmpty(pin) || pin.Length != 6)
            {
                TempData["ErrorMessage"] = "Mã PIN phải gồm 6 chữ số.";
                return View();
            }
            
            // Kiểm tra xác nhận PIN
            if (pin != confirmPin)
            {
                TempData["ErrorMessage"] = "Mã PIN xác nhận không khớp.";
                return View();
            }
            
            // Kiểm tra xem PIN chỉ chứa chữ số
            if (!pin.All(char.IsDigit))
            {
                TempData["ErrorMessage"] = "Mã PIN chỉ được chứa chữ số.";
                return View();
            }
            
            try
            {
                // Mã hóa PIN trước khi lưu (đơn giản hóa bằng cách đảo ngược chuỗi)
                // Trong thực tế, nên sử dụng các thuật toán mã hóa mạnh hơn
                string encryptedPin = EncryptPin(pin);
                
                // Cập nhật thông tin người dùng
                user.DigitalPin = encryptedPin;
                user.IsPinSetup = true;
                
                var result = await _userManager.UpdateAsync(user);
                
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Thiết lập Mã PIN Digital OTP thành công!";
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up PIN for user {UserId}", user.Id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thiết lập PIN. Vui lòng thử lại.";
            }
            
            return View();
        }
        
        // Hàm đơn giản để mã hóa PIN (chỉ dành cho demo)
        private string EncryptPin(string pin)
        {
            // Trong thực tế, nên sử dụng các thuật toán mã hóa mạnh như bcrypt, SHA256, v.v.
            // Đây chỉ là một ví dụ đơn giản bằng cách đảo ngược chuỗi và thêm salt
            string salt = "BankSaveSalt"; // Salt nên được lưu trữ an toàn
            string saltedPin = pin + salt;
            char[] array = saltedPin.ToCharArray();
            Array.Reverse(array);
            return new string(array);
        }
        
        // Hàm để xác minh PIN
        private bool VerifyPin(string enteredPin, string storedEncryptedPin)
        {
            string encryptedEnteredPin = EncryptPin(enteredPin);
            return encryptedEnteredPin == storedEncryptedPin;
        }
        
        // GET: Yêu cầu nhập PIN trước khi hiển thị OTP
        public IActionResult RequestPin(string purpose)
        {
            // Kiểm tra mục đích
            if (string.IsNullOrEmpty(purpose) || (purpose != "OpenSavingsAccount" && purpose != "CloseSavingsAccount"))
            {
                TempData["ErrorMessage"] = "Mục đích không hợp lệ.";
                return RedirectToAction("Dashboard");
            }
            
            ViewBag.Purpose = purpose;
            ViewBag.PurposeText = purpose == "OpenSavingsAccount" ? "mở sổ tiết kiệm" : "đóng sổ tiết kiệm";
            return View();
        }
        
        // POST: Xác minh PIN và chuyển hướng đến trang hiển thị OTP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPin(string pin, string purpose)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            // Kiểm tra mục đích
            if (string.IsNullOrEmpty(purpose) || (purpose != "OpenSavingsAccount" && purpose != "CloseSavingsAccount"))
            {
                TempData["ErrorMessage"] = "Mục đích không hợp lệ.";
                return RedirectToAction("Dashboard");
            }
            
            // Kiểm tra PIN
            if (string.IsNullOrEmpty(pin) || pin.Length != 6 || !pin.All(char.IsDigit))
            {
                TempData["ErrorMessage"] = "Mã PIN không hợp lệ. Vui lòng nhập mã PIN 6 chữ số.";
                ViewBag.Purpose = purpose;
                ViewBag.PurposeText = purpose == "OpenSavingsAccount" ? "mở sổ tiết kiệm" : "đóng sổ tiết kiệm";
                return View();
            }
            
            // Xác minh PIN
            if (!VerifyPin(pin, user.DigitalPin))
            {
                TempData["ErrorMessage"] = "Mã PIN không chính xác. Vui lòng thử lại.";
                ViewBag.Purpose = purpose;
                ViewBag.PurposeText = purpose == "OpenSavingsAccount" ? "mở sổ tiết kiệm" : "đóng sổ tiết kiệm";
                return View();
            }
            
            // Nếu PIN đúng, chuyển hướng đến trang hiển thị OTP
            // Chuyển đổi mục đích để phù hợp với DisplayOTP
            string displayPurpose = purpose == "OpenSavingsAccount" ? "OpenSavingsBook" : purpose;
            return RedirectToAction("DisplayOTP", new { purpose = displayPurpose });
        }
        
        // GET: Đổi mã PIN
        public IActionResult ChangePin()
        {
            return View();
        }
        
        // POST: Gửi OTP để đổi PIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePin(string action)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            // Gửi OTP đến email của khách hàng
            await GenerateAndSendOTP(user.Email, user.Id, "ChangePin");
            
            TempData["InfoMessage"] = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra email và nhập mã OTP để tiếp tục.";
            return RedirectToAction("VerifyChangePinOtp");
        }
        
        // GET: Xác minh OTP để đổi PIN
        public IActionResult VerifyChangePinOtp()
        {
            return View();
        }
        
        // POST: Xác minh OTP để đổi PIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyChangePinOtp(string otpCode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            // Kiểm tra mã OTP
            var otp = await _context.OTPVerifications
                .Where(o => o.Email == user.Email && o.OTPCode == otpCode && o.Purpose == "ChangePin" && !o.IsUsed && o.ExpiryTime > DateTime.Now)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
                
            if (otp == null)
            {
                TempData["ErrorMessage"] = "Mã OTP không hợp lệ hoặc đã hết hạn.";
                return View();
            }
            
            // Đánh dấu OTP đã sử dụng
            otp.IsUsed = true;
            _context.OTPVerifications.Update(otp);
            await _context.SaveChangesAsync();
            
            // Chuyển hướng đến trang nhập PIN mới
            return RedirectToAction("EnterNewPin");
        }
        
        // GET: Nhập PIN mới
        public IActionResult EnterNewPin()
        {
            return View();
        }
        
        // POST: Xác nhận PIN mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnterNewPin(string newPin, string confirmPin)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            // Kiểm tra độ dài PIN
            if (string.IsNullOrEmpty(newPin) || newPin.Length != 6)
            {
                TempData["ErrorMessage"] = "Mã PIN mới phải gồm 6 chữ số.";
                return View();
            }
            
            // Kiểm tra xác nhận PIN
            if (newPin != confirmPin)
            {
                TempData["ErrorMessage"] = "Mã PIN xác nhận không khớp.";
                return View();
            }
            
            // Kiểm tra xem PIN chỉ chứa chữ số
            if (!newPin.All(char.IsDigit))
            {
                TempData["ErrorMessage"] = "Mã PIN chỉ được chứa chữ số.";
                return View();
            }
            
            try
            {
                // Mã hóa PIN mới trước khi lưu
                string encryptedPin = EncryptPin(newPin);
                
                // Cập nhật thông tin người dùng
                user.DigitalPin = encryptedPin;
                user.IsPinSetup = true;
                
                var result = await _userManager.UpdateAsync(user);
                
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Đổi Mã PIN Digital OTP thành công!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing PIN for user {UserId}", user.Id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đổi PIN. Vui lòng thử lại.";
            }
            
            return View();
        }

        // Hiển thị form đặt lịch hẹn cho khách hàng
        [HttpGet]
        public async Task<IActionResult> BookAppointment()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy danh sách chi nhánh đang hoạt động
            var branches = await _context.ChiNhanhs
                .Where(c => c.TrangThaiHD == "HoatDong")
                .ToListAsync();

            // Lấy danh sách dịch vụ cho phép đặt lịch
            var services = await _context.LoaiDichVus
                .Where(d => d.ChoPhepDatLich)
                .ToListAsync();

            ViewBag.Branches = branches;
            ViewBag.Services = services;
            ViewBag.User = user;

            return View();
        }

        // API để lấy danh sách các khung giờ đã được đặt
        [HttpGet]
        public async Task<IActionResult> GetBookedTimeSlots(string branchId, DateTime date)
        {
            // Lấy danh sách các lịch hẹn đã được duyệt cho chi nhánh và ngày cụ thể
            var bookedAppointments = await _context.LichHens
                .Where(l => l.MaCN == branchId && 
                           l.NgayGiaoDich.Date == date.Date && 
                           (l.TrangThai == "DaDuyet" || l.TrangThai == "ChoDuyet"))
                .Select(l => l.KhungGio)
                .ToListAsync();

            return Json(bookedAppointments);
        }

        // Xử lý đặt lịch hẹn
        [HttpPost]
        public async Task<IActionResult> BookAppointment(string maDV, string maCN, DateTime ngayGiaoDich, string khungGio, string ghiChuKH)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(maDV) || string.IsNullOrEmpty(maCN) || ngayGiaoDich == default(DateTime) || string.IsNullOrEmpty(khungGio))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin.";
                return RedirectToAction(nameof(BookAppointment));
            }

            // Kiểm tra chi nhánh có tồn tại và đang hoạt động không
            var branch = await _context.ChiNhanhs
                .FirstOrDefaultAsync(c => c.MaCN == maCN && c.TrangThaiHD == "HoatDong");
            if (branch == null)
            {
                TempData["ErrorMessage"] = "Chi nhánh không hợp lệ hoặc đã đóng cửa.";
                return RedirectToAction(nameof(BookAppointment));
            }

            // Kiểm tra dịch vụ có tồn tại và cho phép đặt lịch không
            var service = await _context.LoaiDichVus
                .FirstOrDefaultAsync(d => d.MaDV == maDV && d.ChoPhepDatLich);
            if (service == null)
            {
                TempData["ErrorMessage"] = "Dịch vụ không hợp lệ hoặc không cho phép đặt lịch.";
                return RedirectToAction(nameof(BookAppointment));
            }

            // Kiểm tra ngày giao dịch có phải là ngày trong tương lai không
            if (ngayGiaoDich.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Ngày giao dịch không hợp lệ.";
                return RedirectToAction(nameof(BookAppointment));
            }

            // Tạo mã lịch hẹn duy nhất
            string maLichHen = "LH" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(100, 999);

            // Tạo đối tượng lịch hẹn
            var appointment = new LichHen
            {
                MaLichHen = maLichHen,
                MaKH = user.Id,
                MaDV = maDV,
                MaCN = maCN,
                NgayGiaoDich = ngayGiaoDich,
                KhungGio = khungGio,
                GhiChuKH = ghiChuKH ?? "",
                TrangThai = "ChoDuyet",
                ThoiGianTao = DateTime.Now
            };

            // Lưu vào cơ sở dữ liệu
            _context.LichHens.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đặt lịch hẹn thành công. Mã lịch hẹn của bạn là: " + maLichHen;
            return RedirectToAction(nameof(AppointmentConfirmation), new { maLichHen = maLichHen });
        }

        // Hiển thị xác nhận đặt lịch hẹn
        [HttpGet]
        public async Task<IActionResult> AppointmentConfirmation(string maLichHen)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var appointment = await _context.LichHens
                .Include(l => l.KhachHang)
                .Include(l => l.LoaiDichVu)
                .Include(l => l.ChiNhanh)
                .FirstOrDefaultAsync(l => l.MaLichHen == maLichHen && l.MaKH == user.Id);

            if (appointment == null)
            {
                return NotFound();
            }

            ViewBag.User = user;
            return View(appointment);
        }
    }
}
