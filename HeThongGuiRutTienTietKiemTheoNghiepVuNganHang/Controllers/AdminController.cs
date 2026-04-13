using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models;
using System.Linq;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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
            var totalUsers = await _userManager.Users.CountAsync();
            var totalTransactions = await _context.GiaoDichTietKiems.CountAsync();
            var totalSavingsBooks = await _context.SoTietKiems.CountAsync();
            var totalBankAccounts = await _context.TaiKhoanNganHangs.CountAsync();

            ViewBag.User = user;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalTransactions = totalTransactions;
            ViewBag.TotalSavingsBooks = totalSavingsBooks;
            ViewBag.TotalBankAccounts = totalBankAccounts;

            if (TempData["LoginSuccess"] != null)
            {
                ViewBag.LoginSuccess = TempData["LoginSuccess"];
            }

            return View();
        }

        // Quản lý người dùng
        public async Task<IActionResult> UserManagement(string q, string role = null, string email = null, string username = null)
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            IQueryable<User> query = _userManager.Users;
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(u => u.HoTen.Contains(q) || u.Email.Contains(q) || u.UserName.Contains(q));
            }
            
            // Bộ lọc nâng cao
            if (!string.IsNullOrWhiteSpace(role))
            {
                // Lấy danh sách người dùng theo vai trò
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                var userIdsInRole = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }
            
            if (!string.IsNullOrWhiteSpace(email))
            {
                email = email.Trim();
                query = query.Where(u => u.Email.Contains(email));
            }
            
            if (!string.IsNullOrWhiteSpace(username))
            {
                username = username.Trim();
                query = query.Where(u => u.UserName.Contains(username));
            }

            var users = await query.OrderBy(u => u.HoTen).Take(200).ToListAsync();
            
            // Get roles for each user
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var u in users)
            {
                userRoles[u.Id] = await _userManager.GetRolesAsync(u);
            }

            ViewBag.Users = users;
            ViewBag.UserRoles = userRoles;
            ViewBag.Query = q;
            ViewBag.Role = role;
            ViewBag.Email = email;
            ViewBag.Username = username;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("UserManagement");
            }

            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            _logger.LogWarning($"Admin {User.Identity?.Name} locked user {user.Email}");
            TempData["ActionMessage"] = $"Đã khóa tài khoản {user.Email}.";
            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("UserManagement");
            }

            await _userManager.SetLockoutEndDateAsync(user, null);
            _logger.LogInformation($"Admin {User.Identity?.Name} unlocked user {user.Email}");
            TempData["ActionMessage"] = $"Đã mở khóa tài khoản {user.Email}.";
            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("UserManagement");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                _logger.LogWarning($"Admin {User.Identity?.Name} reset password for {user.Email}");
                TempData["ActionMessage"] = $"Đã đặt lại mật khẩu cho {user.Email}.";
            }
            else
            {
                TempData["ActionMessage"] = "Đặt lại mật khẩu thất bại.";
            }

            return RedirectToAction("UserManagement");
        }

        // Quản lý vai trò
        public async Task<IActionResult> RoleManagement()
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = roles;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ActionMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("UserManagement");
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["ActionMessage"] = "Vai trò không tồn tại.";
                return RedirectToAction("UserManagement");
            }

            // Remove old roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Add new role
            await _userManager.AddToRoleAsync(user, roleName);
            user.Role = roleName;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation($"Admin {User.Identity?.Name} assigned role {roleName} to {user.Email}");
            TempData["ActionMessage"] = $"Đã gán vai trò {roleName} cho {user.Email}.";
            return RedirectToAction("UserManagement");
        }

        // Gửi thông báo hệ thống
        public async Task<IActionResult> SystemNotifications()
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendSystemNotification(string targetRole, string title, string content)
        {
            var users = new List<User>();

            if (targetRole == "All")
            {
                users = await _userManager.Users.ToListAsync();
            }
            else
            {
                users = (await _userManager.GetUsersInRoleAsync(targetRole)).ToList();
            }

            foreach (var user in users)
            {
                if (user.Role == "KhachHang")
                {
                    _context.ThongBaos.Add(new ThongBao
                    {
                        MaKH = user.Id,
                        TieuDe = title,
                        NoiDung = content,
                        TrangThai = "Chưa đọc",
                        NgayGui = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Admin {User.Identity?.Name} sent notification to {targetRole}: {title}");
            TempData["ActionMessage"] = $"Đã gửi thông báo đến {users.Count} người dùng.";
            return RedirectToAction("SystemNotifications");
        }

        // Lịch sử đăng nhập
        public async Task<IActionResult> LoginHistory(string ipFilter = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            // Query lịch sử đăng nhập
            IQueryable<LichSuDangNhap> query = _context.LichSuDangNhaps
                .Include(l => l.NguoiDung)
                .OrderByDescending(l => l.TGDangNhap);

            // Áp dụng bộ lọc IP nếu có
            if (!string.IsNullOrWhiteSpace(ipFilter))
            {
                query = query.Where(l => l.DiaChiIP.Contains(ipFilter));
            }

            // Áp dụng bộ lọc ngày nếu có
            if (fromDate.HasValue)
            {
                query = query.Where(l => l.TGDangNhap >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                // Đặt thời gian kết thúc là cuối ngày
                var endDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.TGDangNhap <= endDate);
            }

            var loginHistories = await query.Take(200).ToListAsync();

            ViewBag.IPFilter = ipFilter;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.LoginHistories = loginHistories;

            return View();
        }

        // Quản lý session người dùng
        public async Task<IActionResult> ActiveSessions()
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            // Lấy các session đang hoạt động (chưa đăng xuất)
            var activeSessions = await _context.LichSuDangNhaps
                .Include(l => l.NguoiDung)
                .Where(l => l.TrangThai == "DangHoatDong" || l.TGDangXuat == null)
                .OrderByDescending(l => l.TGDangNhap)
                .ToListAsync();

            ViewBag.ActiveSessions = activeSessions;
            return View();
        }

        // Cảnh báo đăng nhập bất thường
        public async Task<IActionResult> SuspiciousLogins()
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            // Lấy các đăng nhập có nhiều lần thất bại
            var suspiciousLogins = await _context.LichSuDangNhaps
                .Include(l => l.NguoiDung)
                .Where(l => l.SoLanDangNhapThatBai > 3)
                .OrderByDescending(l => l.TGDangNhap)
                .ToListAsync();

            ViewBag.SuspiciousLogins = suspiciousLogins;
            return View();
        }

        // Kết thúc session người dùng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TerminateSession(int sessionId)
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var session = await _context.LichSuDangNhaps
                .FirstOrDefaultAsync(l => l.MaLichSu == sessionId);

            if (session == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy session.";
                return RedirectToAction("ActiveSessions");
            }

            // Cập nhật trạng thái session thành đã đăng xuất
            session.TrangThai = "DangXuat";
            session.TGDangXuat = DateTime.Now;

            _context.LichSuDangNhaps.Update(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Admin {admin.UserName} terminated session {sessionId} for user {session.MaDN}");
            TempData["SuccessMessage"] = "Đã kết thúc session thành công.";
            return RedirectToAction("ActiveSessions");
        }

        // Hiển thị danh sách chi nhánh
        [HttpGet]
        public async Task<IActionResult> ManageBranches()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var branches = await _context.ChiNhanhs.ToListAsync();
            ViewBag.User = user;
            return View(branches);
        }

        // Hiển thị form tạo chi nhánh mới
        [HttpGet]
        public async Task<IActionResult> CreateBranch()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            ViewBag.User = user;
            return View();
        }

        // Xử lý tạo chi nhánh mới
        [HttpPost]
        public async Task<IActionResult> CreateBranch(string tenChiNhanh, string diaChi, string soDienThoai, string gioLamViec, string trangThaiHD)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(tenChiNhanh) || string.IsNullOrEmpty(diaChi))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin bắt buộc.";
                ViewBag.User = user;
                return View();
            }

            // Tạo đối tượng chi nhánh mới
            var branch = new ChiNhanh
            {
                MaCN = "CN" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(100, 999),
                TenChiNhanh = tenChiNhanh,
                DiaChi = diaChi,
                SoDienThoai = soDienThoai,
                GioLamViec = gioLamViec,
                TrangThaiHD = string.IsNullOrEmpty(trangThaiHD) ? "HoatDong" : trangThaiHD
            };

            _context.ChiNhanhs.Add(branch);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm chi nhánh thành công.";
            return RedirectToAction(nameof(ManageBranches));
        }

        // Hiển thị form chỉnh sửa chi nhánh
        [HttpGet]
        public async Task<IActionResult> EditBranch(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var branch = await _context.ChiNhanhs.FindAsync(id);
            if (branch == null)
            {
                return NotFound();
            }

            ViewBag.User = user;
            return View(branch);
        }

        // Xử lý chỉnh sửa chi nhánh
        [HttpPost]
        public async Task<IActionResult> EditBranch(ChiNhanh branch)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            if (ModelState.IsValid)
            {
                var existingBranch = await _context.ChiNhanhs.FindAsync(branch.MaCN);
                if (existingBranch == null)
                {
                    return NotFound();
                }

                existingBranch.TenChiNhanh = branch.TenChiNhanh;
                existingBranch.DiaChi = branch.DiaChi;
                existingBranch.SoDienThoai = branch.SoDienThoai;
                existingBranch.GioLamViec = branch.GioLamViec;
                existingBranch.TrangThaiHD = branch.TrangThaiHD;

                _context.ChiNhanhs.Update(existingBranch);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật chi nhánh thành công.";
                return RedirectToAction(nameof(ManageBranches));
            }

            ViewBag.User = user;
            return View(branch);
        }

        // Xóa chi nhánh
        [HttpPost]
        public async Task<IActionResult> DeleteBranch(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var branch = await _context.ChiNhanhs.FindAsync(id);
            if (branch == null)
            {
                TempData["ErrorMessage"] = "Chi nhánh không tồn tại.";
                return RedirectToAction(nameof(ManageBranches));
            }

            _context.ChiNhanhs.Remove(branch);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa chi nhánh thành công.";
            return RedirectToAction(nameof(ManageBranches));
        }

        // Hiển thị danh sách loại dịch vụ
        [HttpGet]
        public async Task<IActionResult> ManageServices()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var services = await _context.LoaiDichVus.ToListAsync();
            ViewBag.User = user;
            return View(services);
        }

        // Hiển thị form tạo loại dịch vụ mới
        [HttpGet]
        public async Task<IActionResult> CreateService()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            ViewBag.User = user;
            return View();
        }

        // Xử lý tạo loại dịch vụ mới
        [HttpPost]
        public async Task<IActionResult> CreateService(string tenDV, int thoiGianUocTinh, string moTa, bool choPhepDatLich)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(tenDV))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin bắt buộc.";
                ViewBag.User = user;
                return View();
            }

            // Tạo đối tượng loại dịch vụ mới
            var service = new LoaiDichVu
            {
                MaDV = "DV" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(100, 999),
                TenDV = tenDV,
                ThoiGianUocTinh = thoiGianUocTinh,
                MoTa = moTa,
                ChoPhepDatLich = choPhepDatLich
            };

            _context.LoaiDichVus.Add(service);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm loại dịch vụ thành công.";
            return RedirectToAction(nameof(ManageServices));
        }

        // Hiển thị form chỉnh sửa loại dịch vụ
        [HttpGet]
        public async Task<IActionResult> EditService(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var service = await _context.LoaiDichVus.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            ViewBag.User = user;
            return View(service);
        }

        // Xử lý chỉnh sửa loại dịch vụ
        [HttpPost]
        public async Task<IActionResult> EditService(LoaiDichVu service)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            if (ModelState.IsValid)
            {
                var existingService = await _context.LoaiDichVus.FindAsync(service.MaDV);
                if (existingService == null)
                {
                    return NotFound();
                }

                existingService.TenDV = service.TenDV;
                existingService.ThoiGianUocTinh = service.ThoiGianUocTinh;
                existingService.MoTa = service.MoTa;
                existingService.ChoPhepDatLich = service.ChoPhepDatLich;

                _context.LoaiDichVus.Update(existingService);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật loại dịch vụ thành công.";
                return RedirectToAction(nameof(ManageServices));
            }

            ViewBag.User = user;
            return View(service);
        }

        // Xóa loại dịch vụ
        [HttpPost]
        public async Task<IActionResult> DeleteService(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var service = await _context.LoaiDichVus.FindAsync(id);
            if (service == null)
            {
                TempData["ErrorMessage"] = "Loại dịch vụ không tồn tại.";
                return RedirectToAction(nameof(ManageServices));
            }

            _context.LoaiDichVus.Remove(service);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa loại dịch vụ thành công.";
            return RedirectToAction(nameof(ManageServices));
        }

        // Hiển thị danh sách khung giờ
        [HttpGet]
        public async Task<IActionResult> ManageTimeSlots()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy danh sách chi nhánh thay vì khung giờ
            var branches = await _context.ChiNhanhs.ToListAsync();
            ViewBag.User = user;
            return View(branches); // Trả về danh sách chi nhánh thay vì khung giờ
        }

        // Hiển thị form tạo chi nhánh mới
        [HttpGet]
        public async Task<IActionResult> CreateTimeSlot()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            ViewBag.User = user;
            return RedirectToAction("CreateBranch", "Admin"); // Chuyển hướng đến tạo chi nhánh
        }

        // Xử lý tạo chi nhánh mới
        [HttpPost]
        public async Task<IActionResult> CreateTimeSlot(string thoiGianBatDau, string thoiGianKetThuc, bool trangThai, string ghiChu)
        {
            // Chuyển hướng đến action tạo chi nhánh
            return RedirectToAction("CreateBranch", "Admin");
        }

        // Hiển thị form chỉnh sửa chi nhánh
        [HttpGet]
        public async Task<IActionResult> EditTimeSlot(string id)
        {
            // Chuyển hướng đến action chỉnh sửa chi nhánh
            return RedirectToAction("EditBranch", "Admin", new { id = id });
        }

        // Xử lý chỉnh sửa chi nhánh
        [HttpPost]
        public async Task<IActionResult> EditTimeSlot(string maKG, string thoiGianBatDau, string thoiGianKetThuc, bool trangThai, string ghiChu)
        {
            // Chuyển hướng đến action chỉnh sửa chi nhánh
            return RedirectToAction("EditBranch", "Admin", new { id = maKG });
        }

        // Xóa chi nhánh
        [HttpPost]
        public async Task<IActionResult> DeleteTimeSlot(string id)
        {
            // Chuyển hướng đến action xóa chi nhánh
            return RedirectToAction("DeleteBranch", "Admin", new { id = id });
        }
    }
}
