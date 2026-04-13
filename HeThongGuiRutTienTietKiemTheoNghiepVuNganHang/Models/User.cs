using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    public class User : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string Role { get; set; } // "KhachHang", "NhanVien", or "Admin"
        
        // Properties for KhachHang
        public int? MaKH { get; set; }
        public string? HoTen { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? CCCD { get; set; }
        public string? SDT { get; set; }
        public string? DiaChi { get; set; }
        public string? NgheNghiep { get; set; }
        public string? DigitalPin { get; set; } // Digital PIN for transaction authentication
        public bool IsPinSetup { get; set; } = false; // Flag to indicate if PIN has been set up
        
        // Properties for NhanVien
        public int? MaNV { get; set; }
        public string? HoTenNV { get; set; }
        public string? ViTri { get; set; }
        
        // Navigation properties
        public virtual ICollection<TaiKhoanNganHang> TaiKhoanNganHangs { get; set; } = new List<TaiKhoanNganHang>();
        public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();
        public virtual ICollection<GiaoDichTietKiem> GiaoDichTietKiems { get; set; } = new List<GiaoDichTietKiem>();
        public virtual ICollection<GiaoDichNganHang> GiaoDichNganHangs { get; set; } = new List<GiaoDichNganHang>();
    }
}