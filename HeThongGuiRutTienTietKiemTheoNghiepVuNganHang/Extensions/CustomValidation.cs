using System;
using System.ComponentModel.DataAnnotations;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Extensions
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CustomAgeValidationAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public CustomAgeValidationAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime dateOfBirth)
            {
                var today = DateTime.Today;
                var age = today.Year - dateOfBirth.Year;

                // Check if birthday has occurred this year
                if (dateOfBirth.Date > today.AddYears(-age))
                {
                    age--;
                }

                if (age < _minimumAge)
                {
                    return new ValidationResult(ErrorMessage ?? $"Bạn phải đủ {_minimumAge} tuổi để đăng ký tài khoản");
                }
            }

            return ValidationResult.Success;
        }
    }
}