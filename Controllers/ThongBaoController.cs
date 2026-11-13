using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Controllers
{
    [Authorize]
    public class ThongBaoController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ThongBaoController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private record TbItem(string Loai, string Text, string Sub, string Url, long T);

        private static long Ms(DateTime d) => new DateTimeOffset(d).ToUnixTimeMilliseconds();

        private static string Cat(string? s, int n)
        {
            s = (s ?? "").Trim();
            return s.Length > n ? s.Substring(0, n) + "…" : s;
        }

        [HttpGet]
        public async Task<IActionResult> Lay()
        {
            int me = Me;
            var now = DateTime.Now;
            var ds = new List<TbItem>();

            // 1. Người phê duyệt: đề nghị đang chờ mình, hoặc đã quá hạn
            if (User.IsInRole(TenVaiTro.NguoiPheDuyet))
            {
                var mo = await _db.BuocXuLyHoSos
                    .Where(b => b.MaNguoiDung == me && b.ThoiDiemHoanTat == null
                             && b.HoSoDeNghi!.TrangThai == TrangThaiHoSo.ChoDuyet)
                    .Select(b => new
                    {
                        b.MaHoSo,
                        So = b.HoSoDeNghi!.SoHoSo,
                        Loai = b.HoSoDeNghi.LoaiDeNghi!.TenLoai,
                        Nguoi = b.HoSoDeNghi.NguoiDeNghi!.HoTen,
                        Buoc = b.BuocQuyTrinh!.TenBuoc,
                        b.ThoiDiemNhan,
                        b.HanXuLy
                    })
                    .ToListAsync();

                foreach (var x in mo)
                {
                    var url = Url.Action("Details", "HoSoDeNghi", new { id = x.MaHoSo })!;
                    string sub = $"Bước: {x.Buoc} · Hạn {x.HanXuLy:dd/MM HH:mm}";
                    if (now > x.HanXuLy)
                        ds.Add(new TbItem("quahan", $"Đề nghị {x.So} ({x.Loai}) của {x.Nguoi} đã quá hạn xử lý", sub, url, Ms(x.HanXuLy)));
                    else
                        ds.Add(new TbItem("cho", $"{x.Nguoi} gửi đề nghị {x.Loai} {x.So} chờ bạn xử lý", sub, url, Ms(x.ThoiDiemNhan)));
                }
            }

            // 2. Người đề nghị: kết quả xử lý trên đề nghị của mình (30 ngày gần nhất)
            var tu = now.AddDays(-30);
            var xong = await _db.BuocXuLyHoSos
                .Where(b => b.HoSoDeNghi!.MaNguoiDung == me && b.MaNguoiDung != me
                         && b.ThoiDiemHoanTat != null && b.ThoiDiemHoanTat >= tu)
                .Select(b => new
                {
                    b.MaHoSo,
                    So = b.HoSoDeNghi!.SoHoSo,
                    TrangThaiHs = b.HoSoDeNghi.TrangThai,
                    Ten = b.NguoiXuLy!.HoTen,
                    Buoc = b.BuocQuyTrinh!.TenBuoc,
                    b.KetQua,
                    b.YKien,
                    Xong = b.ThoiDiemHoanTat!.Value
                })
                .ToListAsync();

            var cuoiCung = xong.GroupBy(x => x.MaHoSo).ToDictionary(g => g.Key, g => g.Max(x => x.Xong));

            foreach (var x in xong)
            {
                var url = Url.Action("Details", "HoSoDeNghi", new { id = x.MaHoSo })!;
                string yKien = string.IsNullOrWhiteSpace(x.YKien) ? $"Bước: {x.Buoc}" : "Ý kiến: " + Cat(x.YKien, 80);

                if (x.KetQua == KetQuaXuLy.PheDuyet)
                {
                    bool hoanTat = x.TrangThaiHs == TrangThaiHoSo.DaDuyet && x.Xong == cuoiCung[x.MaHoSo];
                    string text = hoanTat
                        ? $"{x.Ten} đã phê duyệt, đề nghị {x.So} của bạn đã được duyệt hoàn tất"
                        : $"{x.Ten} đã phê duyệt đề nghị {x.So} của bạn";
                    ds.Add(new TbItem("duyet", text, $"Bước: {x.Buoc}", url, Ms(x.Xong)));
                }
                else if (x.KetQua == KetQuaXuLy.TuChoi)
                {
                    ds.Add(new TbItem("tuchoi", $"{x.Ten} đã từ chối đề nghị {x.So} của bạn", yKien, url, Ms(x.Xong)));
                }
                else if (x.KetQua == KetQuaXuLy.YeuCauChinhSua)
                {
                    ds.Add(new TbItem("chinhsua", $"{x.Ten} yêu cầu bạn chỉnh sửa đề nghị {x.So}", yKien, url, Ms(x.Xong)));
                }
            }

            var kq = ds.OrderByDescending(x => x.T).Take(20).ToList();
            return Json(new { items = kq });
        }
    }
}