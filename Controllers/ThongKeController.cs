using System.Text;
using InternalRequest_Management.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Controllers
{
    [Authorize(Roles = TenVaiTro.NguoiPheDuyet + "," + TenVaiTro.QuanTriVien)]
    public class ThongKeController : Controller
    {
        private const int MauNhoDuoi = 5;
        private readonly ApplicationDbContext _db;
        private readonly GeminiService _ai;

        public ThongKeController(ApplicationDbContext db, GeminiService ai)
        {
            _db = db;
            _ai = ai;
        }

        public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay, int? maLoai)
        {
            var vm = await Tinh(tuNgay, denNgay, maLoai);
            ViewBag.LoaiList = await _db.LoaiDeNghis.OrderBy(x => x.TenLoai).ToListAsync();
            return View(vm);
        }

        public async Task<IActionResult> XuatExcel(DateTime? tuNgay, DateTime? denNgay, int? maLoai)
        {
            var vm = await Tinh(tuNgay, denNgay, maLoai);

            using var wb = new XLWorkbook();

            var ws1 = wb.Worksheets.Add("Thống kê theo bước");
            string[] h1 = { "Hạng", "Loại đề nghị", "Quy trình", "Phiên bản", "Bước", "Số lượt xử lý",
                            "TB (giờ)", "Lớn nhất (giờ)", "Số lượt quá hạn", "Tỷ lệ quá hạn (%)", "Ghi chú" };
            for (int c = 0; c < h1.Length; c++) ws1.Cell(1, c + 1).Value = h1[c];
            int r = 2;
            foreach (var b in vm.Buocs)
            {
                ws1.Cell(r, 1).Value = b.Hang;
                ws1.Cell(r, 2).Value = b.TenLoai;
                ws1.Cell(r, 3).Value = b.TenQuyTrinh;
                ws1.Cell(r, 4).Value = b.PhienBan;
                ws1.Cell(r, 5).Value = b.TenBuoc;
                ws1.Cell(r, 6).Value = b.SoLuot;
                ws1.Cell(r, 7).Value = Math.Round(b.TbGio, 2);
                ws1.Cell(r, 8).Value = Math.Round(b.MaxGio, 2);
                ws1.Cell(r, 9).Value = b.SoQuaHan;
                ws1.Cell(r, 10).Value = Math.Round(b.TyLeQuaHan, 1);
                ws1.Cell(r, 11).Value = b.MauNho ? "Mẫu nhỏ, chỉ tham khảo" : "";
                r++;
            }
            ws1.Row(1).Style.Font.Bold = true;
            ws1.Columns().AdjustToContents();

            var ws2 = wb.Worksheets.Add("Thống kê theo người xử lý");
            string[] h2 = { "Người xử lý", "Số lượt xử lý", "TB (giờ)", "Số lượt quá hạn" };
            for (int c = 0; c < h2.Length; c++) ws2.Cell(1, c + 1).Value = h2[c];
            r = 2;
            foreach (var n in vm.Nguois)
            {
                ws2.Cell(r, 1).Value = n.HoTen;
                ws2.Cell(r, 2).Value = n.SoLuot;
                ws2.Cell(r, 3).Value = Math.Round(n.TbGio, 2);
                ws2.Cell(r, 4).Value = n.SoQuaHan;
                r++;
            }
            ws2.Row(1).Style.Font.Bold = true;
            ws2.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ThongKe_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ---------- Nhận xét bằng AI ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NhanXet(DateTime? tuNgay, DateTime? denNgay, int? maLoai)
        {
            var vm = await Tinh(tuNgay, denNgay, maLoai);
            if (vm.TongLuot == 0)
                return Json(new { ok = false, text = "Chưa có dữ liệu trong khoảng thời gian đã chọn để phân tích." });

            // Hạn xử lý quy định của từng bước, để AI so sánh với thời gian thực tế
            var ids = vm.Buocs.Select(b => b.MaBuoc).ToList();
            var han = await _db.BuocQuyTrinhs
                .Where(b => ids.Contains(b.MaBuoc))
                .ToDictionaryAsync(b => b.MaBuoc, b => b.ThoiHanXuLyGio);

            // Tình hình hiện tại
            var now = DateTime.Now;
            var dangMo = _db.BuocXuLyHoSos
                .Where(b => b.ThoiDiemHoanTat == null && b.HoSoDeNghi!.TrangThai == TrangThaiHoSo.ChoDuyet);
            int dangCho = await dangMo.CountAsync();
            int dangQuaHan = await dangMo.CountAsync(b => b.HanXuLy < now);

            string khoang = (tuNgay.HasValue || denNgay.HasValue)
                ? $"từ {tuNgay?.ToString("dd/MM/yyyy") ?? "đầu"} đến {denNgay?.ToString("dd/MM/yyyy") ?? "nay"}"
                : "toàn bộ thời gian";

            var sb = new StringBuilder();
            sb.AppendLine("Bạn là chuyên viên phân tích quy trình phê duyệt nội bộ, viết nhận xét cho người quản lý.");
            sb.AppendLine("Chỉ dựa vào số liệu bên dưới, không bịa số liệu. Trả lời bằng tiếng Việt, tối đa 200 từ, theo đúng ba phần:");
            sb.AppendLine("1) Nhận định chung: 2 câu về mức độ chậm trễ của toàn hệ thống.");
            sb.AppendLine("2) Bước cần ưu tiên: nêu bước nghẽn nhất, so thời gian thực tế với hạn quy định, tỷ lệ quá hạn. Nguyên nhân chỉ được viết dạng \"có thể do...\", không khẳng định.");
            sb.AppendLine("3) Gợi ý: 2 đến 3 hành động cụ thể (điều chỉnh hạn xử lý, bổ sung người duyệt dự phòng, nhắc việc, rút gọn bước...).");
            sb.AppendLine("Bước có ít hơn 5 lượt xử lý là mẫu nhỏ: chỉ ghi chú \"chưa đủ căn cứ kết luận\", không rút ra nhận định mạnh.");
            sb.AppendLine("Không nêu tên người, vì dữ liệu về người xử lý đã được ẩn danh.");
            sb.AppendLine();
            sb.AppendLine($"PHẠM VI: {khoang}.");
            sb.AppendLine($"TỔNG QUAN: {vm.TongLuot} lượt xử lý đã hoàn tất, thời gian trung bình {vm.TbChung:0.0} giờ, " +
                          $"{vm.TongQuaHan} lượt quá hạn ({vm.TyLeQuaHan:0.#}%).");
            sb.AppendLine($"HIỆN TẠI: {dangCho} hồ sơ đang chờ duyệt, trong đó {dangQuaHan} hồ sơ đã quá hạn.");
            sb.AppendLine();
            sb.AppendLine("CÁC BƯỚC (xếp từ chậm nhất):");
            foreach (var b in vm.Buocs.Take(8))
            {
                string hanQd = han.TryGetValue(b.MaBuoc, out var g) ? $"{g} giờ" : "không rõ";
                sb.AppendLine($"- Bước \"{b.TenBuoc}\" (loại {b.TenLoai}): {b.SoLuot} lượt, trung bình {b.TbGio:0.0} giờ, " +
                              $"lớn nhất {b.MaxGio:0.0} giờ, hạn quy định {hanQd}, quá hạn {b.SoQuaHan}/{b.SoLuot} lượt" +
                              (b.MauNho ? " [MẪU NHỎ]" : "") + ".");
            }
            sb.AppendLine();
            sb.AppendLine("NGƯỜI XỬ LÝ (ẩn danh, xếp từ chậm nhất):");
            int stt = 1;
            foreach (var n in vm.Nguois.Take(5))
            {
                sb.AppendLine($"- Người xử lý {stt++}: {n.SoLuot} lượt, trung bình {n.TbGio:0.0} giờ, quá hạn {n.SoQuaHan} lượt.");
            }

            var (text, loi) = await _ai.HoiAsync(sb.ToString(), HttpContext.RequestAborted);
            return Json(new { ok = loi == null, text = text ?? loi });
        }

        // ---------- Tính số liệu ----------
        private async Task<ThongKeVm> Tinh(DateTime? tuNgay, DateTime? denNgay, int? maLoai)
        {
            var vm = new ThongKeVm { TuNgay = tuNgay, DenNgay = denNgay, MaLoai = maLoai };

            var q = _db.BuocXuLyHoSos
                .Where(b => b.ThoiDiemHoanTat != null && b.ThoiGianXuLyGio != null);

            if (tuNgay.HasValue) q = q.Where(b => b.ThoiDiemHoanTat >= tuNgay.Value.Date);
            if (denNgay.HasValue) q = q.Where(b => b.ThoiDiemHoanTat < denNgay.Value.Date.AddDays(1));
            if (maLoai.HasValue && maLoai > 0) q = q.Where(b => b.HoSoDeNghi!.MaLoai == maLoai);

            var rows = await q.Select(b => new
            {
                b.MaBuoc,
                TenBuoc = b.BuocQuyTrinh!.TenBuoc,
                TenQT = b.BuocQuyTrinh.QuyTrinhPheDuyet!.TenQuyTrinh,
                PhienBan = b.BuocQuyTrinh.QuyTrinhPheDuyet.PhienBan,
                TenLoai = b.BuocQuyTrinh.QuyTrinhPheDuyet.LoaiDeNghi!.TenLoai,
                Gio = b.ThoiGianXuLyGio!.Value,
                b.QuaHan,
                b.MaNguoiDung,
                HoTen = b.NguoiXuLy!.HoTen
            }).ToListAsync();

            if (rows.Count > 0)
            {
                vm.TongLuot = rows.Count;
                vm.TbChung = rows.Average(x => x.Gio);
                vm.TongQuaHan = rows.Count(x => x.QuaHan);
                vm.TyLeQuaHan = vm.TongQuaHan * 100.0 / vm.TongLuot;

                vm.Buocs = rows
                    .GroupBy(x => new { x.MaBuoc, x.TenBuoc, x.TenQT, x.PhienBan, x.TenLoai })
                    .Select(g => new ThongKeBuoc
                    {
                        MaBuoc = g.Key.MaBuoc,
                        TenBuoc = g.Key.TenBuoc,
                        TenQuyTrinh = g.Key.TenQT,
                        PhienBan = g.Key.PhienBan,
                        TenLoai = g.Key.TenLoai,
                        SoLuot = g.Count(),
                        TbGio = g.Average(x => x.Gio),
                        MaxGio = g.Max(x => x.Gio),
                        SoQuaHan = g.Count(x => x.QuaHan),
                        TyLeQuaHan = g.Count(x => x.QuaHan) * 100.0 / g.Count(),
                        MauNho = g.Count() < MauNhoDuoi
                    })
                    .OrderByDescending(x => x.TbGio)
                    .ToList();
                for (int i = 0; i < vm.Buocs.Count; i++) vm.Buocs[i].Hang = i + 1;

                vm.Nguois = rows
                    .GroupBy(x => new { x.MaNguoiDung, x.HoTen })
                    .Select(g => new ThongKeNguoi
                    {
                        HoTen = g.Key.HoTen,
                        SoLuot = g.Count(),
                        TbGio = g.Average(x => x.Gio),
                        SoQuaHan = g.Count(x => x.QuaHan)
                    })
                    .OrderByDescending(x => x.TbGio)
                    .ToList();
            }

            // Số hồ sơ theo trạng thái (theo ngày tạo)
            var hs = _db.HoSoDeNghis.AsQueryable();
            if (tuNgay.HasValue) hs = hs.Where(h => h.NgayTao >= tuNgay.Value.Date);
            if (denNgay.HasValue) hs = hs.Where(h => h.NgayTao < denNgay.Value.Date.AddDays(1));
            if (maLoai.HasValue && maLoai > 0) hs = hs.Where(h => h.MaLoai == maLoai);
            vm.TheoTrangThai = await hs.GroupBy(h => h.TrangThai)
                .Select(g => new { g.Key, Dem = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Dem);

            return vm;
        }
    }
}