using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    [Table("ThongBao")]
    public class ThongBao
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaThongBao { get; set; }

        public string MaKH { get; set; }

        [StringLength(100)]
        public string TieuDe { get; set; }

        [StringLength(255)]
        public string NoiDung { get; set; }

        public DateTime NgayGui { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string TrangThai { get; set; } = "Chưa đọc";

        // Navigation properties
        [ForeignKey("MaKH")]
        public virtual User KhachHang { get; set; }
    }
}