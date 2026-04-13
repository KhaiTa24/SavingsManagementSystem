using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    public class GoiTietKiem
    {
        [Key]
        public int MaGoiTietKiem { get; set; }

        [Required, MaxLength(100)]
        public string TenGoi { get; set; }

        public string MoTa { get; set; }

        [Range(1, 120, ErrorMessage = "Kỳ hạn phải từ 1-120 tháng")]
        public int KyHanThang { get; set; }

        [Range(0.01, 100, ErrorMessage = "Lãi suất phải từ 0.01% đến 100%")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal LaiSuat { get; set; }

        [Range(typeof(decimal), "100000", "9999999999999999", ErrorMessage = "Số tiền tối thiểu phải từ 100,000 VNĐ")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienToiThieu { get; set; }

        [Required]
        [MaxLength(50)]
        public string HinhThucTraLai { get; set; }

        public bool TaiTucTuDong { get; set; }

        // Loại tái tục sẽ được chọn bởi khách hàng khi mở sổ tiết kiệm, không cần lưu trong gói
        // [Required]
        // [MaxLength(50)]
        // public string LoaiTaiTuc { get; set; }

        public bool ChoPhepRutTruocHan { get; set; }

        [MaxLength(20)]
        public string TrangThai { get; set; } // Active / Inactive
    }
}