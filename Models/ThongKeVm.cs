namespace InternalRequest_Management.Models
{
    public class ThongKeBuoc
    {
        public int Hang { get; set; }
        public int MaBuoc { get; set; }
        public string TenLoai { get; set; } = string.Empty;
        public string TenQuyTrinh { get; set; } = string.Empty;
        public int PhienBan { get; set; }
        public string TenBuoc { get; set; } = string.Empty;
        public int SoLuot { get; set; }
        public double TbGio { get; set; }
        public double MaxGio { get; set; }
        public int SoQuaHan { get; set; }
        public double TyLeQuaHan { get; set; }
        public bool MauNho { get; set; }
    }

    public class ThongKeNguoi
    {
        public string HoTen { get; set; } = string.Empty;
        public int SoLuot { get; set; }
        public double TbGio { get; set; }
        public int SoQuaHan { get; set; }
    }

    public class ThongKeVm
    {
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public int? MaLoai { get; set; }

        public int TongLuot { get; set; }
        public double TbChung { get; set; }
        public int TongQuaHan { get; set; }
        public double TyLeQuaHan { get; set; }

        public List<ThongKeBuoc> Buocs { get; set; } = new List<ThongKeBuoc>();
        public List<ThongKeNguoi> Nguois { get; set; } = new List<ThongKeNguoi>();
        public Dictionary<string, int> TheoTrangThai { get; set; } = new Dictionary<string, int>();
    }
}