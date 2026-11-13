using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternalRequest_Management.Models
{
    // Tên trạng thái hồ sơ dùng thống nhất toàn hệ thống
    public static class TrangThaiHoSo
    {
        public const string Nhap = "Nháp";
        public const string ChoDuyet = "Chờ duyệt";
        public const string ChoChinhSua = "Chờ chỉnh sửa";
        public const string DaDuyet = "Đã duyệt";
        public const string DaTuChoi = "Đã từ chối";
        public const string DaHuy = "Đã hủy";
    }

    // Kết quả xử lý của một bước
    public static class KetQuaXuLy
    {
        public const string PheDuyet = "Phê duyệt";
        public const string TuChoi = "Từ chối";
        public const string YeuCauChinhSua = "Yêu cầu chỉnh sửa";
    }

    // Tên 3 vai trò
    public static class TenVaiTro
    {
        public const string NguoiDeNghi = "Người đề nghị";
        public const string NguoiPheDuyet = "Người phê duyệt";
        public const string QuanTriVien = "Quản trị viên";
    }

    [Table("PhongBan")]
    public class PhongBan
    {
        [Key]
        public int MaPhongBan { get; set; }

        [Required, MaxLength(255)]
        public string TenPhongBan { get; set; } = string.Empty;

        public bool TrangThai { get; set; } = true;

        public int? MaPhongBanCha { get; set; }
        [ForeignKey("MaPhongBanCha")]
        public PhongBan? PhongBanCapTren { get; set; }

        public ICollection<PhongBan> PhongBanCapDuoi { get; set; } = new List<PhongBan>();
        public ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
    }

    [Table("ChucDanh")]
    public class ChucDanh
    {
        [Key]
        public int MaChucDanh { get; set; }

        [Required, MaxLength(255)]
        public string TenChucDanh { get; set; } = string.Empty;

        public ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
    }

    [Table("VaiTro")]
    public class VaiTro
    {
        [Key]
        public int MaVaiTro { get; set; }

        [Required, MaxLength(255)]
        public string TenVaiTro { get; set; } = string.Empty;

        public ICollection<NguoiDungVaiTro> NguoiDungVaiTros { get; set; } = new List<NguoiDungVaiTro>();
    }

    [Table("NguoiDung")]
    public class NguoiDung
    {
        [Key]
        public int MaNguoiDung { get; set; }

        [Required, MaxLength(255)]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string HoTen { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Email { get; set; }

        // Lưu mật khẩu đã băm, không lưu chữ thường
        [MaxLength(255)]
        public string? MatKhau { get; set; }

        public bool TrangThai { get; set; } = true;

        public int? MaPhongBan { get; set; }
        [ForeignKey("MaPhongBan")]
        public PhongBan? PhongBan { get; set; }

        public int? MaChucDanh { get; set; }
        [ForeignKey("MaChucDanh")]
        public ChucDanh? ChucDanh { get; set; }

        public int? MaNguoiQuanLy { get; set; }
        [ForeignKey("MaNguoiQuanLy")]
        public NguoiDung? NguoiQuanLy { get; set; }

        public ICollection<NguoiDung> NhanVienCapDuoi { get; set; } = new List<NguoiDung>();
        public ICollection<NguoiDungVaiTro> NguoiDungVaiTros { get; set; } = new List<NguoiDungVaiTro>();
        public ICollection<BuocQuyTrinh> BuocQuyTrinhs { get; set; } = new List<BuocQuyTrinh>();
        public ICollection<HoSoDeNghi> HoSoDeNghis { get; set; } = new List<HoSoDeNghi>();
        public ICollection<BuocXuLyHoSo> BuocXuLyHoSos { get; set; } = new List<BuocXuLyHoSo>();
        public ICollection<LichSuXuLy> LichSuXuLys { get; set; } = new List<LichSuXuLy>();
    }

    [Table("NguoiDungVaiTro")]
    public class NguoiDungVaiTro
    {
        public int MaNguoiDung { get; set; }
        [ForeignKey("MaNguoiDung")]
        public NguoiDung? NguoiDung { get; set; }

        public int MaVaiTro { get; set; }
        [ForeignKey("MaVaiTro")]
        public VaiTro? VaiTro { get; set; }
    }

    [Table("LoaiDeNghi")]
    public class LoaiDeNghi
    {
        [Key]
        public int MaLoai { get; set; }

        [Required, MaxLength(255)]
        public string TenLoai { get; set; } = string.Empty;

        // NGHI_PHEP, CONG_TAC, HOAT_DONG
        [Required, MaxLength(50)]
        public string MaKyThuat { get; set; } = string.Empty;

        public bool TrangThaiSuDung { get; set; } = true;

        public ICollection<QuyTrinhPheDuyet> QuyTrinhPheDuyets { get; set; } = new List<QuyTrinhPheDuyet>();
        public ICollection<HoSoDeNghi> HoSoDeNghis { get; set; } = new List<HoSoDeNghi>();
    }

    [Table("QuyTrinhPheDuyet")]
    public class QuyTrinhPheDuyet
    {
        [Key]
        public int MaQuyTrinh { get; set; }

        [Required, MaxLength(255)]
        public string TenQuyTrinh { get; set; } = string.Empty;

        public int PhienBan { get; set; } = 1;

        public bool TrangThaiApDung { get; set; } = true;

        public int MaLoai { get; set; }
        [ForeignKey("MaLoai")]
        public LoaiDeNghi? LoaiDeNghi { get; set; }

        public ICollection<BuocQuyTrinh> BuocQuyTrinhs { get; set; } = new List<BuocQuyTrinh>();
        public ICollection<HoSoDeNghi> HoSoDeNghis { get; set; } = new List<HoSoDeNghi>();
    }

    [Table("BuocQuyTrinh")]
    public class BuocQuyTrinh
    {
        [Key]
        public int MaBuoc { get; set; }

        [Required, MaxLength(255)]
        public string TenBuoc { get; set; } = string.Empty;

        public int ThuTuBuoc { get; set; }

        public int ThoiHanXuLyGio { get; set; } = 24;

        [MaxLength(255)]
        public string? DieuKienApDung { get; set; }

        public int MaQuyTrinh { get; set; }
        [ForeignKey("MaQuyTrinh")]
        public QuyTrinhPheDuyet? QuyTrinhPheDuyet { get; set; }

        // Người được gán xử lý bước này
        public int MaNguoiDung { get; set; }
        [ForeignKey("MaNguoiDung")]
        public NguoiDung? NguoiXuLy { get; set; }

        public ICollection<HoSoDeNghi> HoSoDangODayBuoc { get; set; } = new List<HoSoDeNghi>();
        public ICollection<BuocXuLyHoSo> BuocXuLyHoSos { get; set; } = new List<BuocXuLyHoSo>();
    }

    [Table("HoSoDeNghi")]
    public class HoSoDeNghi
    {
        [Key]
        public int MaHoSo { get; set; }

        [Required, MaxLength(255)]
        public string SoHoSo { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string TrangThai { get; set; } = TrangThaiHoSo.Nhap;

        public string? NoiDung { get; set; }

        [Column(TypeName = "date")]
        public DateTime? NgayBatDau { get; set; }

        [Column(TypeName = "date")]
        public DateTime? NgayKetThuc { get; set; }

        [MaxLength(255)]
        public string? DiaDiem { get; set; }

        public double? KinhPhi { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public int MaLoai { get; set; }
        [ForeignKey("MaLoai")]
        public LoaiDeNghi? LoaiDeNghi { get; set; }

        // Quy trình áp dụng, được gán khi tạo hồ sơ
        public int? MaQuyTrinh { get; set; }
        [ForeignKey("MaQuyTrinh")]
        public QuyTrinhPheDuyet? QuyTrinhPheDuyet { get; set; }

        // Bước hiện tại, để trống khi nháp hoặc đã kết thúc
        public int? MaBuoc { get; set; }
        [ForeignKey("MaBuoc")]
        public BuocQuyTrinh? BuocHienTai { get; set; }

        // Người đề nghị
        public int MaNguoiDung { get; set; }
        [ForeignKey("MaNguoiDung")]
        public NguoiDung? NguoiDeNghi { get; set; }

        public ICollection<BuocXuLyHoSo> BuocXuLyHoSos { get; set; } = new List<BuocXuLyHoSo>();
        public ICollection<LichSuXuLy> LichSuXuLys { get; set; } = new List<LichSuXuLy>();
        public ICollection<DeNghiDinhKem> DeNghiDinhKems { get; set; } = new List<DeNghiDinhKem>();
    }

    [Table("BuocXuLyHoSo")]
    public class BuocXuLyHoSo
    {
        [Key]
        public int MaBuocXuLy { get; set; }

        public DateTime ThoiDiemNhan { get; set; } = DateTime.Now;

        public DateTime HanXuLy { get; set; }

        public DateTime? ThoiDiemHoanTat { get; set; }

        [MaxLength(50)]
        public string? KetQua { get; set; }

        public string? YKien { get; set; }

        public double? ThoiGianXuLyGio { get; set; }

        public bool QuaHan { get; set; } = false;

        public int MaHoSo { get; set; }
        [ForeignKey("MaHoSo")]
        public HoSoDeNghi? HoSoDeNghi { get; set; }

        public int MaBuoc { get; set; }
        [ForeignKey("MaBuoc")]
        public BuocQuyTrinh? BuocQuyTrinh { get; set; }

        // Người xử lý thực tế
        public int MaNguoiDung { get; set; }
        [ForeignKey("MaNguoiDung")]
        public NguoiDung? NguoiXuLy { get; set; }
    }

    [Table("LichSuXuLy")]
    public class LichSuXuLy
    {
        [Key]
        public int MaDong { get; set; }

        public string? NoiDung { get; set; }

        public DateTime ThoiDiem { get; set; } = DateTime.Now;

        public int MaHoSo { get; set; }
        [ForeignKey("MaHoSo")]
        public HoSoDeNghi? HoSoDeNghi { get; set; }

        public int MaNguoiDung { get; set; }
        [ForeignKey("MaNguoiDung")]
        public NguoiDung? NguoiThucHien { get; set; }
    }

    [Table("TepDinhKem")]
    public class TepDinhKem
    {
        [Key]
        public int MaTep { get; set; }

        [Required, MaxLength(255)]
        public string TenTep { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? LoaiTep { get; set; }

        public int? KichThuoc { get; set; }

        public byte[]? DuLieuTep { get; set; }

        public DateTime NgayTaiLen { get; set; } = DateTime.Now;

        public ICollection<DeNghiDinhKem> DeNghiDinhKems { get; set; } = new List<DeNghiDinhKem>();
    }

    [Table("DeNghiDinhKem")]
    public class DeNghiDinhKem
    {
        public int MaTep { get; set; }
        [ForeignKey("MaTep")]
        public TepDinhKem? TepDinhKem { get; set; }

        public int MaHoSo { get; set; }
        [ForeignKey("MaHoSo")]
        public HoSoDeNghi? HoSoDeNghi { get; set; }
    }
}