using System.ComponentModel.DataAnnotations;

namespace InternalRequest_Management.Models
{
    public class TaiKhoanVm
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [MaxLength(255)]
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(255)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        // Chỉ để hiển thị, không cho sửa
        public string? TenDangNhap { get; set; }
        public string? VaiTro { get; set; }
        public string? PhongBan { get; set; }
        public string? ChucDanh { get; set; }
        public string? QuanLy { get; set; }
        public string? TrangThai { get; set; }
    }
}