using System;
using System.ComponentModel.DataAnnotations;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    public class OTPVerification
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public string Email { get; set; }
        
        [Required]
        public string OTPCode { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime ExpiryTime { get; set; }
        
        public bool IsUsed { get; set; }
        
        [Required]
        public string Purpose { get; set; } // "OpenSavingsAccount" hoặc các mục đích khác
    }
}