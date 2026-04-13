using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Services
{
    public interface ILoginTrackingService
    {
        Task TrackSuccessfulLoginAsync(User user);
        Task TrackFailedLoginAsync(string email, string ipAddress);
        Task TrackLogoutAsync(string userId);
    }

    public class LoginTrackingService : ILoginTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginTrackingService(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task TrackSuccessfulLoginAsync(User user)
        {
            try
            {
                var ipAddress = GetClientIpAddress();
                
                // Lấy vai trò chính xác của người dùng
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault() ?? user.Role ?? "Unknown";
                
                // Kiểm tra xem có session nào chưa kết thúc cho user này không
                var existingSession = await _context.LichSuDangNhaps
                    .Where(l => l.MaDN == user.Id && l.TrangThai == "DangHoatDong" && l.TGDangXuat == null)
                    .OrderByDescending(l => l.TGDangNhap)
                    .FirstOrDefaultAsync();

                if (existingSession != null)
                {
                    // Cập nhật session hiện tại thành đã đăng xuất
                    existingSession.TrangThai = "DangXuat";
                    existingSession.TGDangXuat = DateTime.Now;
                    _context.LichSuDangNhaps.Update(existingSession);
                }

                // Tạo bản ghi đăng nhập mới
                var loginRecord = new LichSuDangNhap
                {
                    MaDN = user.Id,
                    LoaiNguoiDung = userRole,
                    TGDangNhap = DateTime.Now,
                    DiaChiIP = ipAddress,
                    TrangThai = "DangHoatDong",
                    SoLanDangNhapThatBai = 0
                };

                _context.LichSuDangNhaps.Add(loginRecord);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log exception - trong môi trường thực tế nên sử dụng logging framework
                Console.WriteLine($"Error tracking successful login: {ex.Message}");
            }
        }

        public async Task TrackFailedLoginAsync(string email, string ipAddress)
        {
            try
            {
                // Lấy thông tin user từ email
                var user = await _userManager.FindByEmailAsync(email);
                
                if (user != null)
                {
                    // Lấy số lần đăng nhập thất bại từ Identity
                    var accessFailedCount = await _userManager.GetAccessFailedCountAsync(user);
                    
                    // Lấy vai trò của người dùng
                    var roles = await _userManager.GetRolesAsync(user);
                    var userRole = roles.FirstOrDefault() ?? user.Role ?? "Unknown";
                    
                    // Tạo bản ghi đăng nhập thất bại
                    var loginRecord = new LichSuDangNhap
                    {
                        MaDN = user.Id,
                        LoaiNguoiDung = userRole,
                        TGDangNhap = DateTime.Now,
                        DiaChiIP = ipAddress ?? GetClientIpAddress(),
                        TrangThai = "ThatBai",
                        SoLanDangNhapThatBai = accessFailedCount
                    };

                    _context.LichSuDangNhaps.Add(loginRecord);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Trường hợp email không tồn tại trong hệ thống
                    var loginRecord = new LichSuDangNhap
                    {
                        MaDN = "UNKNOWN",
                        LoaiNguoiDung = "Unknown",
                        TGDangNhap = DateTime.Now,
                        DiaChiIP = ipAddress ?? GetClientIpAddress(),
                        TrangThai = "ThatBai",
                        SoLanDangNhapThatBai = 1
                    };

                    _context.LichSuDangNhaps.Add(loginRecord);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log exception - trong môi trường thực tế nên sử dụng logging framework
                Console.WriteLine($"Error tracking failed login: {ex.Message}");
            }
        }

        public async Task TrackLogoutAsync(string userId)
        {
            try
            {
                // Tìm session đang hoạt động gần nhất của user
                var activeSession = await _context.LichSuDangNhaps
                    .Where(l => l.MaDN == userId && l.TrangThai == "DangHoatDong" && l.TGDangXuat == null)
                    .OrderByDescending(l => l.TGDangNhap)
                    .FirstOrDefaultAsync();

                if (activeSession != null)
                {
                    // Cập nhật trạng thái thành đã đăng xuất
                    activeSession.TrangThai = "DangXuat";
                    activeSession.TGDangXuat = DateTime.Now;
                    _context.LichSuDangNhaps.Update(activeSession);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log exception - trong môi trường thực tế nên sử dụng logging framework
                Console.WriteLine($"Error tracking logout: {ex.Message}");
            }
        }

        private string GetClientIpAddress()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.Connection?.RemoteIpAddress != null)
                {
                    return httpContext.Connection.RemoteIpAddress.ToString();
                }

                if (httpContext?.Request?.Headers?.ContainsKey("X-Forwarded-For") == true)
                {
                    return httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').First().Trim() ??
                           IPAddress.Loopback.ToString();
                }

                return IPAddress.Loopback.ToString();
            }
            catch
            {
                return IPAddress.Loopback.ToString();
            }
        }
    }
}