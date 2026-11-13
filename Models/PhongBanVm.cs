using System.ComponentModel.DataAnnotations;

namespace InternalRequest_Management.Models
{
    public class PhongBanVm
    {
        public int MaPhongBan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phòng ban")]
        [MaxLength(255)]
        [Display(Name = "Tên phòng ban")]
        public string TenPhongBan { get; set; } = string.Empty;

        [Display(Name = "Phòng ban cấp trên")]
        public int? MaPhongBanCha { get; set; }
    }

    public class PhongBanRow
    {
        public PhongBan PhongBan { get; set; } = null!;
        public int Cap { get; set; }
        public string? TenCha { get; set; }
        public int SoNguoi { get; set; }
        public int SoNguoiHoatDong { get; set; }
        public int SoCon { get; set; }
        public int SoConHoatDong { get; set; }
    }

    public class ChucDanhVm
    {
        public int MaChucDanh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên chức danh")]
        [MaxLength(255)]
        [Display(Name = "Tên chức danh")]
        public string TenChucDanh { get; set; } = string.Empty;
    }
}