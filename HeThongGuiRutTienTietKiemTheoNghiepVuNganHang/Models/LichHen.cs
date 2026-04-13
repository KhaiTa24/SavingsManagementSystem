using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    public class LichHen
    {
        [Key]
        public string MaLichHen { get; set; }

        [Required]
        public string MaKH { get; set; }

        [Required]
        public string MaDV { get; set; }

        [Required]
        public string MaCN { get; set; }

        [Required]
        public DateTime NgayGiaoDich { get; set; }

        [Required]
        public string KhungGio { get; set; } // Ví dụ: "08:00-09:00"

        public DateTime ThoiGianTao { get; set; } = DateTime.Now;

        public string TrangThai { get; set; } = "ChoDuyet"; // ChoDuyet, DaDuyet, DaHuy

        public string? GhiChuKH { get; set; }

        // Navigation properties
        [ForeignKey("MaKH")]
        public virtual User KhachHang { get; set; }

        [ForeignKey("MaDV")]
        public virtual LoaiDichVu LoaiDichVu { get; set; }

        [ForeignKey("MaCN")]
        public virtual ChiNhanh ChiNhanh { get; set; }
    }
}