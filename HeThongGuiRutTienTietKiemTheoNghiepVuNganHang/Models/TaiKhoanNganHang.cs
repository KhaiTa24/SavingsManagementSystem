using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    [Table("TaiKhoanNganHang")]
    public class TaiKhoanNganHang
    {
        [Key]
        [StringLength(12)]
        public string SoTaiKhoan { get; set; }

        public string MaKH { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SoDu { get; set; } = 0;

        [StringLength(50)]
        public string TrangThai { get; set; } = "Hoạt động";

        public DateTime NgayMoTaiKhoan { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("MaKH")]
        public virtual User KhachHang { get; set; }

        public virtual ICollection<SoTietKiem> SoTietKiems { get; set; } = new List<SoTietKiem>();
        public virtual ICollection<GiaoDichNganHang> GiaoDichNganHangs { get; set; } = new List<GiaoDichNganHang>();
    }
}