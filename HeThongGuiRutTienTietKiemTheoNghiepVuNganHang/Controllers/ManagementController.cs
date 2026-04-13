using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Text;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Controllers
{
    [Authorize(Roles = "NhanVienQuanLy")]
    public class ManagementController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManagementController> _logger;

        public ManagementController(
            UserManager<User> userManager,
            ApplicationDbContext context,
            ILogger<ManagementController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        // Overview - Trang tổng quan
        public async Task<IActionResult> Overview()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Thống kê tổng quan
            var totalTransactions = await _context.GiaoDichTietKiems.CountAsync();
            var pendingTransactions = await _context.GiaoDichTietKiems.CountAsync(g => g.TrangThaiGD == "ChoDuyet");
            var totalSavingsBooks = await _context.SoTietKiems.CountAsync();
            var activeTellers = await _userManager.GetUsersInRoleAsync("NhanVienGiaoDich");

            ViewBag.User = user;
            ViewBag.TotalTransactions = totalTransactions;
            ViewBag.PendingTransactions = pendingTransactions;
            ViewBag.TotalSavingsBooks = totalSavingsBooks;
            ViewBag.ActiveTellers = activeTellers.Count;

            if (TempData["LoginSuccess"] != null)
            {
                ViewBag.LoginSuccess = TempData["LoginSuccess"];
            }

            return View();
        }

        // Quản lý giao dịch chờ duyệt
        public async Task<IActionResult> TransactionApproval(string customerName = null, string transactionType = null, string employeeName = null, DateTime? fromDate = null, DateTime? toDate = null, decimal? minAmount = null, decimal? maxAmount = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            // Lấy giao dịch tiết kiệm chờ duyệt
            var savingsTransactionsQuery = _context.GiaoDichTietKiems
                .Where(g => g.TrangThaiGD == "ChoDuyet")
                .Include(g => g.SoTietKiem)
                    .ThenInclude(s => s.TaiKhoanNganHang)
                        .ThenInclude(t => t.KhachHang)
                .Include(g => g.NhanVien)
                .AsQueryable();
                
            // Lấy giao dịch ngân hàng chờ duyệt
            var bankTransactionsQuery = _context.GiaoDichNganHangs
                .Where(g => g.TrangThaiGD == "ChoDuyet")
                .Include(g => g.TaiKhoanNganHang)
                    .ThenInclude(t => t.KhachHang)
                .Include(g => g.NhanVien)
                .AsQueryable();
                
            // Áp dụng bộ lọc cho giao dịch tiết kiệm
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                customerName = customerName.Trim();
                savingsTransactionsQuery = savingsTransactionsQuery.Where(g => g.SoTietKiem.TaiKhoanNganHang.KhachHang.HoTen.Contains(customerName));
            }
            
            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                savingsTransactionsQuery = savingsTransactionsQuery.Where(g => g.LoaiGD == transactionType);
            }
            
            if (!string.IsNullOrWhiteSpace(employeeName))
            {
                employeeName = employeeName.Trim();
                savingsTransactionsQuery = savingsTransactionsQuery.Where(g => g.NhanVien.HoTen.Contains(employeeName));
            }
            
            if (fromDate.HasValue)
            {
                savingsTransactionsQuery = savingsTransactionsQuery.Where(g => g.NgayGD >= fromDate.Value);
            }
            
            if (toDate.HasValue)
            {
                savingsTransactionsQuery = savingsTransactionsQuery.Where(g => g.NgayGD <= toDate.Value);
            }
            
            if (minAmount.HasValue)
            {
                savingsTransactionsQuery = savingsTransactionsQuery.Where(g => g.SoTien >= minAmount.Value);
            }
            
            if (maxAmount.HasValue)
            {
                savingsTransactionsQuery = savingsTransactionsQuery.Where(g => g.SoTien <= maxAmount.Value);
            }
            
            // Áp dụng bộ lọc cho giao dịch ngân hàng
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                bankTransactionsQuery = bankTransactionsQuery.Where(g => g.TaiKhoanNganHang.KhachHang.HoTen.Contains(customerName));
            }
            
            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                bankTransactionsQuery = bankTransactionsQuery.Where(g => g.LoaiGD == transactionType);
            }
            
            if (!string.IsNullOrWhiteSpace(employeeName))
            {
                employeeName = employeeName.Trim();
                bankTransactionsQuery = bankTransactionsQuery.Where(g => g.NhanVien.HoTen.Contains(employeeName));
            }
            
            if (fromDate.HasValue)
            {
                bankTransactionsQuery = bankTransactionsQuery.Where(g => g.NgayGD >= fromDate.Value);
            }
            
            if (toDate.HasValue)
            {
                bankTransactionsQuery = bankTransactionsQuery.Where(g => g.NgayGD <= toDate.Value);
            }
            
            if (minAmount.HasValue)
            {
                bankTransactionsQuery = bankTransactionsQuery.Where(g => g.SoTien >= minAmount.Value);
            }
            
            if (maxAmount.HasValue)
            {
                bankTransactionsQuery = bankTransactionsQuery.Where(g => g.SoTien <= maxAmount.Value);
            }

            // Lấy dữ liệu cho cả hai loại giao dịch
            var pendingSavingsTransactions = await savingsTransactionsQuery
                .OrderByDescending(g => g.NgayGD)
                .Take(25)
                .ToListAsync();
                
            var pendingBankTransactions = await bankTransactionsQuery
                .OrderByDescending(g => g.NgayGD)
                .Take(25)
                .ToListAsync();
                
            // Truyền riêng hai danh sách thay vì kết hợp
            ViewBag.PendingSavingsTransactions = pendingSavingsTransactions;
            ViewBag.PendingBankTransactions = pendingBankTransactions;
            
            // Các tham số bộ lọc
            ViewBag.CustomerName = customerName;
            ViewBag.TransactionType = transactionType;
            ViewBag.EmployeeName = employeeName;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.MinAmount = minAmount;
            ViewBag.MaxAmount = maxAmount;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveTransaction(int? maGD, string transactionSource)
        {
            if (!maGD.HasValue)
            {
                TempData["ActionMessage"] = "Mã giao dịch không hợp lệ.";
                return RedirectToAction("TransactionApproval");
            }

            // Xử lý giao dịch tiết kiệm
            if (transactionSource == "Savings")
            {
                var gd = await _context.GiaoDichTietKiems
                    .Include(x => x.SoTietKiem)
                        .ThenInclude(stk => stk.TaiKhoanNganHang)
                        .ThenInclude(t => t.KhachHang)
                    .FirstOrDefaultAsync(x => x.MaGD == maGD.Value);

                if (gd == null)
                {
                    TempData["ActionMessage"] = "Không tìm thấy giao dịch tiết kiệm.";
                    return RedirectToAction("TransactionApproval");
                }

                // Kiểm tra nếu đây là giao dịch mở sổ tiết kiệm chờ duyệt
                // Chỉ xử lý giao dịch do nhân viên tạo ra (MaNV không null)
                if (gd.LoaiGD == "Gửi" && gd.TrangThaiGD == "ChoDuyet" && gd.SoTietKiem != null && !string.IsNullOrEmpty(gd.MaNV))
                {
                    // Cập nhật trạng thái giao dịch
                    gd.TrangThaiGD = "DaDuyet";
                    
                    // Cập nhật trạng thái sổ tiết kiệm
                    gd.SoTietKiem.TrangThai = "Đang hoạt động";
                    
                    // KHI NHÂN VIÊN MỞ SỔ TIẾT KIỆM TẠI QUẦY (DÙNG TIỀN MẶT), KHÔNG TRỪ TIỀN TỪ TÀI KHOẢN NGÂN HÀNG
                    // Không cần thực hiện bất kỳ thao tác nào với số dư tài khoản
                    
                    await _context.SaveChangesAsync();

                    var maKH = gd.SoTietKiem?.TaiKhoanNganHang?.MaKH;
                    if (!string.IsNullOrEmpty(maKH))
                    {
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = maKH,
                            TieuDe = "Mở sổ tiết kiệm thành công",
                            NoiDung = $"Yêu cầu mở sổ tiết kiệm #{gd.SoTietKiem.MaSTK} với số tiền {string.Format("{0:N0}", gd.SoTien)} đ đã được duyệt. Sổ tiết kiệm đã được tạo thành công.",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }

                    TempData["ActionMessage"] = "Đã duyệt giao dịch mở sổ tiết kiệm thành công.";
                    return RedirectToAction("TransactionApproval");
                }
                else if (gd.LoaiGD == "Gửi" && gd.TrangThaiGD == "ChoDuyet" && gd.SoTietKiem != null && string.IsNullOrEmpty(gd.MaNV))
                {
                    // Đây là giao dịch do khách hàng tự tạo - không nên có trong danh sách chờ duyệt
                    // Nhưng nếu có thì chỉ cần cập nhật trạng thái để đồng bộ
                    gd.TrangThaiGD = "DaDuyet";
                    await _context.SaveChangesAsync();
                    
                    TempData["ActionMessage"] = "Giao dịch tiết kiệm đã được xử lý.";
                    return RedirectToAction("TransactionApproval");
                }
                else if (gd.LoaiGD == "Rút" && gd.TrangThaiGD == "ChoDuyet")
                {
                    // Đây là giao dịch tất toán sổ tiết kiệm chờ duyệt
                    // Cập nhật trạng thái giao dịch
                    gd.TrangThaiGD = "DaDuyet";
                    
                    // Cập nhật trạng thái sổ tiết kiệm
                    if (gd.SoTietKiem != null)
                    {
                        gd.SoTietKiem.TrangThai = "Đã tất toán";
                        gd.SoTietKiem.NgayDaoHan = DateTime.Now;
                        
                        // Cộng tiền vào tài khoản ngân hàng
                        if (gd.SoTietKiem.TaiKhoanNganHang != null)
                        {
                            gd.SoTietKiem.TaiKhoanNganHang.SoDu += gd.SoTien;
                        }
                    }
                    
                    await _context.SaveChangesAsync();

                    var maKH = gd.SoTietKiem?.TaiKhoanNganHang?.MaKH;
                    if (!string.IsNullOrEmpty(maKH))
                    {
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = maKH,
                            TieuDe = "Tất toán sổ tiết kiệm thành công",
                            NoiDung = $"Yêu cầu tất toán sổ tiết kiệm #{gd.SoTietKiem?.MaSTK} đã được duyệt. Số tiền {string.Format("{0:N0}", gd.SoTien)} đ đã được chuyển về tài khoản ngân hàng của bạn.",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }

                    TempData["ActionMessage"] = "Đã duyệt giao dịch tất toán sổ tiết kiệm thành công.";
                    return RedirectToAction("TransactionApproval");
                }
                else
                {
                    // Xử lý các giao dịch tiết kiệm khác như bình thường
                    gd.TrangThaiGD = "DaDuyet";
                    await _context.SaveChangesAsync();

                    var maKH = gd.SoTietKiem?.TaiKhoanNganHang?.MaKH;
                    if (!string.IsNullOrEmpty(maKH))
                    {
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = maKH,
                            TieuDe = "Giao dịch tiết kiệm được duyệt",
                            NoiDung = $"Giao dịch tiết kiệm #{gd.MaGD} đã được duyệt bởi Quản lý",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }

                    TempData["ActionMessage"] = "Đã duyệt giao dịch tiết kiệm thành công.";
                    return RedirectToAction("TransactionApproval");
                }
            }
            // Xử lý giao dịch ngân hàng
            else if (transactionSource == "Bank")
            {
                var gd = await _context.GiaoDichNganHangs
                    .Include(x => x.TaiKhoanNganHang)
                    .FirstOrDefaultAsync(x => x.MaGD == maGD.Value);

                if (gd == null)
                {
                    TempData["ActionMessage"] = "Không tìm thấy giao dịch ngân hàng.";
                    return RedirectToAction("TransactionApproval");
                }

                // Xử lý giao dịch nạp tiền
                if (gd.LoaiGD == "Nạp tiền" && gd.TrangThaiGD == "ChoDuyet")
                {
                    // Cập nhật trạng thái giao dịch
                    gd.TrangThaiGD = "DaDuyet";
                    
                    // Cộng tiền vào tài khoản ngân hàng
                    if (gd.TaiKhoanNganHang != null)
                    {
                        gd.TaiKhoanNganHang.SoDu += gd.SoTien;
                    }
                    
                    await _context.SaveChangesAsync();

                    var maKH = gd.TaiKhoanNganHang?.MaKH;
                    if (!string.IsNullOrEmpty(maKH))
                    {
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = maKH,
                            TieuDe = "Nạp tiền thành công",
                            NoiDung = $"Yêu cầu nạp {string.Format("{0:N0}", gd.SoTien)} đ vào tài khoản ngân hàng {gd.SoTaiKhoan} đã được duyệt. Số dư hiện tại: {string.Format("{0:N0}", gd.TaiKhoanNganHang?.SoDu)} đ.",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }

                    TempData["ActionMessage"] = "Đã duyệt giao dịch nạp tiền thành công.";
                    return RedirectToAction("TransactionApproval");
                }
                // Xử lý giao dịch rút tiền
                else if (gd.LoaiGD == "Rút tiền" && gd.TrangThaiGD == "ChoDuyet")
                {
                    // Cập nhật trạng thái giao dịch
                    gd.TrangThaiGD = "DaDuyet";
                    
                    // Trừ tiền từ tài khoản ngân hàng
                    if (gd.TaiKhoanNganHang != null)
                    {
                        gd.TaiKhoanNganHang.SoDu -= gd.SoTien;
                    }
                    
                    await _context.SaveChangesAsync();

                    var maKH = gd.TaiKhoanNganHang?.MaKH;
                    if (!string.IsNullOrEmpty(maKH))
                    {
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = maKH,
                            TieuDe = "Rút tiền thành công",
                            NoiDung = $"Yêu cầu rút {string.Format("{0:N0}", gd.SoTien)} đ từ tài khoản ngân hàng {gd.SoTaiKhoan} đã được duyệt. Số dư hiện tại: {string.Format("{0:N0}", gd.TaiKhoanNganHang?.SoDu)} đ.",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }

                    TempData["ActionMessage"] = "Đã duyệt giao dịch rút tiền thành công.";
                    return RedirectToAction("TransactionApproval");
                }
                else
                {
                    // Xử lý các giao dịch ngân hàng khác như bình thường
                    gd.TrangThaiGD = "DaDuyet";
                    await _context.SaveChangesAsync();

                    var maKH = gd.TaiKhoanNganHang?.MaKH;
                    if (!string.IsNullOrEmpty(maKH))
                    {
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = maKH,
                            TieuDe = "Giao dịch ngân hàng được duyệt",
                            NoiDung = $"Giao dịch ngân hàng #{gd.MaGD} đã được duyệt bởi Quản lý",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }

                    TempData["ActionMessage"] = "Đã duyệt giao dịch ngân hàng thành công.";
                    return RedirectToAction("TransactionApproval");
                }
            }
            else
            {
                TempData["ActionMessage"] = "Loại giao dịch không hợp lệ.";
                return RedirectToAction("TransactionApproval");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectTransaction(int? maGD, string reason, string transactionSource)
        {
            if (!maGD.HasValue)
            {
                TempData["ActionMessage"] = "Mã giao dịch không hợp lệ.";
                return RedirectToAction("TransactionApproval");
            }

            // Xử lý giao dịch tiết kiệm
            if (transactionSource == "Savings")
            {
                var gd = await _context.GiaoDichTietKiems
                    .Include(x => x.SoTietKiem)
                        .ThenInclude(stk => stk.TaiKhoanNganHang)
                    .FirstOrDefaultAsync(x => x.MaGD == maGD.Value);

                if (gd == null)
                {
                    TempData["ActionMessage"] = "Không tìm thấy giao dịch tiết kiệm.";
                    return RedirectToAction("TransactionApproval");
                }

                // Kiểm tra nếu đây là giao dịch mở sổ tiết kiệm chờ duyệt
                // Chỉ xử lý giao dịch do nhân viên tạo ra (MaNV không null)
                if (gd.LoaiGD == "Gửi" && gd.TrangThaiGD == "ChoDuyet" && gd.SoTietKiem != null && !string.IsNullOrEmpty(gd.MaNV))
                {
                    // Cập nhật trạng thái giao dịch
                    gd.TrangThaiGD = "TuChoi";
                    
                    // Cập nhật trạng thái sổ tiết kiệm
                    gd.SoTietKiem.TrangThai = "Bị từ chối";
                    
                    await _context.SaveChangesAsync();

                    var maKH = gd.SoTietKiem?.TaiKhoanNganHang?.MaKH;
                    if (!string.IsNullOrEmpty(maKH))
                    {
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = maKH,
                            TieuDe = "Yêu cầu mở sổ tiết kiệm bị từ chối",
                            NoiDung = string.IsNullOrWhiteSpace(reason) ? 
                                $"Yêu cầu mở sổ tiết kiệm #{gd.SoTietKiem.MaSTK} với số tiền {string.Format("{0:N0}", gd.SoTien)} đ đã bị từ chối." : 
                                $"Yêu cầu mở sổ tiết kiệm #{gd.SoTietKiem.MaSTK} với số tiền {string.Format("{0:N0}", gd.SoTien)} đ đã bị từ chối. Lý do: {reason}",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }

                    TempData["ActionMessage"] = "Đã từ chối giao dịch mở sổ tiết kiệm.";
                    return RedirectToAction("TransactionApproval");
                }
                else if (gd.LoaiGD == "Rút" && gd.TrangThaiGD == "ChoDuyet")
                {
                    // Đây là giao dịch tất toán sổ tiết kiệm chờ duyệt
                    // Cập nhật trạng thái giao dịch
                    gd.TrangThaiGD = "TuChoi";
                    
                    // Cập nhật trạng thái sổ tiết kiệm
                    if (gd.SoTietKiem != null)
                    {
                        gd.SoTietKiem.TrangThai = "Đang hoạt động"; // Trở lại trạng thái hoạt động
                    }
                    
                    await _context.SaveChangesAsync();

                    var maKH = gd.SoTietKiem?.TaiKhoanNganHang?.MaKH;
                    if (!string.IsNullOrEmpty(maKH))
                    {
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = maKH,
                            TieuDe = "Yêu cầu tất toán sổ tiết kiệm bị từ chối",
                            NoiDung = string.IsNullOrWhiteSpace(reason) ? 
                                $"Yêu cầu tất toán sổ tiết kiệm #{gd.SoTietKiem?.MaSTK} đã bị từ chối." : 
                                $"Yêu cầu tất toán sổ tiết kiệm #{gd.SoTietKiem?.MaSTK} đã bị từ chối. Lý do: {reason}",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }

                    TempData["ActionMessage"] = "Đã từ chối giao dịch tất toán sổ tiết kiệm.";
                    return RedirectToAction("TransactionApproval");
                }
                else
                {
                    // Xử lý các giao dịch tiết kiệm khác như bình thường
                    gd.TrangThaiGD = "TuChoi";
                    await _context.SaveChangesAsync();

                    var maKH = gd.SoTietKiem?.TaiKhoanNganHang?.MaKH;
                    if (!string.IsNullOrEmpty(maKH))
                    {
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = maKH,
                            TieuDe = "Giao dịch tiết kiệm bị từ chối",
                            NoiDung = string.IsNullOrWhiteSpace(reason) ? 
                                $"Giao dịch tiết kiệm #{gd.MaGD} đã bị từ chối." : 
                                $"Giao dịch tiết kiệm #{gd.MaGD} đã bị từ chối. Lý do: {reason}",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }

                    TempData["ActionMessage"] = "Đã từ chối giao dịch tiết kiệm.";
                    return RedirectToAction("TransactionApproval");
                }
            }
            // Xử lý giao dịch ngân hàng
            else if (transactionSource == "Bank")
            {
                var gd = await _context.GiaoDichNganHangs
                    .Include(x => x.TaiKhoanNganHang)
                    .FirstOrDefaultAsync(x => x.MaGD == maGD.Value);

                if (gd == null)
                {
                    TempData["ActionMessage"] = "Không tìm thấy giao dịch ngân hàng.";
                    return RedirectToAction("TransactionApproval");
                }

                // Xử lý giao dịch nạp tiền
                if (gd.LoaiGD == "Nạp tiền" && gd.TrangThaiGD == "ChoDuyet")
                {
                    // Cập nhật trạng thái giao dịch
                    gd.TrangThaiGD = "TuChoi";
                    
                    await _context.SaveChangesAsync();

                    var maKH = gd.TaiKhoanNganHang?.MaKH;
                    if (!string.IsNullOrEmpty(maKH))
                    {
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = maKH,
                            TieuDe = "Yêu cầu nạp tiền bị từ chối",
                            NoiDung = string.IsNullOrWhiteSpace(reason) ? 
                                $"Yêu cầu nạp {string.Format("{0:N0}", gd.SoTien)} đ vào tài khoản ngân hàng {gd.SoTaiKhoan} đã bị từ chối." : 
                                $"Yêu cầu nạp {string.Format("{0:N0}", gd.SoTien)} đ vào tài khoản ngân hàng {gd.SoTaiKhoan} đã bị từ chối. Lý do: {reason}",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }

                    TempData["ActionMessage"] = "Đã từ chối giao dịch nạp tiền.";
                    return RedirectToAction("TransactionApproval");
                }
                // Xử lý giao dịch rút tiền
                else if (gd.LoaiGD == "Rút tiền" && gd.TrangThaiGD == "ChoDuyet")
                {
                    // Cập nhật trạng thái giao dịch
                    gd.TrangThaiGD = "TuChoi";
                    
                    await _context.SaveChangesAsync();

                    var maKH = gd.TaiKhoanNganHang?.MaKH;
                    if (!string.IsNullOrEmpty(maKH))
                    {
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = maKH,
                            TieuDe = "Yêu cầu rút tiền bị từ chối",
                            NoiDung = string.IsNullOrWhiteSpace(reason) ? 
                                $"Yêu cầu rút {string.Format("{0:N0}", gd.SoTien)} đ từ tài khoản ngân hàng {gd.SoTaiKhoan} đã bị từ chối." : 
                                $"Yêu cầu rút {string.Format("{0:N0}", gd.SoTien)} đ từ tài khoản ngân hàng {gd.SoTaiKhoan} đã bị từ chối. Lý do: {reason}",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }

                    TempData["ActionMessage"] = "Đã từ chối giao dịch rút tiền.";
                    return RedirectToAction("TransactionApproval");
                }
                else
                {
                    // Xử lý các giao dịch ngân hàng khác như bình thường
                    gd.TrangThaiGD = "TuChoi";
                    await _context.SaveChangesAsync();

                    var maKH = gd.TaiKhoanNganHang?.MaKH;
                    if (!string.IsNullOrEmpty(maKH))
                    {
                        _context.ThongBaos.Add(new ThongBao
                        {
                            MaKH = maKH,
                            TieuDe = "Giao dịch ngân hàng bị từ chối",
                            NoiDung = string.IsNullOrWhiteSpace(reason) ? 
                                $"Giao dịch ngân hàng #{gd.MaGD} đã bị từ chối." : 
                                $"Giao dịch ngân hàng #{gd.MaGD} đã bị từ chối. Lý do: {reason}",
                            TrangThai = "Chưa đọc",
                            NgayGui = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }

                    TempData["ActionMessage"] = "Đã từ chối giao dịch ngân hàng.";
                    return RedirectToAction("TransactionApproval");
                }
            }
            else
            {
                TempData["ActionMessage"] = "Loại giao dịch không hợp lệ.";
                return RedirectToAction("TransactionApproval");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ShowRejectForm(int? maGD, string transactionSource)
        {
            if (!maGD.HasValue)
            {
                TempData["ActionMessage"] = "Mã giao dịch không hợp lệ.";
                return RedirectToAction("TransactionApproval");
            }

            // Xử lý giao dịch tiết kiệm
            if (transactionSource == "Savings")
            {
                var gd = await _context.GiaoDichTietKiems
                    .Include(x => x.SoTietKiem)
                        .ThenInclude(stk => stk.TaiKhoanNganHang)
                            .ThenInclude(tk => tk.KhachHang)
                    .FirstOrDefaultAsync(x => x.MaGD == maGD.Value);

                if (gd == null)
                {
                    TempData["ActionMessage"] = "Không tìm thấy giao dịch tiết kiệm.";
                    return RedirectToAction("TransactionApproval");
                }

                ViewBag.Transaction = gd;
                ViewBag.TransactionSource = "Savings";
                return View("RejectTransactionForm");
            }
            // Xử lý giao dịch ngân hàng
            else if (transactionSource == "Bank")
            {
                var gd = await _context.GiaoDichNganHangs
                    .Include(x => x.TaiKhoanNganHang)
                        .ThenInclude(tk => tk.KhachHang)
                    .FirstOrDefaultAsync(x => x.MaGD == maGD.Value);

                if (gd == null)
                {
                    TempData["ActionMessage"] = "Không tìm thấy giao dịch ngân hàng.";
                    return RedirectToAction("TransactionApproval");
                }

                ViewBag.Transaction = gd;
                ViewBag.TransactionSource = "Bank";
                return View("RejectTransactionForm");
            }
            else
            {
                TempData["ActionMessage"] = "Loại giao dịch không hợp lệ.";
                return RedirectToAction("TransactionApproval");
            }
        }

        // Quản lý sổ tiết kiệm
        public async Task<IActionResult> SavingsBookManagement(
            string customerName = null,
            string accountNumber = null,
            decimal? minAmount = null,
            decimal? maxAmount = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string status = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            // Lấy tất cả sổ tiết kiệm với includes
            var savingsQuery = _context.SoTietKiems
                .Include(s => s.TaiKhoanNganHang)
                    .ThenInclude(t => t.KhachHang)
                .AsQueryable();
                
            // Áp dụng bộ lọc
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                savingsQuery = savingsQuery.Where(s => s.TaiKhoanNganHang.KhachHang.HoTen.Contains(customerName.Trim()));
            }
            
            if (!string.IsNullOrWhiteSpace(accountNumber))
            {
                savingsQuery = savingsQuery.Where(s => s.TaiKhoanNganHang.SoTaiKhoan.Contains(accountNumber.Trim()));
            }
            
            if (minAmount.HasValue)
            {
                savingsQuery = savingsQuery.Where(s => s.SoTienGui >= minAmount.Value);
            }
            
            if (maxAmount.HasValue)
            {
                savingsQuery = savingsQuery.Where(s => s.SoTienGui <= maxAmount.Value);
            }
            
            if (fromDate.HasValue)
            {
                savingsQuery = savingsQuery.Where(s => s.NgayMoSo >= fromDate.Value);
            }
            
            if (toDate.HasValue)
            {
                savingsQuery = savingsQuery.Where(s => s.NgayMoSo <= toDate.Value);
            }
            
            if (!string.IsNullOrWhiteSpace(status))
            {
                savingsQuery = savingsQuery.Where(s => s.TrangThai == status);
            }

            // Lấy kết quả sau khi áp dụng bộ lọc
            var allSavingsBooks = await savingsQuery
                .OrderByDescending(s => s.NgayMoSo)
                .Take(100)
                .ToListAsync();

            // Sổ sắp đến hạn (trong 30 ngày tới)
            var upcomingMaturity = allSavingsBooks
                .Where(s => s.NgayDaoHan <= DateTime.Now.AddDays(30) && s.NgayDaoHan >= DateTime.Now && s.TrangThai == "Đang hoạt động")
                .ToList();

            // Truyền dữ liệu và giá trị bộ lọc về view
            ViewBag.AllSavingsBooks = allSavingsBooks;
            ViewBag.UpcomingMaturity = upcomingMaturity;
            ViewBag.CustomerName = customerName;
            ViewBag.AccountNumber = accountNumber;
            ViewBag.MinAmount = minAmount;
            ViewBag.MaxAmount = maxAmount;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status;
            
            return View();
        }

        // Xuất báo cáo sổ tiết kiệm
        [HttpGet]
        public async Task<IActionResult> ExportSavingsReport(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            // Lấy tất cả sổ tiết kiệm với includes
            var savingsQuery = _context.SoTietKiems
                .Include(s => s.TaiKhoanNganHang)
                    .ThenInclude(t => t.KhachHang)
                .AsQueryable();
                
            // Áp dụng bộ lọc ngày mở sổ nếu có
            if (fromDate.HasValue)
            {
                savingsQuery = savingsQuery.Where(s => s.NgayMoSo >= fromDate.Value);
            }
            
            if (toDate.HasValue)
            {
                savingsQuery = savingsQuery.Where(s => s.NgayMoSo <= toDate.Value);
            }

            var allSavingsBooks = await savingsQuery
                .OrderByDescending(s => s.NgayMoSo)
                .ToListAsync();

            // Sổ sắp đến hạn (trong tháng tới)
            var startDate = DateTime.Now.Date;
            var endDate = DateTime.Now.Date.AddMonths(1);
            var upcomingMaturity = allSavingsBooks
                .Where(s => s.NgayDaoHan >= startDate && s.NgayDaoHan <= endDate && s.TrangThai == "Đang hoạt động")
                .ToList();

            // Tạo nội dung CSV
            var csvContent = GenerateSavingsReportCsv(allSavingsBooks, upcomingMaturity, fromDate, toDate);
            var fileName = $"BaoCaoSoTietKiem_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            
            // Trả về file CSV
            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
        }

        // Tạo nội dung báo cáo CSV
        private string GenerateSavingsReportCsv(List<SoTietKiem> allSavingsBooks, List<SoTietKiem> upcomingMaturity, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("BÁO CÁO SỔ TIẾT KIỆM");
            csv.AppendLine($"Ngày tạo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            
            // Thông tin bộ lọc nếu có
            if (fromDate.HasValue || toDate.HasValue)
            {
                var filterInfo = "Bộ lọc: ";
                if (fromDate.HasValue)
                    filterInfo += $"Từ ngày {fromDate.Value:dd/MM/yyyy} ";
                if (toDate.HasValue)
                    filterInfo += $"Đến ngày {toDate.Value:dd/MM/yyyy}";
                csv.AppendLine(filterInfo);
            }
            
            csv.AppendLine();
            
            // Danh sách tất cả sổ tiết kiệm đã mở
            csv.AppendLine("DANH SÁCH TẤT CẢ SỔ TIẾT KIỆM ĐÃ MỞ");
            csv.AppendLine("Mã STK,Khách hàng,Số tài khoản,Số tiền gửi,Lãi suất,Ngày mở,Ngày đáo hạn,Trạng thái");
            
            foreach (var s in allSavingsBooks)
            {
                csv.AppendLine($"{s.MaSTK},{s.TaiKhoanNganHang?.KhachHang?.HoTen ?? "N/A"},{s.TaiKhoanNganHang?.SoTaiKhoan ?? "N/A"},{s.SoTienGui:#,##0},{s.LaiSuat}%,{s.NgayMoSo:dd/MM/yyyy},{s.NgayDaoHan:dd/MM/yyyy},{s.TrangThai}");
            }
            
            csv.AppendLine();
            
            // Danh sách sổ tiết kiệm sắp đáo hạn trong tháng tới
            csv.AppendLine("DANH SÁCH SỔ TIẾT KIỆM SẮP ĐÁO HẠN TRONG THÁNG TỚI");
            csv.AppendLine("Mã STK,Khách hàng,Số tài khoản,Số tiền gửi,Lãi suất,Ngày mở,Ngày đáo hạn,Trạng thái");
            
            foreach (var s in upcomingMaturity)
            {
                csv.AppendLine($"{s.MaSTK},{s.TaiKhoanNganHang?.KhachHang?.HoTen ?? "N/A"},{s.TaiKhoanNganHang?.SoTaiKhoan ?? "N/A"},{s.SoTienGui:#,##0},{s.LaiSuat}%,{s.NgayMoSo:dd/MM/yyyy},{s.NgayDaoHan:dd/MM/yyyy},{s.TrangThai}");
            }
            
            return csv.ToString();
        }

        // Quản lý nhân viên giao dịch
        public async Task<IActionResult> TellerManagement(string name = null, string email = null, string username = null, int? minTransactions = null, int? maxTransactions = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var tellers = await _userManager.GetUsersInRoleAsync("NhanVienGiaoDich");
            
            // Lấy thống kê hiệu suất cho mỗi teller
            var tellerStats = new List<dynamic>();
            foreach (var teller in tellers)
            {
                var transactionCount = await _context.GiaoDichTietKiems
                    .CountAsync(g => g.MaNV == teller.Id);
                
                var todayCount = await _context.GiaoDichTietKiems
                    .CountAsync(g => g.MaNV == teller.Id && g.NgayGD.Date == DateTime.Today);

                tellerStats.Add(new
                {
                    Teller = teller,
                    TotalTransactions = transactionCount,
                    TodayTransactions = todayCount
                });
            }
            
            // Bộ lọc nâng cao
            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();
                tellerStats = tellerStats.Where(s => s.Teller.HoTen.Contains(name) || s.Teller.UserName.Contains(name)).ToList();
            }
            
            if (!string.IsNullOrWhiteSpace(email))
            {
                email = email.Trim();
                tellerStats = tellerStats.Where(s => s.Teller.Email.Contains(email)).ToList();
            }
            
            if (!string.IsNullOrWhiteSpace(username))
            {
                username = username.Trim();
                tellerStats = tellerStats.Where(s => s.Teller.UserName.Contains(username)).ToList();
            }
            
            if (minTransactions.HasValue)
            {
                tellerStats = tellerStats.Where(s => s.TotalTransactions >= minTransactions.Value).ToList();
            }
            
            if (maxTransactions.HasValue)
            {
                tellerStats = tellerStats.Where(s => s.TotalTransactions <= maxTransactions.Value).ToList();
            }

            ViewBag.TellerStats = tellerStats;
            ViewBag.Name = name;
            ViewBag.Email = email;
            ViewBag.Username = username;
            ViewBag.MinTransactions = minTransactions;
            ViewBag.MaxTransactions = maxTransactions;
            return View();
        }

        // Xem báo cáo giao dịch ngày của nhân viên
        public async Task<IActionResult> DailyReports()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            // Lấy tất cả báo cáo giao dịch ngày được tạo bởi nhân viên giao dịch
            var reports = await _context.BaoCaoGiaoDichNgays
                .Include(r => r.NhanVien)
                .OrderByDescending(r => r.NgayBaoCao)
                .ThenByDescending(r => r.NgayLap)
                .ToListAsync();

            return View(reports);
        }

        // Xem chi tiết báo cáo
        public async Task<IActionResult> DailyReportDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var report = await _context.BaoCaoGiaoDichNgays
                .Include(r => r.NhanVien)
                .FirstOrDefaultAsync(r => r.MaBaoCao == id);

            if (report == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy báo cáo.";
                return RedirectToAction("DailyReports");
            }

            // Lấy danh sách chi tiết giao dịch cho báo cáo
            ViewBag.TransactionDetails = await GetTransactionDetails(report.MaNV, report.NgayBaoCao);

            return View(report);
        }

        // Lấy danh sách chi tiết giao dịch
        private async Task<List<object>> GetTransactionDetails(string employeeId, DateTime reportDate)
        {
            var transactionDetails = new List<object>();

            // Lấy giao dịch tiết kiệm
            var savingsTransactions = await _context.GiaoDichTietKiems
                .Include(g => g.SoTietKiem)
                .ThenInclude(s => s.TaiKhoanNganHang)
                .ThenInclude(t => t.KhachHang)
                .Where(g => g.MaNV == employeeId && g.NgayGD.Date == reportDate.Date)
                .ToListAsync();

            foreach (var transaction in savingsTransactions)
            {
                transactionDetails.Add(new
                {
                    MaGD = transaction.MaGD,
                    LoaiGD = transaction.LoaiGD,
                    SoTien = transaction.SoTien,
                    SoTaiKhoan = transaction.SoTietKiem?.TaiKhoanNganHang?.SoTaiKhoan,
                    HoTenKH = transaction.SoTietKiem?.TaiKhoanNganHang?.KhachHang?.HoTen,
                    ThoiGian = transaction.NgayGD,
                    TrangThai = transaction.TrangThaiGD
                });
            }

            // Lấy giao dịch ngân hàng
            var bankTransactions = await _context.GiaoDichNganHangs
                .Include(g => g.TaiKhoanNganHang)
                .ThenInclude(t => t.KhachHang)
                .Where(g => g.MaNV == employeeId && g.NgayGD.Date == reportDate.Date)
                .ToListAsync();

            foreach (var transaction in bankTransactions)
            {
                transactionDetails.Add(new
                {
                    MaGD = transaction.MaGD,
                    LoaiGD = transaction.LoaiGD,
                    SoTien = transaction.SoTien,
                    SoTaiKhoan = transaction.SoTaiKhoan,
                    HoTenKH = transaction.TaiKhoanNganHang?.KhachHang?.HoTen,
                    ThoiGian = transaction.NgayGD,
                    TrangThai = transaction.TrangThaiGD
                });
            }

            // Sắp xếp theo thời gian
            return transactionDetails.OrderBy(t => ((dynamic)t).ThoiGian).ToList();
        }

        // Quản lý khách hàng
        public async Task<IActionResult> CustomerManagement(string q, string sortBy = "name", string sortOrder = "asc", string email = null, string phone = null, string cccd = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            // Chỉ hiển thị những tài khoản có vai trò là khách hàng
            var customers = await _userManager.GetUsersInRoleAsync("KhachHang");
            
            IEnumerable<User> query = customers;
            
            // Bộ lọc tìm kiếm
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(u => u.HoTen.Contains(q) || u.Email.Contains(q) || u.SDT.Contains(q) || u.CCCD.Contains(q));
            }
            
            // Bộ lọc nâng cao
            if (!string.IsNullOrWhiteSpace(email))
            {
                email = email.Trim();
                query = query.Where(u => u.Email.Contains(email));
            }
            
            if (!string.IsNullOrWhiteSpace(phone))
            {
                phone = phone.Trim();
                query = query.Where(u => u.SDT.Contains(phone));
            }
            
            if (!string.IsNullOrWhiteSpace(cccd))
            {
                cccd = cccd.Trim();
                query = query.Where(u => u.CCCD.Contains(cccd));
            }

            // Sắp xếp
            switch (sortBy.ToLower())
            {
                case "email":
                    query = sortOrder.ToLower() == "desc" ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email);
                    break;
                case "phone":
                    query = sortOrder.ToLower() == "desc" ? query.OrderByDescending(u => u.SDT) : query.OrderBy(u => u.SDT);
                    break;
                case "cccd":
                    query = sortOrder.ToLower() == "desc" ? query.OrderByDescending(u => u.CCCD) : query.OrderBy(u => u.CCCD);
                    break;
                case "name":
                default:
                    query = sortOrder.ToLower() == "desc" ? query.OrderByDescending(u => u.HoTen) : query.OrderBy(u => u.HoTen);
                    break;
            }

            var users = query.Take(200).ToList();
            ViewBag.Customers = users;
            ViewBag.Query = q;
            ViewBag.Email = email;
            ViewBag.Phone = phone;
            ViewBag.CCCD = cccd;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            return View();
        }

        // Chi tiết khách hàng
        public async Task<IActionResult> CustomerDetails(string id, string savingsType = null, DateTime? savingsFromDate = null, DateTime? savingsToDate = null, decimal? savingsMinAmount = null, decimal? savingsMaxAmount = null, string bankType = null, DateTime? bankFromDate = null, DateTime? bankToDate = null, decimal? bankMinAmount = null, decimal? bankMaxAmount = null)
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
            
            // Lấy giao dịch tiết kiệm liên quan đến sổ tiết kiệm của khách hàng
            var savingsQuery = _context.GiaoDichTietKiems
                .Where(g => g.MaSTK.HasValue && soTietKiemIds.Contains(g.MaSTK.Value))
                .Include(g => g.SoTietKiem)
                .Include(g => g.NhanVien)
                .AsQueryable();
                
            // Bộ lọc giao dịch tiết kiệm
            if (!string.IsNullOrWhiteSpace(savingsType))
            {
                savingsQuery = savingsQuery.Where(g => g.LoaiGD == savingsType);
            }
            
            if (savingsFromDate.HasValue)
            {
                savingsQuery = savingsQuery.Where(g => g.NgayGD >= savingsFromDate.Value);
            }
            
            if (savingsToDate.HasValue)
            {
                savingsQuery = savingsQuery.Where(g => g.NgayGD <= savingsToDate.Value);
            }
            
            if (savingsMinAmount.HasValue)
            {
                savingsQuery = savingsQuery.Where(g => g.SoTien >= savingsMinAmount.Value);
            }
            
            if (savingsMaxAmount.HasValue)
            {
                savingsQuery = savingsQuery.Where(g => g.SoTien <= savingsMaxAmount.Value);
            }

            var savingsTransactions = await savingsQuery
                .OrderByDescending(g => g.NgayGD)
                .Take(20)
                .ToListAsync();
                
            // Lấy giao dịch ngân hàng liên quan đến tài khoản ngân hàng của khách hàng
            var bankAccountNumbers = bankAccounts.Select(b => b.SoTaiKhoan).ToList();
            var bankQuery = _context.GiaoDichNganHangs
                .Where(g => bankAccountNumbers.Contains(g.SoTaiKhoan))
                .Include(g => g.TaiKhoanNganHang)
                .Include(g => g.NhanVien)
                .AsQueryable();
                
            // Bộ lọc giao dịch ngân hàng
            if (!string.IsNullOrWhiteSpace(bankType))
            {
                bankQuery = bankQuery.Where(g => g.LoaiGD == bankType);
            }
            
            if (bankFromDate.HasValue)
            {
                bankQuery = bankQuery.Where(g => g.NgayGD >= bankFromDate.Value);
            }
            
            if (bankToDate.HasValue)
            {
                bankQuery = bankQuery.Where(g => g.NgayGD <= bankToDate.Value);
            }
            
            if (bankMinAmount.HasValue)
            {
                bankQuery = bankQuery.Where(g => g.SoTien >= bankMinAmount.Value);
            }
            
            if (bankMaxAmount.HasValue)
            {
                bankQuery = bankQuery.Where(g => g.SoTien <= bankMaxAmount.Value);
            }

            var bankTransactions = await bankQuery
                .OrderByDescending(g => g.NgayGD)
                .Take(20)
                .ToListAsync();

            ViewBag.Customer = kh;
            ViewBag.BankAccounts = bankAccounts;
            ViewBag.SavingsTransactions = savingsTransactions;
            ViewBag.BankTransactions = bankTransactions;
            ViewBag.TotalSavings = bankAccounts.Sum(b => b.SoTietKiems.Sum(s => s.SoTienGui));
            
            // Truyền giá trị bộ lọc về view
            ViewBag.SavingsType = savingsType;
            ViewBag.SavingsFromDate = savingsFromDate?.ToString("yyyy-MM-dd");
            ViewBag.SavingsToDate = savingsToDate?.ToString("yyyy-MM-dd");
            ViewBag.SavingsMinAmount = savingsMinAmount;
            ViewBag.SavingsMaxAmount = savingsMaxAmount;
            
            ViewBag.BankType = bankType;
            ViewBag.BankFromDate = bankFromDate?.ToString("yyyy-MM-dd");
            ViewBag.BankToDate = bankToDate?.ToString("yyyy-MM-dd");
            ViewBag.BankMinAmount = bankMinAmount;
            ViewBag.BankMaxAmount = bankMaxAmount;
            
            return View();
        }

        // Báo cáo thống kê
        public async Task<IActionResult> Reports()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var thisYear = new DateTime(today.Year, 1, 1);

            // Thống kê giao dịch
            var monthTransactions = await _context.GiaoDichTietKiems
                .Where(g => g.NgayGD >= thisMonth)
                .ToListAsync();

            var yearTransactions = await _context.GiaoDichTietKiems
                .Where(g => g.NgayGD >= thisYear)
                .ToListAsync();

            ViewBag.MonthTransactionCount = monthTransactions.Count;
            ViewBag.MonthTransactionAmount = monthTransactions.Sum(g => g.SoTien);
            ViewBag.YearTransactionCount = yearTransactions.Count;
            ViewBag.YearTransactionAmount = yearTransactions.Sum(g => g.SoTien);

            return View();
        }

        // Xem chi tiết giao dịch của nhân viên
        public async Task<IActionResult> EmployeeTransactionDetails(string id, string savingsCustomer = null, string savingsType = null, DateTime? savingsFromDate = null, DateTime? savingsToDate = null, decimal? savingsMinAmount = null, decimal? savingsMaxAmount = null, string bankCustomer = null, string bankType = null, DateTime? bankFromDate = null, DateTime? bankToDate = null, decimal? bankMinAmount = null, decimal? bankMaxAmount = null)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            
            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null || !await _userManager.IsInRoleAsync(employee, "NhanVienGiaoDich"))
            {
                TempData["ActionMessage"] = "Không tìm thấy nhân viên giao dịch.";
                return RedirectToAction("TellerManagement");
            }

            // Lấy giao dịch tiết kiệm do nhân viên này thực hiện
            var savingsQuery = _context.GiaoDichTietKiems
                .Where(g => g.MaNV == id)
                .Include(g => g.SoTietKiem)
                    .ThenInclude(s => s.TaiKhoanNganHang)
                        .ThenInclude(t => t.KhachHang)
                .Include(g => g.NhanVien)
                .AsQueryable();
                
            // Bộ lọc giao dịch tiết kiệm
            if (!string.IsNullOrWhiteSpace(savingsCustomer))
            {
                savingsQuery = savingsQuery.Where(g => g.SoTietKiem.TaiKhoanNganHang.KhachHang.HoTen.Contains(savingsCustomer));
            }
            
            if (!string.IsNullOrWhiteSpace(savingsType))
            {
                savingsQuery = savingsQuery.Where(g => g.LoaiGD == savingsType);
            }
            
            if (savingsFromDate.HasValue)
            {
                savingsQuery = savingsQuery.Where(g => g.NgayGD >= savingsFromDate.Value);
            }
            
            if (savingsToDate.HasValue)
            {
                savingsQuery = savingsQuery.Where(g => g.NgayGD <= savingsToDate.Value);
            }
            
            if (savingsMinAmount.HasValue)
            {
                savingsQuery = savingsQuery.Where(g => g.SoTien >= savingsMinAmount.Value);
            }
            
            if (savingsMaxAmount.HasValue)
            {
                savingsQuery = savingsQuery.Where(g => g.SoTien <= savingsMaxAmount.Value);
            }

            var savingsTransactions = await savingsQuery
                .OrderByDescending(g => g.NgayGD)
                .Take(50)
                .ToListAsync();

            // Lấy giao dịch ngân hàng do nhân viên này thực hiện
            var bankQuery = _context.GiaoDichNganHangs
                .Where(g => g.MaNV == id)
                .Include(g => g.TaiKhoanNganHang)
                    .ThenInclude(t => t.KhachHang)
                .Include(g => g.NhanVien)
                .AsQueryable();
                
            // Bộ lọc giao dịch ngân hàng
            if (!string.IsNullOrWhiteSpace(bankCustomer))
            {
                bankQuery = bankQuery.Where(g => g.TaiKhoanNganHang.KhachHang.HoTen.Contains(bankCustomer));
            }
            
            if (!string.IsNullOrWhiteSpace(bankType))
            {
                bankQuery = bankQuery.Where(g => g.LoaiGD == bankType);
            }
            
            if (bankFromDate.HasValue)
            {
                bankQuery = bankQuery.Where(g => g.NgayGD >= bankFromDate.Value);
            }
            
            if (bankToDate.HasValue)
            {
                bankQuery = bankQuery.Where(g => g.NgayGD <= bankToDate.Value);
            }
            
            if (bankMinAmount.HasValue)
            {
                bankQuery = bankQuery.Where(g => g.SoTien >= bankMinAmount.Value);
            }
            
            if (bankMaxAmount.HasValue)
            {
                bankQuery = bankQuery.Where(g => g.SoTien <= bankMaxAmount.Value);
            }

            var bankTransactions = await bankQuery
                .OrderByDescending(g => g.NgayGD)
                .Take(50)
                .ToListAsync();

            ViewBag.Employee = employee;
            ViewBag.SavingsTransactions = savingsTransactions;
            ViewBag.BankTransactions = bankTransactions;
            
            // Truyền giá trị bộ lọc về view
            ViewBag.SavingsCustomer = savingsCustomer;
            ViewBag.SavingsType = savingsType;
            ViewBag.SavingsFromDate = savingsFromDate?.ToString("yyyy-MM-dd");
            ViewBag.SavingsToDate = savingsToDate?.ToString("yyyy-MM-dd");
            ViewBag.SavingsMinAmount = savingsMinAmount;
            ViewBag.SavingsMaxAmount = savingsMaxAmount;
            
            ViewBag.BankCustomer = bankCustomer;
            ViewBag.BankType = bankType;
            ViewBag.BankFromDate = bankFromDate?.ToString("yyyy-MM-dd");
            ViewBag.BankToDate = bankToDate?.ToString("yyyy-MM-dd");
            ViewBag.BankMinAmount = bankMinAmount;
            ViewBag.BankMaxAmount = bankMaxAmount;
            
            return View();
        }
    // Quản lý gói tiết kiệm
        public async Task<IActionResult> TermDepositProducts(string q)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            IQueryable<GoiTietKiem> query = _context.GoiTietKiems.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p => p.TenGoi.Contains(q));
            }

            var products = await query.OrderBy(p => p.KyHanThang).ToListAsync();
            ViewBag.Products = products;
            ViewBag.Query = q;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CreateTermProduct(int? dummy = null)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTermProduct()
        {
            var model = new GoiTietKiem();
            
            // Đọc trực tiếp từ form để tránh lỗi model binding
            model.TenGoi = Request.Form["TenGoi"].ToString();
            model.MoTa = Request.Form["MoTa"].ToString();
            model.HinhThucTraLai = Request.Form["HinhThucTraLai"].ToString();
            // Loại tái tục sẽ được chọn bởi khách hàng khi mở sổ tiết kiệm, không cần đọc từ form tạo gói
            
            // Xử lý các giá trị số
            if (decimal.TryParse(Request.Form["LaiSuat"], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal laiSuat))
            {
                model.LaiSuat = laiSuat;
            }
            
            if (int.TryParse(Request.Form["KyHanThang"], out int kyHanThang))
            {
                model.KyHanThang = kyHanThang;
            }
            
            if (decimal.TryParse(Request.Form["SoTienToiThieu"], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal soTienToiThieu))
            {
                model.SoTienToiThieu = soTienToiThieu;
            }
            
            // Xử lý các giá trị boolean
            model.TaiTucTuDong = Request.Form.ContainsKey("TaiTucTuDong");
            model.ChoPhepRutTruocHan = Request.Form.ContainsKey("ChoPhepRutTruocHan");
            
            model.TrangThai = "Active";

            if (ModelState.IsValid)
            {
                _context.GoiTietKiems.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo gói tiết kiệm mới thành công!";
                return RedirectToAction("TermDepositProducts");
            }

            // Nếu có lỗi, trả lại view với model
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditTermProduct(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            var product = await _context.GoiTietKiems.FindAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTermProduct(int id)
        {
            var product = await _context.GoiTietKiems.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Đọc trực tiếp từ form để tránh lỗi model binding
            product.TenGoi = Request.Form["TenGoi"].ToString();
            product.MoTa = Request.Form["MoTa"].ToString();
            product.HinhThucTraLai = Request.Form["HinhThucTraLai"].ToString();
            
            // Xử lý các giá trị số
            if (decimal.TryParse(Request.Form["LaiSuat"], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal laiSuat))
            {
                product.LaiSuat = laiSuat;
            }
            
            if (int.TryParse(Request.Form["KyHanThang"], out int kyHanThang))
            {
                product.KyHanThang = kyHanThang;
            }
            
            if (decimal.TryParse(Request.Form["SoTienToiThieu"], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal soTienToiThieu))
            {
                product.SoTienToiThieu = soTienToiThieu;
            }
            
            product.TrangThai = Request.Form["TrangThai"].ToString();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật gói tiết kiệm thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GoiTietKiemExists(product.MaGoiTietKiem))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("TermDepositProducts");
            }
            return View(product);
        }

        private bool GoiTietKiemExists(int id)
        {
            return _context.GoiTietKiems.Any(e => e.MaGoiTietKiem == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTermProduct(int? id)
        {
            if (!id.HasValue)
            {
                TempData["ErrorMessage"] = "Mã gói tiết kiệm không hợp lệ.";
                return RedirectToAction("TermDepositProducts");
            }

            var product = await _context.GoiTietKiems.FindAsync(id.Value);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy gói tiết kiệm.";
                return RedirectToAction("TermDepositProducts");
            }

            // Kiểm tra xem có sổ tiết kiệm nào đang sử dụng gói này không
            var hasActiveSavings = await _context.SoTietKiems.AnyAsync(s => s.KyHan == product.KyHanThang);
            if (hasActiveSavings)
            {
                TempData["ErrorMessage"] = "Không thể xóa gói tiết kiệm này vì đang có sổ tiết kiệm sử dụng.";
                return RedirectToAction("TermDepositProducts");
            }

            _context.GoiTietKiems.Remove(product);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa gói tiết kiệm thành công!";
            return RedirectToAction("TermDepositProducts");
        }

        // Hiển thị danh sách lịch hẹn cho quản lý
        [HttpGet]
        public async Task<IActionResult> ManageAppointments()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy danh sách lịch hẹn
            var appointments = await _context.LichHens
                .Include(l => l.KhachHang)
                .Include(l => l.LoaiDichVu)
                .Include(l => l.ChiNhanh)
                .OrderByDescending(l => l.ThoiGianTao)
                .ToListAsync();

            ViewBag.User = user;
            return View(appointments);
        }

        // Duyệt lịch hẹn
        [HttpPost]
        public async Task<IActionResult> ApproveAppointment(string maLichHen)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var appointment = await _context.LichHens.FirstOrDefaultAsync(l => l.MaLichHen == maLichHen);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Lịch hẹn không tồn tại.";
                return RedirectToAction(nameof(ManageAppointments));
            }

            // Cập nhật trạng thái
            appointment.TrangThai = "DaDuyet";
            await _context.SaveChangesAsync();

            // Tạo thông báo cho khách hàng
            var notification = new ThongBao
            {
                MaKH = appointment.MaKH,
                TieuDe = "Lịch hẹn đã được duyệt",
                NoiDung = $"Lịch hẹn của bạn với mã {appointment.MaLichHen} đã được duyệt. Vui lòng đến đúng giờ.",
                NgayGui = DateTime.Now,
                TrangThai = "Chưa đọc"
            };

            _context.ThongBaos.Add(notification);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Duyệt lịch hẹn thành công.";
            return RedirectToAction(nameof(ManageAppointments));
        }

        // Hủy lịch hẹn
        [HttpPost]
        public async Task<IActionResult> CancelAppointment(string maLichHen)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var appointment = await _context.LichHens.FirstOrDefaultAsync(l => l.MaLichHen == maLichHen);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Lịch hẹn không tồn tại.";
                return RedirectToAction(nameof(ManageAppointments));
            }

            // Cập nhật trạng thái
            appointment.TrangThai = "DaHuy";
            await _context.SaveChangesAsync();

            // Tạo thông báo cho khách hàng
            var notification = new ThongBao
            {
                MaKH = appointment.MaKH,
                TieuDe = "Lịch hẹn đã bị hủy",
                NoiDung = $"Lịch hẹn của bạn với mã {appointment.MaLichHen} đã bị hủy.",
                NgayGui = DateTime.Now,
                TrangThai = "Chưa đọc"
            };

            _context.ThongBaos.Add(notification);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Hủy lịch hẹn thành công.";
            return RedirectToAction(nameof(ManageAppointments));
        }
    }
}