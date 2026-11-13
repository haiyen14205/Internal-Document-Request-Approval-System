using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace InternalRequest_Management.Models
{
    public class TepInfo
    {
        public int MaTep { get; set; }
        public string TenTep { get; set; } = string.Empty;
        public int? KichThuoc { get; set; }
    }

    public class HoSoVm
    {
        public int MaHoSo { get; set; }

        [Display(Name = "Loại đề nghị")]
        public int MaLoai { get; set; }

        [Display(Name = "Nội dung / lý do")]
        public string? NoiDung { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Từ ngày")]
        public DateTime? NgayBatDau { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Đến ngày")]
        public DateTime? NgayKetThuc { get; set; }

        [Display(Name = "Địa điểm")]
        public string? DiaDiem { get; set; }

        [Display(Name = "Kinh phí dự kiến (VNĐ)")]
        public double? KinhPhi { get; set; }

        public List<IFormFile>? TepMoi { get; set; }
        public List<int>? TepXoa { get; set; }
        public List<TepInfo>? TepHienCo { get; set; }
    }

    public static class TrangThaiUi
    {
        // Màu cũ (dùng cho trang chủ và các trang khác)
        public static string Badge(string trangThai) => trangThai switch
        {
            TrangThaiHoSo.Nhap => "bg-secondary",
            TrangThaiHoSo.ChoDuyet => "bg-warning text-dark",
            TrangThaiHoSo.ChoChinhSua => "bg-info text-dark",
            TrangThaiHoSo.DaDuyet => "bg-success",
            TrangThaiHoSo.DaTuChoi => "bg-danger",
            TrangThaiHoSo.DaHuy => "bg-dark",
            _ => "bg-light text-dark"
        };

        // Nhãn dạng viên thuốc (danh sách và chi tiết)
        public static string Pill(string trangThai) => trangThai switch
        {
            TrangThaiHoSo.Nhap => "pill pill-draft",
            TrangThaiHoSo.ChoDuyet => "pill pill-wait",
            TrangThaiHoSo.ChoChinhSua => "pill pill-edit",
            TrangThaiHoSo.DaDuyet => "pill pill-ok",
            TrangThaiHoSo.DaTuChoi => "pill pill-bad",
            TrangThaiHoSo.DaHuy => "pill pill-cancel",
            _ => "pill pill-draft"
        };
    }

    public static class UiHelper
    {
        // Chữ cái đầu của họ và tên, ví dụ "Nguyễn Văn An" thành "NA"
        public static string Initials(string? ten)
        {
            if (string.IsNullOrWhiteSpace(ten)) return "?";
            var p = ten.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return p.Length == 1 ? p[0][..1].ToUpper() : (p[0][..1] + p[^1][..1]).ToUpper();
        }

        public static string AvatarClass(int id) => "av-" + (Math.Abs(id) % 6);
    }
}