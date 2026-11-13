using System.ComponentModel.DataAnnotations;

namespace InternalRequest_Management.Models
{
    public class NguoiDungVm
    {
        public int MaNguoiDung { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [MaxLength(100)]
        [RegularExpression(@"^[A-Za-z0-9_.]+$", ErrorMessage = "Chỉ gồm chữ không dấu, số, dấu chấm hoặc gạch dưới")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [MaxLength(255)]
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(255)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string? MatKhau { get; set; }

        [Display(Name = "Phòng ban")]
        public int? MaPhongBan { get; set; }

        [Display(Name = "Chức danh")]
        public int? MaChucDanh { get; set; }

        [Display(Name = "Người quản lý trực tiếp")]
        public int? MaNguoiQuanLy { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool TrangThai { get; set; } = true;

        public List<int> VaiTroIds { get; set; } = new List<int>();
    }
}