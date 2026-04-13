using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    [Table("GiaoDichNganHang")]
    public class GiaoDichNganHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaGD { get; set; }

        [StringLength(12)]
        public string SoTaiKhoan { get; set; }

        [StringLength(20)]
        public string LoaiGD { get; set; } // Nạp tiền / Rút tiền

        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTien { get; set; }

        public DateTime NgayGD { get; set; } = DateTime.Now;

        public string MaNV { get; set; } // NV thực hiện giao dịch tại quầy

        [StringLength(50)]
        public string TrangThaiGD { get; set; } // Thành công / Thất bại

        // Navigation properties
        [ForeignKey("SoTaiKhoan")]
        public virtual TaiKhoanNganHang TaiKhoanNganHang { get; set; }

        [ForeignKey("MaNV")]
        public virtual User NhanVien { get; set; }
    }
}