using System.ComponentModel.DataAnnotations;

namespace InternalRequest_Management.Models
{
    public class LoaiDeNghiVm
    {
        public int MaLoai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên loại đề nghị")]
        [MaxLength(255)]
        [Display(Name = "Tên loại đề nghị")]
        public string TenLoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã kỹ thuật")]
        [MaxLength(50)]
        [Display(Name = "Mã kỹ thuật (ví dụ NGHI_PHEP)")]
        public string MaKyThuat { get; set; } = string.Empty;

        [Display(Name = "Đang sử dụng")]
        public bool TrangThaiSuDung { get; set; } = true;
    }

    public class BuocVm
    {
        [Required(ErrorMessage = "Vui lòng nhập tên bước")]
        [MaxLength(255)]
        public string TenBuoc { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn người xử lý")]
        public int MaNguoiDung { get; set; }

        [Range(1, 8760, ErrorMessage = "Hạn xử lý phải từ 1 giờ trở lên")]
        public int ThoiHanXuLyGio { get; set; } = 24;
    }

    public class QuyTrinhVm
    {
        public int MaQuyTrinh { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn loại đề nghị")]
        [Display(Name = "Loại đề nghị")]
        public int MaLoai { get; set; }

        public string? TenLoai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên quy trình")]
        [MaxLength(255)]
        [Display(Name = "Tên quy trình")]
        public string TenQuyTrinh { get; set; } = string.Empty;

        public int PhienBan { get; set; } = 1;

        public List<BuocVm> Buocs { get; set; } = new List<BuocVm>();
    }
}