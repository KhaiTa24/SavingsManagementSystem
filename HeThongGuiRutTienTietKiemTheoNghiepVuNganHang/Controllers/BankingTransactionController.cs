using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Controllers
{
    [Authorize(Roles = "NhanVienGiaoDich")]
    public class BankingTransactionController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BankingTransactionController> _logger;

        public BankingTransactionController(
            UserManager<User> userManager,
            ApplicationDbContext context,
            ILogger<BankingTransactionController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        // GET: Trang nạp tiền vào tài khoản ngân hàng
        public async Task<IActionResult> Deposit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Log for debugging
            _logger.LogInformation("Accessing Deposit view");
            return View("Deposit");
        }

        // POST: Nạp tiền vào tài khoản ngân hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(string accountNumber, decimal amount)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ActionMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(accountNumber) || amount <= 0)
            {
                TempData["ActionMessage"] = "Vui lòng nhập đầy đủ thông tin.";
                return RedirectToAction("Deposit");
            }

            // Kiểm tra tài khoản ngân hàng
            var bankAccount = await _context.TaiKhoanNganHangs
                .FirstOrDefaultAsync(t => t.SoTaiKhoan == accountNumber);

            if (bankAccount == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy tài khoản ngân hàng.";
                return RedirectToAction("Deposit");
            }

            try
            {
                // Sử dụng transaction để đảm bảo toàn vẹn dữ liệu
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Tạo giao dịch nạp tiền chờ duyệt
                        var transactionRecord = new GiaoDichNganHang
                        {
                            SoTaiKhoan = accountNumber,
                            LoaiGD = "Nạp tiền",
                            SoTien = amount,
                            NgayGD = DateTime.Now,
                            MaNV = user.Id,
                            TrangThaiGD = "ChoDuyet" // Thay đổi trạng thái thành chờ duyệt
                        };
                        _context.GiaoDichNganHangs.Add(transactionRecord);

                        // Tạo thông báo cho khách hàng
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = bankAccount.MaKH,
                            TieuDe = "Yêu cầu nạp tiền đang chờ duyệt",
                            NoiDung = $"Yêu cầu nạp {string.Format("{0:N0}", amount)} đ vào tài khoản ngân hàng {bankAccount.SoTaiKhoan} đang chờ duyệt bởi quản lý. Số dư tài khoản sẽ được cập nhật sau khi được duyệt.",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });

                        // Lưu thay đổi
                        await _context.SaveChangesAsync();

                        // Commit transaction
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Employee {user.Email} submitted deposit request of {amount} to bank account {bankAccount.SoTaiKhoan} for approval");

                        TempData["ActionMessage"] = $"Đã gửi yêu cầu nạp {string.Format("{0:N0}", amount)} đ vào tài khoản {bankAccount.SoTaiKhoan}. Số dư sẽ được cập nhật sau khi được quản lý duyệt.";
                        return RedirectToAction("Deposit");
                    }
                    catch (Exception innerEx)
                    {
                        // Rollback nếu có lỗi
                        await transaction.RollbackAsync();
                        _logger.LogError(innerEx, $"Inner error processing deposit: {innerEx.Message}");
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
                _logger.LogError(ex, $"Error processing deposit: {errorMsg}");
                TempData["ActionMessage"] = $"Có lỗi xảy ra khi nạp tiền: {errorMsg}";
                return RedirectToAction("Deposit");
            }
        }

        // GET: Trang rút tiền từ tài khoản ngân hàng
        public async Task<IActionResult> Withdraw()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Log for debugging
            _logger.LogInformation("Accessing Withdraw view");
            return View("Withdraw");
        }

        // POST: Rút tiền từ tài khoản ngân hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(string accountNumber, decimal amount)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ActionMessage"] = "Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(accountNumber) || amount <= 0)
            {
                TempData["ActionMessage"] = "Vui lòng nhập đầy đủ thông tin.";
                return RedirectToAction("Withdraw");
            }

            // Kiểm tra tài khoản ngân hàng
            var bankAccount = await _context.TaiKhoanNganHangs
                .FirstOrDefaultAsync(t => t.SoTaiKhoan == accountNumber);

            if (bankAccount == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy tài khoản ngân hàng.";
                return RedirectToAction("Withdraw");
            }

            // Kiểm tra số dư
            if (amount > bankAccount.SoDu)
            {
                TempData["ActionMessage"] = "Số tiền rút vượt quá số dư trong tài khoản.";
                return RedirectToAction("Withdraw");
            }

            try
            {
                // Sử dụng transaction để đảm bảo toàn vẹn dữ liệu
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Tạo giao dịch rút tiền chờ duyệt
                        var transactionRecord = new GiaoDichNganHang
                        {
                            SoTaiKhoan = accountNumber,
                            LoaiGD = "Rút tiền",
                            SoTien = amount,
                            NgayGD = DateTime.Now,
                            MaNV = user.Id,
                            TrangThaiGD = "ChoDuyet" // Thay đổi trạng thái thành chờ duyệt
                        };
                        _context.GiaoDichNganHangs.Add(transactionRecord);

                        // Tạo thông báo cho khách hàng
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = bankAccount.MaKH,
                            TieuDe = "Yêu cầu rút tiền đang chờ duyệt",
                            NoiDung = $"Yêu cầu rút {string.Format("{0:N0}", amount)} đ từ tài khoản ngân hàng {bankAccount.SoTaiKhoan} đang chờ duyệt bởi quản lý. Số dư tài khoản sẽ được cập nhật sau khi được duyệt.",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });

                        // Lưu thay đổi
                        await _context.SaveChangesAsync();

                        // Commit transaction
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Employee {user.Email} submitted withdrawal request of {amount} from bank account {bankAccount.SoTaiKhoan} for approval");

                        TempData["ActionMessage"] = $"Đã gửi yêu cầu rút {string.Format("{0:N0}", amount)} đ từ tài khoản {bankAccount.SoTaiKhoan}. Đang chờ quản lý duyệt.";
                        return RedirectToAction("Withdraw");
                    }
                    catch (Exception innerEx)
                    {
                        // Rollback nếu có lỗi
                        await transaction.RollbackAsync();
                        _logger.LogError(innerEx, $"Inner error processing withdrawal: {innerEx.Message}");
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
                _logger.LogError(ex, $"Error processing withdrawal: {errorMsg}");
                TempData["ActionMessage"] = $"Có lỗi xảy ra khi rút tiền: {errorMsg}";
                return RedirectToAction("Withdraw");
            }
        }

        // GET: Test page
        public IActionResult Test()
        {
            return View();
        }

        // GET: Lịch sử giao dịch ngân hàng
        public async Task<IActionResult> TransactionHistory(string accountNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            if (string.IsNullOrEmpty(accountNumber))
            {
                TempData["ActionMessage"] = "Vui lòng nhập số tài khoản.";
                return View();
            }

            // Kiểm tra tài khoản ngân hàng
            var bankAccount = await _context.TaiKhoanNganHangs
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.SoTaiKhoan == accountNumber);

            if (bankAccount == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy tài khoản ngân hàng.";
                return View();
            }

            // Lấy lịch sử giao dịch
            var transactions = await _context.GiaoDichNganHangs
                .Where(g => g.SoTaiKhoan == accountNumber)
                .OrderByDescending(g => g.NgayGD)
                .Take(50)
                .ToListAsync();

            ViewBag.BankAccount = bankAccount;
            ViewBag.Transactions = transactions;
            return View();
        }
    }
}