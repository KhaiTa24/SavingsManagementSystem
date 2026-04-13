using System.ComponentModel.DataAnnotations;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    public class ChiNhanh
    {
        [Key]
        public string MaCN { get; set; }

        [Required]
        public string TenChiNhanh { get; set; }

        [Required]
        public string DiaChi { get; set; }

        public string SoDienThoai { get; set; }

        public string GioLamViec { get; set; }

        public string TrangThaiHD { get; set; }
    }
}