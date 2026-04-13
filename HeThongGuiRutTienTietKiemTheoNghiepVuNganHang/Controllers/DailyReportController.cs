using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Controllers
{
    [Authorize(Roles = "NhanVienGiaoDich")]
    public class DailyReportController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public DailyReportController(
            UserManager<User> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: Hiển thị form tạo báo cáo
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            ViewBag.ReportDate = DateTime.Today;
            return View();
        }

        // POST: Tạo báo cáo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string reportType, DateTime reportDate, string notes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Kiểm tra dữ liệu đầu vào
            if (reportDate > DateTime.Today)
            {
                TempData["ActionMessage"] = "Ngày báo cáo không thể lớn hơn ngày hiện tại.";
                return RedirectToAction("Create");
            }

            if (string.IsNullOrEmpty(reportType))
            {
                TempData["ActionMessage"] = "Vui lòng chọn loại báo cáo.";
                return RedirectToAction("Create");
            }

            try
            {
                // Tạo báo cáo mới
                var report = new BaoCaoGiaoDichNgay
                {
                    MaNV = user.Id,
                    NgayBaoCao = reportDate,
                    LoaiBaoCao = reportType,
                    NgayLap = DateTime.Now,
                    GhiChu = notes ?? ""
                };

                // Tính toán các chỉ số dựa trên loại báo cáo
                if (reportType == "BaoCaoSoTien")
                {
                    // Báo cáo số tiền thu chi
                    await CalculateFinancialReport(report, reportDate);
                }
                else if (reportType == "BaoCaoSoLuongGD")
                {
                    // Báo cáo số lượng giao dịch
                    await CalculateTransactionCountReport(report, reportDate);
                }

                // Lưu báo cáo
                _context.BaoCaoGiaoDichNgays.Add(report);
                await _context.SaveChangesAsync();

                // Gửi thông báo cho quản lý
                await NotifyManager(report);

                TempData["ActionMessage"] = "Đã tạo báo cáo thành công và gửi cho quản lý.";
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                TempData["ActionMessage"] = $"Có lỗi xảy ra khi tạo báo cáo: {ex.Message}";
                return RedirectToAction("Create");
            }
        }

        // Tính toán báo cáo số tiền thu chi
        private async Task CalculateFinancialReport(BaoCaoGiaoDichNgay report, DateTime reportDate)
        {
            // Tính tổng số tiền thu (gửi tiền)
            var totalDeposit = await _context.GiaoDichTietKiems
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Gửi")
                .SumAsync(g => (decimal?)g.SoTien) ?? 0;

            // Tính tổng số tiền chi (rút tiền)
            var totalWithdraw = await _context.GiaoDichTietKiems
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Rút")
                .SumAsync(g => (decimal?)g.SoTien) ?? 0;

            // Tính tổng số tiền từ giao dịch ngân hàng
            var bankDeposit = await _context.GiaoDichNganHangs
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Nạp tiền")
                .SumAsync(g => (decimal?)g.SoTien) ?? 0;

            var bankWithdraw = await _context.GiaoDichNganHangs
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Rút tiền")
                .SumAsync(g => (decimal?)g.SoTien) ?? 0;

            // Tổng hợp
            report.TongSoTienThu = totalDeposit + bankDeposit;
            report.TongSoTienChi = totalWithdraw + bankWithdraw;
            report.TongTienRong = report.TongSoTienThu - report.TongSoTienChi;

            // Đếm số giao dịch gửi/rút cho báo cáo tài chính
            var savingsDepositCount = await _context.GiaoDichTietKiems
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Gửi")
                .CountAsync();

            var savingsWithdrawCount = await _context.GiaoDichTietKiems
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Rút")
                .CountAsync();

            var bankDepositCount = await _context.GiaoDichNganHangs
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Nạp tiền")
                .CountAsync();

            var bankWithdrawCount = await _context.GiaoDichNganHangs
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Rút tiền")
                .CountAsync();

            report.SoGiaoDichGui = savingsDepositCount + bankDepositCount;
            report.SoGiaoDichRut = savingsWithdrawCount + bankWithdrawCount;

            // Đếm tổng số giao dịch
            report.TongSoGiaoDich = report.SoGiaoDichGui + report.SoGiaoDichRut;
        }

        // Tính toán báo cáo số lượng giao dịch
        private async Task CalculateTransactionCountReport(BaoCaoGiaoDichNgay report, DateTime reportDate)
        {
            // Đếm số giao dịch gửi tiền tiết kiệm
            var savingsDepositCount = await _context.GiaoDichTietKiems
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Gửi")
                .CountAsync();

            // Đếm số giao dịch rút tiền tiết kiệm
            var savingsWithdrawCount = await _context.GiaoDichTietKiems
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Rút")
                .CountAsync();

            // Đếm số giao dịch nạp tiền ngân hàng
            var bankDepositCount = await _context.GiaoDichNganHangs
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Nạp tiền")
                .CountAsync();

            // Đếm số giao dịch rút tiền ngân hàng
            var bankWithdrawCount = await _context.GiaoDichNganHangs
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Rút tiền")
                .CountAsync();

            // Tổng hợp số giao dịch gửi/rút
            report.SoGiaoDichGui = savingsDepositCount + bankDepositCount;
            report.SoGiaoDichRut = savingsWithdrawCount + bankWithdrawCount;

            // Đếm số giao dịch mở sổ tiết kiệm
            report.SoGiaoDichMoSo = await _context.GiaoDichTietKiems
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Gửi" && g.MaSTK.HasValue)
                .CountAsync();

            // Đếm số giao dịch tất toán sổ tiết kiệm (giả định là giao dịch rút tiền có liên kết với sổ tiết kiệm)
            report.SoGiaoDichTatToan = await _context.GiaoDichTietKiems
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Rút" && g.MaSTK.HasValue)
                .CountAsync();

            // Tổng số giao dịch
            report.TongSoGiaoDich = report.SoGiaoDichGui + report.SoGiaoDichRut;

            // Tính tổng tiền thu/chi cho báo cáo số lượng
            var totalDeposit = await _context.GiaoDichTietKiems
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Gửi")
                .SumAsync(g => (decimal?)g.SoTien) ?? 0;

            var totalWithdraw = await _context.GiaoDichTietKiems
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Rút")
                .SumAsync(g => (decimal?)g.SoTien) ?? 0;

            var bankDeposit = await _context.GiaoDichNganHangs
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Nạp tiền")
                .SumAsync(g => (decimal?)g.SoTien) ?? 0;

            var bankWithdraw = await _context.GiaoDichNganHangs
                .Where(g => g.MaNV == report.MaNV && g.NgayGD.Date == reportDate.Date && g.LoaiGD == "Rút tiền")
                .SumAsync(g => (decimal?)g.SoTien) ?? 0;

            report.TongSoTienThu = totalDeposit + bankDeposit;
            report.TongSoTienChi = totalWithdraw + bankWithdraw;
            report.TongTienRong = report.TongSoTienThu - report.TongSoTienChi;
        }

        // Gửi thông báo cho quản lý
        private async Task NotifyManager(BaoCaoGiaoDichNgay report)
        {
            // Lấy danh sách quản lý
            var managers = await _userManager.GetUsersInRoleAsync("NhanVienQuanLy");
            
            foreach (var manager in managers)
            {
                _context.ThongBaos.Add(new ThongBao
                {
                    MaKH = manager.Id,
                    TieuDe = "Báo cáo giao dịch mới",
                    NoiDung = $"Nhân viên {report.NhanVien?.HoTenNV ?? report.NhanVien?.HoTen} đã gửi báo cáo {GetReportTypeName(report.LoaiBaoCao)} ngày {report.NgayBaoCao:dd/MM/yyyy}.",
                    TrangThai = "Chưa đọc",
                    NgayGui = DateTime.Now
                });
            }
            
            await _context.SaveChangesAsync();
        }

        // Lấy tên loại báo cáo
        private string GetReportTypeName(string reportType)
        {
            return reportType switch
            {
                "BaoCaoSoTien" => "số tiền thu chi",
                "BaoCaoSoLuongGD" => "số lượng giao dịch",
                _ => reportType
            };
        }

        // GET: Danh sách báo cáo đã tạo
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var reports = await _context.BaoCaoGiaoDichNgays
                .Where(r => r.MaNV == user.Id)
                .OrderByDescending(r => r.NgayBaoCao)
                .ToListAsync();

            return View(reports);
        }

        // GET: Chi tiết báo cáo
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var report = await _context.BaoCaoGiaoDichNgays
                .Include(r => r.NhanVien)
                .FirstOrDefaultAsync(r => r.MaBaoCao == id && r.MaNV == user.Id);

            if (report == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy báo cáo.";
                return RedirectToAction("Index");
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

        // GET: Xuất báo cáo PDF/Word
        public async Task<IActionResult> Export(int id, string format)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var report = await _context.BaoCaoGiaoDichNgays
                .Include(r => r.NhanVien)
                .FirstOrDefaultAsync(r => r.MaBaoCao == id && r.MaNV == user.Id);

            if (report == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy báo cáo.";
                return RedirectToAction("Index");
            }

            // Trong thực tế, bạn sẽ tích hợp thư viện tạo PDF/Word ở đây
            // Hiện tại chỉ trả về file text mẫu
            var content = GenerateReportContent(report);
            var fileName = $"BaoCao_{report.LoaiBaoCao}_{report.NgayBaoCao:yyyyMMdd}.{format.ToLower()}";
            
            // Trả về file text mẫu (trong thực tế sẽ là file PDF/Word)
            return File(System.Text.Encoding.UTF8.GetBytes(content), "text/plain", fileName);
        }

        // Tạo nội dung báo cáo
        private string GenerateReportContent(BaoCaoGiaoDichNgay report)
        {
            var content = $"BÁO CÁO GIAO DỊCH NGÀY\n";
            content += $"========================\n\n";
            content += $"Mã báo cáo: {report.MaBaoCao}\n";
            content += $"Ngày báo cáo: {report.NgayBaoCao:dd/MM/yyyy}\n";
            content += $"Loại báo cáo: {GetReportTypeName(report.LoaiBaoCao)}\n";
            content += $"Nhân viên lập: {report.NhanVien?.HoTenNV ?? report.NhanVien?.HoTen}\n";
            content += $"Ngày lập: {report.NgayLap:dd/MM/yyyy HH:mm:ss}\n\n";
            
            if (report.LoaiBaoCao == "BaoCaoSoTien")
            {
                content += $"TỔNG QUAN TÀI CHÍNH\n";
                content += $"------------------\n";
                content += $"Tổng số tiền thu: {report.TongSoTienThu:#,##0} đ\n";
                content += $"Tổng số tiền chi: {report.TongSoTienChi:#,##0} đ\n";
                content += $"Tổng tiền ròng: {report.TongTienRong:#,##0} đ\n";
                content += $"Tổng số giao dịch: {report.TongSoGiaoDich}\n";
            }
            else if (report.LoaiBaoCao == "BaoCaoSoLuongGD")
            {
                content += $"CHI TIẾT GIAO DỊCH\n";
                content += $"------------------\n";
                content += $"Số giao dịch gửi tiền: {report.SoGiaoDichGui}\n";
                content += $"Số giao dịch rút tiền: {report.SoGiaoDichRut}\n";
                content += $"Số giao dịch mở sổ: {report.SoGiaoDichMoSo}\n";
                content += $"Số giao dịch tất toán: {report.SoGiaoDichTatToan}\n";
                content += $"Tổng số giao dịch: {report.TongSoGiaoDich}\n\n";
                
                content += $"TÀI CHÍNH\n";
                content += $"---------\n";
                content += $"Tổng số tiền thu: {report.TongSoTienThu:#,##0} đ\n";
                content += $"Tổng số tiền chi: {report.TongSoTienChi:#,##0} đ\n";
                content += $"Tổng tiền ròng: {report.TongTienRong:#,##0} đ\n";
            }
            
            content += $"\nGhi chú: {report.GhiChu}\n";
            
            return content;
        }
    }
}