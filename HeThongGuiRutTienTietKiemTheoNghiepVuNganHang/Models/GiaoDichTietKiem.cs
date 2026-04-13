using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    [Table("GiaoDichTietKiem")]
    public class GiaoDichTietKiem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaGD { get; set; }

        public int? MaSTK { get; set; } // Cho phép null để sử dụng cho giao dịch tài khoản ngân hàng

        public string? MaNV { get; set; }

        [StringLength(20)]
        public string LoaiGD { get; set; } // Gửi / Rút / Nạp tiền

        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTien { get; set; }

        public DateTime NgayGD { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string TrangThaiGD { get; set; }

        // Navigation properties
        [ForeignKey("MaSTK")]
        public virtual SoTietKiem SoTietKiem { get; set; }

        [ForeignKey("MaNV")]
        public virtual User NhanVien { get; set; }
    }
}