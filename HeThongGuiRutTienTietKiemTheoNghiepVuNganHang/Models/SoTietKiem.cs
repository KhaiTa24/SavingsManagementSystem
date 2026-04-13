using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    [Table("SoTietKiem")]
    public class SoTietKiem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaSTK { get; set; }

        [StringLength(12)]
        public string SoTaiKhoan { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienGui { get; set; }

        public int KyHan { get; set; } // số tháng

        public double LaiSuat { get; set; }

        public DateTime NgayMoSo { get; set; } = DateTime.Now;

        public DateTime NgayDaoHan { get; set; }

        [StringLength(50)]
        public string TrangThai { get; set; } = "Đang hoạt động";

        // Thêm trường loại tái tục được chọn bởi khách hàng
        [StringLength(50)]
        public string LoaiTaiTuc { get; set; }

        // Navigation properties
        [ForeignKey("SoTaiKhoan")]
        public virtual TaiKhoanNganHang TaiKhoanNganHang { get; set; }

        public virtual ICollection<GiaoDichTietKiem> GiaoDichTietKiems { get; set; } = new List<GiaoDichTietKiem>();
    }
}