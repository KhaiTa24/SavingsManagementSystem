using System.ComponentModel.DataAnnotations;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    public class LoaiDichVu
    {
        [Key]
        public string MaDV { get; set; }

        [Required]
        public string TenDV { get; set; }

        public int ThoiGianUocTinh { get; set; } // Tính bằng phút

        public string MoTa { get; set; }

        public bool ChoPhepDatLich { get; set; } = true;
    }
}