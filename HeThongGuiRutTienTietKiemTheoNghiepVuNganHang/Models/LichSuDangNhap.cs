using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    public class LichSuDangNhap
    {
        [Key]
        public int MaLichSu { get; set; }

        [Required]
        [StringLength(450)]
        public string MaDN { get; set; } // ID người dùng từ AspNetUsers

        [Required]
        [StringLength(50)]
        public string LoaiNguoiDung { get; set; } // Vai trò: Admin, KhachHang, NhanVienGiaoDich, NhanVienQuanLy

        [Required]
        public DateTime TGDangNhap { get; set; }

        public DateTime? TGDangXuat { get; set; }

        [Required]
        public int SoLanDangNhapThatBai { get; set; } = 0;

        [Required]
        [StringLength(45)]
        public string DiaChiIP { get; set; }

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } // DangNhap, DangXuat, ThatBai, DangHoatDong

        // Navigation property
        [ForeignKey("MaDN")]
        public virtual User NguoiDung { get; set; }
    }
}