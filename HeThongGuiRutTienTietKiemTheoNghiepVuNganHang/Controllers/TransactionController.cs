using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models;
using System.Linq;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Controllers
{
    [Authorize(Roles = "NhanVienGiaoDich")]
    public class TransactionController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(
            UserManager<User> userManager,
            ApplicationDbContext context,
            ILogger<TransactionController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        private int CalculateInterest(SoTietKiem savingsBook, DateTime endDate)
        {
            // Tính số ngày từ ngày mở sổ đến ngày đáo hạn
            var days = (endDate - savingsBook.NgayMoSo).Days;
            // Tính lãi suất theo công thức: Số tiền * Lãi suất * Số ngày / 365
            return (int)(savingsBook.SoTienGui * (decimal)savingsBook.LaiSuat * days / 36500);
        }

        // Phương thức tính tổng số tiền (gốc + lãi) khi tất toán sổ tiết kiệm
        private decimal CalculateTotalAmountWithInterest(SoTietKiem savingsBook)
        {
            // Tính số ngày gửi thực tế
            var days = (DateTime.Now - savingsBook.NgayMoSo).Days;
            
            // Tính lãi suất theo ngày
            var dailyInterestRate = savingsBook.LaiSuat / 100 / 365;
            
            // Tính tiền lãi
            var interest = savingsBook.SoTienGui * (decimal)(days * dailyInterestRate);
            
            // Trả về tổng số tiền (gốc + lãi)
            return savingsBook.SoTienGui + interest;
        }

        public async Task<IActionResult> Overview()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Giao dịch do nhân viên này thực hiện
            var myTransactions = await _context.GiaoDichTietKiems
                .Where(g => g.MaNV == user.Id)
                .OrderByDescending(g => g.NgayGD)
                .Take(20)
                .Include(g => g.SoTietKiem)
                .ToListAsync();

            // Thống kê theo ngày
            var today = DateTime.Today;
            var todayTransactions = myTransactions.Where(g => g.NgayGD.Date == today).ToList();
            var totalTodayAmount = todayTransactions.Sum(g => g.SoTien);
            var countToday = todayTransactions.Count;

            // Thống kê theo tuần
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var weekTransactions = myTransactions.Where(g => g.NgayGD.Date >= startOfWeek).ToList();
            var countWeek = weekTransactions.Count;

            // Thống kê theo tháng
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var monthTransactions = myTransactions.Where(g => g.NgayGD.Date >= startOfMonth).ToList();
            var totalMonthAmount = monthTransactions.Sum(g => g.SoTien);
            var countMonth = monthTransactions.Count;

            // Thống kê loại giao dịch
            var countDeposit = myTransactions.Count(g => g.LoaiGD == "Gửi");
            var countWithdraw = myTransactions.Count(g => g.LoaiGD == "Rút");

            ViewBag.User = user;
            ViewBag.MyTransactions = myTransactions;
            ViewBag.TotalTodayAmount = totalTodayAmount;
            ViewBag.CountToday = countToday;
            ViewBag.CountWeek = countWeek;
            ViewBag.CountMonth = countMonth;
            ViewBag.TotalMonthAmount = totalMonthAmount;
            ViewBag.CountDeposit = countDeposit;
            ViewBag.CountWithdraw = countWithdraw;

            if (TempData["LoginSuccess"] != null)
            {
                ViewBag.LoginSuccess = TempData["LoginSuccess"];
            }

            // Giao dịch chờ duyệt
            var pendingTransactions = await _context.GiaoDichTietKiems
                .Where(g => g.TrangThaiGD == "ChoDuyet")
                .OrderByDescending(g => g.NgayGD)
                .Include(g => g.SoTietKiem)
                .Take(20)
                .ToListAsync();
            ViewBag.PendingTransactions = pendingTransactions;

            // Tra cứu khách hàng theo TempData (CCCD / Email / SĐT)
            if (TempData["SearchQuery"] != null)
            {
                var q = TempData["SearchQuery"].ToString();
                // Chỉ hiển thị những tài khoản có vai trò là khách hàng
                var allResults = await _userManager.Users
                    .Where(u => u.CCCD.Contains(q) || u.Email.Contains(q) || u.SDT.Contains(q))
                    .ToListAsync();

                var customerResults = new List<User>();
                foreach (var u in allResults)
                {
                    if (await _userManager.IsInRoleAsync(u, "KhachHang"))
                    {
                        customerResults.Add(u);
                    }
                }

                ViewBag.CustomerResults = customerResults.Take(50).ToList(); // Giới hạn 50 kết quả
                TempData["SearchQuery"] = q; // Giữ lại giá trị tìm kiếm
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNotification(string accountNumber, string title, string content)
        {
            if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                TempData["ActionMessage"] = "Vui lòng nhập đầy đủ số tài khoản, tiêu đề và nội dung thông báo.";
                return RedirectToAction("CreateNotificationPage");
            }

            // Tìm tài khoản ngân hàng theo số tài khoản
            var bankAccount = await _context.TaiKhoanNganHangs
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.SoTaiKhoan == accountNumber);

            if (bankAccount == null || bankAccount.KhachHang == null || !await _userManager.IsInRoleAsync(bankAccount.KhachHang, "KhachHang"))
            {
                TempData["ActionMessage"] = "Không tìm thấy tài khoản ngân hàng hoặc khách hàng không hợp lệ.";
                return RedirectToAction("CreateNotificationPage");
            }

            var notification = new ThongBao
            {
                MaKH = bankAccount.MaKH,
                TieuDe = title.Trim(),
                NoiDung = content.Trim(),
                TrangThai = "Chưa đọc",
                NgayGui = DateTime.Now
            };

            _context.ThongBaos.Add(notification);
            await _context.SaveChangesAsync();

            TempData["ActionMessage"] = "Đã gửi thông báo tới khách hàng.";
            return RedirectToAction("CreateNotificationPage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCustomer(string userId, string note)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ActionMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var customer = await _userManager.FindByIdAsync(userId);
            if (customer == null || !await _userManager.IsInRoleAsync(customer, "KhachHang"))
            {
                TempData["ActionMessage"] = "Không tìm thấy khách hàng.";
                return RedirectToAction("Customers");
            }

            // Gửi thông báo yêu cầu xác minh danh tính
            _context.ThongBaos.Add(new ThongBao
            {
                MaKH = userId,
                TieuDe = "Yêu cầu xác minh danh tính",
                NoiDung = string.IsNullOrWhiteSpace(note) ? "Yêu cầu xác minh danh tính" : note,
                TrangThai = "Chưa đọc",
                NgayGui = DateTime.Now
            });
            await _context.SaveChangesAsync();
            TempData["ActionMessage"] = "Đã gửi yêu cầu xác minh tới khách hàng.";
            return RedirectToAction("Customers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveTransaction(int? maGD)
        {
            if (!maGD.HasValue)
            {
                TempData["ActionMessage"] = "Mã giao dịch không hợp lệ.";
                return RedirectToAction("Dashboard");
            }

            var gd = await _context.GiaoDichTietKiems
                .Include(x => x.SoTietKiem)
                    .ThenInclude(stk => stk.TaiKhoanNganHang)
                .FirstOrDefaultAsync(x => x.MaGD == maGD.Value);
            if (gd == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy giao dịch.";
                return RedirectToAction("Dashboard");
            }
            gd.TrangThaiGD = "DaDuyet";
            await _context.SaveChangesAsync();
            var maKH = gd.SoTietKiem?.TaiKhoanNganHang?.MaKH;
            if (!string.IsNullOrEmpty(maKH))
            {
                _context.ThongBaos.Add(new ThongBao
                {
                    MaKH = maKH,
                    TieuDe = "Giao dịch được duyệt",
                    NoiDung = $"Giao dịch #{gd.MaGD} đã được duyệt",
                    TrangThai = "Chưa đọc",
                    NgayGui = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
            TempData["ActionMessage"] = "Đã duyệt giao dịch.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectTransaction(int? maGD, string reason)
        {
            if (!maGD.HasValue)
            {
                TempData["ActionMessage"] = "Mã giao dịch không hợp lệ.";
                return RedirectToAction("Dashboard");
            }

            var gd = await _context.GiaoDichTietKiems
                .Include(x => x.SoTietKiem)
                    .ThenInclude(stk => stk.TaiKhoanNganHang)
                .FirstOrDefaultAsync(x => x.MaGD == maGD.Value);
            if (gd == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy giao dịch.";
                return RedirectToAction("Dashboard");
            }
            gd.TrangThaiGD = "TuChoi";
            await _context.SaveChangesAsync();
            var maKH = gd.SoTietKiem?.TaiKhoanNganHang?.MaKH;
            if (!string.IsNullOrEmpty(maKH))
            {
                _context.ThongBaos.Add(new ThongBao
                {
                    MaKH = maKH,
                    TieuDe = "Giao dịch bị từ chối",
                    NoiDung = string.IsNullOrWhiteSpace(reason) ? $"Giao dịch #{gd.MaGD} bị từ chối" : reason,
                    TrangThai = "Chưa đọc",
                    NgayGui = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
            TempData["ActionMessage"] = "Đã từ chối giao dịch.";
            return RedirectToAction("Dashboard");
        }

        // Quản lý khách hàng
        public async Task<IActionResult> Customers(string q)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            // Chỉ hiển thị những tài khoản có vai trò là khách hàng
            var customers = await _userManager.GetUsersInRoleAsync("KhachHang");
            
            IEnumerable<User> query = customers;
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(u => u.HoTen.Contains(q) || u.Email.Contains(q) || u.SDT.Contains(q) || u.CCCD.Contains(q));
            }

            var users = query.OrderBy(u => u.HoTen).Take(200).ToList();
            ViewBag.Users = users;
            ViewBag.Query = q;
            return View();
        }

        // Chi tiết khách hàng
        public async Task<IActionResult> CustomerDetails(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var kh = await _userManager.FindByIdAsync(id);
            // Chỉ hiển thị chi tiết của khách hàng có vai trò là khách hàng
            if (kh == null || !await _userManager.IsInRoleAsync(kh, "KhachHang")) return NotFound();

            var bankAccounts = await _context.TaiKhoanNganHangs
                .Where(t => t.MaKH == kh.Id)
                .Include(t => t.SoTietKiems)
                .ToListAsync();

            var soTietKiemIds = bankAccounts.SelectMany(b => b.SoTietKiems.Select(s => s.MaSTK)).ToList();
            var recentTransactions = await _context.GiaoDichTietKiems
                .Where(g => g.MaSTK.HasValue && soTietKiemIds.Contains(g.MaSTK.Value))
                .OrderByDescending(g => g.NgayGD)
                .Take(10)
                .Include(g => g.SoTietKiem)
                .ToListAsync();

            ViewBag.Customer = kh;
            ViewBag.BankAccounts = bankAccounts;
            ViewBag.RecentTransactions = recentTransactions;
            ViewBag.TotalSavings = bankAccounts.Sum(b => b.SoTietKiems.Sum(s => s.SoTienGui));
            return View();
        }

        // GET: Trang tìm kiếm khách hàng
        public async Task<IActionResult> SearchCustomersPage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            return View();
        }

        // POST: Tìm kiếm khách hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SearchCustomersPage(string query)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ActionMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                TempData["ActionMessage"] = "Vui lòng nhập từ khóa tìm kiếm.";
                return RedirectToAction("SearchCustomersPage");
            }

            // Lưu query vào TempData để sử dụng trong action Customers
            TempData["SearchQuery"] = query.Trim();
            return RedirectToAction("Customers");
        }

        // GET: Cập nhật thông tin liên lạc khách hàng
        public async Task<IActionResult> UpdateCustomerContactPage(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var customer = await _userManager.FindByIdAsync(id);
            if (customer == null || !await _userManager.IsInRoleAsync(customer, "KhachHang")) return NotFound();

            return View(customer);
        }

        // POST: Cập nhật thông tin liên lạc khách hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCustomerContactPage(string id, string email, string phoneNumber, string address)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ActionMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var customer = await _userManager.FindByIdAsync(id);
            if (customer == null || !await _userManager.IsInRoleAsync(customer, "KhachHang")) return NotFound();

            // Cập nhật thông tin
            if (!string.IsNullOrWhiteSpace(email))
            {
                customer.Email = email;
            }
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                customer.SDT = phoneNumber;
            }
            if (!string.IsNullOrWhiteSpace(address))
            {
                customer.DiaChi = address;
            }

            var result = await _userManager.UpdateAsync(customer);
            if (result.Succeeded)
            {
                TempData["ActionMessage"] = "Đã cập nhật thông tin khách hàng thành công.";
            }
            else
            {
                TempData["ActionMessage"] = "Có lỗi xảy ra khi cập nhật thông tin khách hàng.";
            }

            return RedirectToAction("Customers");
        }

        // GET: Nạp tiền vào tài khoản thanh toán
        public IActionResult Deposit()
        {
            // Chuyển hướng sang controller mới
            return RedirectToAction("Deposit", "BankingTransaction");
        }

        // POST: Nạp tiền vào tài khoản thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Deposit(string accountNumber, decimal amount)
        {
            // Chuyển hướng sang controller mới
            return RedirectToAction("Deposit", "BankingTransaction");
        }

        // GET: Rút tiền từ tài khoản thanh toán
        public IActionResult Withdraw()
        {
            // Chuyển hướng sang controller mới
            return RedirectToAction("Withdraw", "BankingTransaction");
        }

        // POST: Rút tiền từ tài khoản thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Withdraw(string accountNumber, decimal amount)
        {
            // Chuyển hướng sang controller mới
            return RedirectToAction("Withdraw", "BankingTransaction");
        }

        // GET: Mở sổ tiết kiệm
        public async Task<IActionResult> OpenSavingsBook()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy danh sách gói tiết kiệm active
            var packages = await _context.GoiTietKiems
                .Where(g => g.TrangThai == "Active")
                .OrderBy(g => g.KyHanThang)
                .ToListAsync();

            ViewBag.SavingsPackages = packages;
            return View();
        }

        // POST: Mở sổ tiết kiệm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OpenSavingsBook(
            string bankAccountNumber,
            int packageId,
            decimal amount,
            DateTime startDate,
            string renewalType) // Thêm tham số loại tái tục
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ActionMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(bankAccountNumber) || amount <= 0 || packageId <= 0 || string.IsNullOrEmpty(renewalType))
            {
                TempData["ActionMessage"] = "Vui lòng nhập đầy đủ thông tin.";
                return RedirectToAction("OpenSavingsBook");
            }

            // Kiểm tra tài khoản ngân hàng
            var bankAccount = await _context.TaiKhoanNganHangs
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.SoTaiKhoan == bankAccountNumber);

            if (bankAccount == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy tài khoản ngân hàng.";
                return RedirectToAction("OpenSavingsBook");
            }

            // Kiểm tra gói tiết kiệm
            var package = await _context.GoiTietKiems.FindAsync(packageId);
            if (package == null || package.TrangThai != "Active")
            {
                TempData["ActionMessage"] = "Gói tiết kiệm không hợp lệ.";
                return RedirectToAction("OpenSavingsBook");
            }

            // Kiểm tra số tiền tối thiểu theo gói
            if (amount < package.SoTienToiThieu)
            {
                TempData["ActionMessage"] = $"Số tiền gửi phải lớn hơn hoặc bằng {string.Format("{0:N0}", package.SoTienToiThieu)} đ theo gói đã chọn.";
                return RedirectToAction("OpenSavingsBook");
            }

            // Khi nhân viên mở sổ tại quầy (dùng tiền mặt), không cần kiểm tra số dư
            // Chỉ kiểm tra số dư khi khách hàng tự mở sổ trên hệ thống
            // (Logic này sẽ được xử lý khi quản lý duyệt giao dịch)

            try
            {
                // Sử dụng transaction để đảm bảo toàn vẹn dữ liệu
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Tạo sổ tiết kiệm mới với trạng thái chờ duyệt
                        var savingsBook = new SoTietKiem
                        {
                            SoTaiKhoan = bankAccount.SoTaiKhoan,
                            NgayMoSo = startDate,
                            NgayDaoHan = startDate.AddMonths(package.KyHanThang),
                            SoTienGui = amount,
                            LaiSuat = (double)package.LaiSuat,
                            KyHan = package.KyHanThang,
                            TrangThai = "Chờ duyệt",
                            LoaiTaiTuc = renewalType // Sử dụng loại tái tục được chọn bởi nhân viên
                        };
                        _context.SoTietKiems.Add(savingsBook);
                        
                        // Lưu sổ tiết kiệm trước để có MaSTK
                        await _context.SaveChangesAsync();

                        // Tạo giao dịch gửi tiền chờ duyệt
                        var giaoDich = new GiaoDichTietKiem
                        {
                            MaSTK = savingsBook.MaSTK,
                            LoaiGD = "Gửi",
                            SoTien = amount,
                            NgayGD = DateTime.Now,
                            MaNV = user.Id, // Nhân viên thực hiện giao dịch
                            TrangThaiGD = "ChoDuyet"
                        };
                        _context.GiaoDichTietKiems.Add(giaoDich);

                        // Tạo thông báo cho khách hàng
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = bankAccount.MaKH,
                            TieuDe = "Yêu cầu mở sổ tiết kiệm đang chờ duyệt",
                            NoiDung = $"Yêu cầu mở sổ tiết kiệm #{savingsBook.MaSTK} với số tiền {string.Format("{0:N0}", amount)} đ đang chờ duyệt bởi quản lý.",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });

                        // Lưu thay đổi
                        await _context.SaveChangesAsync();

                        // Commit transaction
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Employee {user.Email} submitted savings book #{savingsBook.MaSTK} for customer {bankAccount.KhachHang?.Email} for approval");

                        TempData["ActionMessage"] = $"Đã gửi yêu cầu mở sổ tiết kiệm #{savingsBook.MaSTK} với số tiền {string.Format("{0:N0}", amount)} đ. Đang chờ quản lý duyệt.";
                        return RedirectToAction("OpenSavingsBook");
                    }
                    catch (Exception innerEx)
                    {
                        // Rollback nếu có lỗi
                        await transaction.RollbackAsync();
                        _logger.LogError(innerEx, $"Inner error processing opening savings book: {innerEx.Message}");
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
                _logger.LogError(ex, $"Error processing opening savings book: {errorMsg}");
                TempData["ActionMessage"] = $"Có lỗi xảy ra khi mở sổ tiết kiệm: {errorMsg}";
                return RedirectToAction("OpenSavingsBook");
            }
        }

        // Hàm tạo số tài khoản ngân hàng ngẫu nhiên
        private string GenerateBankAccountNumber()
        {
            var random = new Random();
            var accountNumber = "";
            for (int i = 0; i < 12; i++)
            {
                accountNumber += random.Next(0, 10);
            }
            return accountNumber;
        }

        // GET: Tất toán sổ tiết kiệm
        public async Task<IActionResult> CloseSavingsBook(string customerId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Nếu có customerId, lấy thông tin khách hàng và sổ tiết kiệm
            if (!string.IsNullOrEmpty(customerId))
            {
                var customer = await _userManager.FindByIdAsync(customerId);
                if (customer != null && await _userManager.IsInRoleAsync(customer, "KhachHang"))
                {
                    var activeSavingsBooks = await _context.SoTietKiems
                        .Include(s => s.TaiKhoanNganHang)
                        .Where(s => s.TaiKhoanNganHang.MaKH == customerId && 
                                   s.TrangThai == "Đang hoạt động")
                        .ToListAsync();

                    ViewBag.Customer = customer;
                    ViewBag.ActiveSavingsBooks = activeSavingsBooks;
                }
            }

            return View();
        }

        // POST: Tất toán sổ tiết kiệm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseSavingsBook(string customerId, int savingsBookId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ActionMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(customerId) || savingsBookId <= 0)
            {
                TempData["ActionMessage"] = "Vui lòng chọn sổ tiết kiệm cần tất toán.";
                return RedirectToAction("CloseSavingsBook");
            }

            // Kiểm tra sổ tiết kiệm
            var savingsBook = await _context.SoTietKiems
                .Include(s => s.TaiKhoanNganHang)
                .FirstOrDefaultAsync(s => s.MaSTK == savingsBookId && 
                                         s.TaiKhoanNganHang.MaKH == customerId && 
                                         s.TrangThai == "Đang hoạt động");

            if (savingsBook == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy sổ tiết kiệm hợp lệ.";
                return RedirectToAction("CloseSavingsBook");
            }

            // Tính tổng số tiền (gốc + lãi)
            var totalAmount = CalculateTotalAmountWithInterest(savingsBook);
            var interest = totalAmount - savingsBook.SoTienGui;

            // Kiểm tra nếu tất toán trước hạn
            bool isOnTime = DateTime.Now >= savingsBook.NgayDaoHan;
            if (!isOnTime)
            {
                TempData["ActionMessage"] = "Chưa đến ngày đáo hạn. Không thể tất toán trước hạn.";
                return RedirectToAction("CloseSavingsBook");
            }

            try
            {
                // Sử dụng transaction để đảm bảo toàn vẹn dữ liệu
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Cập nhật trạng thái sổ tiết kiệm
                        savingsBook.TrangThai = "Đã tất toán";
                        savingsBook.NgayDaoHan = DateTime.Now;

                        // Cập nhật số dư tài khoản ngân hàng
                        var bankAccount = savingsBook.TaiKhoanNganHang;
                        bankAccount.SoDu += totalAmount;

                        // Tạo giao dịch rút tiền
                        var giaoDich = new GiaoDichTietKiem
                        {
                            MaSTK = savingsBook.MaSTK,
                            LoaiGD = "Rút",
                            SoTien = totalAmount,
                            NgayGD = DateTime.Now,
                            MaNV = null, // Giao dịch tự động qua hệ thống
                            TrangThaiGD = "DaDuyet"
                        };
                        _context.GiaoDichTietKiems.Add(giaoDich);

                        // Tạo thông báo
                        string notificationMessage = isOnTime 
                            ? $"Bạn đã đáo hạn sổ tiết kiệm #{savingsBook.MaSTK}. Số tiền gốc và lãi đã được chuyển về tài khoản ngân hàng của bạn."
                            : $"Bạn đã tất toán trước hạn sổ tiết kiệm #{savingsBook.MaSTK}. Số tiền gốc và lãi đã được chuyển về tài khoản ngân hàng của bạn.";

                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = customerId,
                            TieuDe = "Tất toán sổ tiết kiệm thành công",
                            NoiDung = notificationMessage,
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });

                        // Lưu thay đổi
                        await _context.SaveChangesAsync();

                        // Commit transaction
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Employee {user.Email} closed savings book #{savingsBook.MaSTK} for customer {customerId}");

                        TempData["ActionMessage"] = $"Đã tất toán sổ tiết kiệm #{savingsBook.MaSTK}. Tổng số tiền nhận được: {string.Format("{0:N0}", totalAmount)} đ (Gốc: {string.Format("{0:N0}", savingsBook.SoTienGui)} đ, Lãi: {string.Format("{0:N0}", interest)} đ).";
                        return RedirectToAction("CloseSavingsBook");
                    }
                    catch (Exception innerEx)
                    {
                        // Rollback nếu có lỗi
                        await transaction.RollbackAsync();
                        _logger.LogError(innerEx, $"Inner error processing closing savings book: {innerEx.Message}");
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
                _logger.LogError(ex, $"Error processing closing savings book: {errorMsg}");
                TempData["ActionMessage"] = $"Có lỗi xảy ra khi tất toán sổ tiết kiệm: {errorMsg}";
                return RedirectToAction("CloseSavingsBook");
            }
        }

        // GET: Giao dịch chờ duyệt
        public async Task<IActionResult> PendingApprovals(string customerName = null, string transactionType = null, DateTime? fromDate = null, DateTime? toDate = null, decimal? minAmount = null, decimal? maxAmount = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var query = _context.GiaoDichTietKiems
                .Where(g => g.TrangThaiGD == "ChoDuyet")
                .Include(g => g.SoTietKiem)
                    .ThenInclude(s => s.TaiKhoanNganHang)
                        .ThenInclude(t => t.KhachHang)
                .AsQueryable();
                
            // Bộ lọc nâng cao
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                customerName = customerName.Trim();
                query = query.Where(g => g.SoTietKiem.TaiKhoanNganHang.KhachHang.HoTen.Contains(customerName));
            }
            
            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                query = query.Where(g => g.LoaiGD == transactionType);
            }
            
            if (fromDate.HasValue)
            {
                query = query.Where(g => g.NgayGD >= fromDate.Value);
            }
            
            if (toDate.HasValue)
            {
                query = query.Where(g => g.NgayGD <= toDate.Value);
            }
            
            if (minAmount.HasValue)
            {
                query = query.Where(g => g.SoTien >= minAmount.Value);
            }
            
            if (maxAmount.HasValue)
            {
                query = query.Where(g => g.SoTien <= maxAmount.Value);
            }

            var pendingTransactions = await query
                .OrderByDescending(g => g.NgayGD)
                .Take(50)
                .ToListAsync();

            ViewBag.PendingTransactions = pendingTransactions;
            ViewBag.CustomerName = customerName;
            ViewBag.TransactionType = transactionType;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.MinAmount = minAmount;
            ViewBag.MaxAmount = maxAmount;
            return View();
        }

        // GET: Tạo thông báo
        public async Task<IActionResult> CreateNotificationPage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            return View();
        }

        // GET: Nạp tiền vào tài khoản
        public IActionResult DepositMoney()
        {
            // Chuyển hướng sang controller mới
            return RedirectToAction("Deposit", "BankingTransaction");
        }

        // POST: Nạp tiền vào tài khoản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DepositMoney(string accountNumber, decimal amount)
        {
            // Chuyển hướng sang controller mới
            return RedirectToAction("Deposit", "BankingTransaction");
        }

        // API endpoint để lấy thông tin khách hàng theo số tài khoản
        [HttpGet]
        public async Task<IActionResult> GetCustomerInfo(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                return Json(new { success = false, message = "Vui lòng nhập số tài khoản." });
            }

            var bankAccount = await _context.TaiKhoanNganHangs
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.SoTaiKhoan == accountNumber);

            if (bankAccount == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản ngân hàng." });
            }

            return Json(new { 
                success = true, 
                customerName = bankAccount.KhachHang?.HoTen,
                cccd = bankAccount.KhachHang?.CCCD,
                email = bankAccount.KhachHang?.Email,
                phoneNumber = bankAccount.KhachHang?.SDT,
                currentBalance = bankAccount.SoDu
            });
        }

    }
}