using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    [Table("BaoCaoGiaoDichNgay")]
    public class BaoCaoGiaoDichNgay
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaBaoCao { get; set; }

        public string MaNV { get; set; } // Nhân viên lập báo cáo

        public DateTime NgayBaoCao { get; set; }

        [StringLength(50)]
        public string LoaiBaoCao { get; set; } // "BaoCaoSoTien" / "BaoCaoSoLuongGD"

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongSoTienThu { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongSoTienChi { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTienRong { get; set; }

        public int TongSoGiaoDich { get; set; }

        public int SoGiaoDichGui { get; set; }

        public int SoGiaoDichRut { get; set; }

        public int SoGiaoDichMoSo { get; set; }

        public int SoGiaoDichTatToan { get; set; }

        public DateTime NgayLap { get; set; }

        [StringLength(500)]
        public string GhiChu { get; set; }

        // Navigation properties
        [ForeignKey("MaNV")]
        public virtual User NhanVien { get; set; }
    }
}