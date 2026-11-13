using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;
using InternalRequest_Management.Services;

namespace InternalRequest_Management.Controllers
{
    [Authorize(Roles = TenVaiTro.NguoiPheDuyet)]
    public class GoiYAiController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly GeminiService _ai;

        public GoiYAiController(ApplicationDbContext db, GeminiService ai)
        {
            _db = db;
            _ai = ai;
        }

        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> TomTat(int id)
        {
            var h = await _db.HoSoDeNghis
                .Include(x => x.LoaiDeNghi)
                .Include(x => x.NguoiDeNghi).ThenInclude(u => u!.ChucDanh)
                .Include(x => x.BuocXuLyHoSos)
                .FirstOrDefaultAsync(x => x.MaHoSo == id);
            if (h == null) return NotFound();

            // Chỉ người đang phải xử lý hồ sơ mới dùng được
            bool dangXuLy = h.TrangThai == TrangThaiHoSo.ChoDuyet
                && h.BuocXuLyHoSos.Any(b => b.ThoiDiemHoanTat == null && b.MaNguoiDung == Me);
            if (!dangXuLy)
                return Json(new { ok = false, text = "Chỉ người đang xử lý đề nghị mới dùng được gợi ý AI." });

            var prompt = await DungPrompt(h);
            var (text, loi) = await _ai.HoiAsync(prompt, HttpContext.RequestAborted);
            return Json(new { ok = loi == null, text = text ?? loi });
        }

        // Số liệu do code tính, AI chỉ diễn giải
        private async Task<string> DungPrompt(HoSoDeNghi h)
        {
            var now = DateTime.Now;

            var cungLoai = await _db.HoSoDeNghis
                .Where(x => x.MaLoai == h.MaLoai && x.MaHoSo != h.MaHoSo
                         && x.TrangThai != TrangThaiHoSo.Nhap && x.TrangThai != TrangThaiHoSo.DaHuy)
                .ToListAsync();
            var kinhPhi = cungLoai.Where(x => x.KinhPhi.HasValue).Select(x => x.KinhPhi!.Value).ToList();

            var trungLich = await _db.HoSoDeNghis
                .Where(x => x.MaNguoiDung == h.MaNguoiDung && x.MaHoSo != h.MaHoSo
                    && (x.TrangThai == TrangThaiHoSo.ChoDuyet || x.TrangThai == TrangThaiHoSo.ChoChinhSua
                        || x.TrangThai == TrangThaiHoSo.DaDuyet)
                    && h.NgayBatDau != null && h.NgayKetThuc != null
                    && x.NgayBatDau != null && x.NgayKetThuc != null
                    && x.NgayBatDau <= h.NgayKetThuc && x.NgayKetThuc >= h.NgayBatDau)
                .Select(x => new { x.SoHoSo, x.NgayBatDau, x.NgayKetThuc })
                .ToListAsync();

            int soLanCungLoai90Ngay = await _db.HoSoDeNghis.CountAsync(x =>
                x.MaNguoiDung == h.MaNguoiDung && x.MaLoai == h.MaLoai && x.MaHoSo != h.MaHoSo
                && x.TrangThai != TrangThaiHoSo.Nhap && x.TrangThai != TrangThaiHoSo.DaHuy
                && x.NgayTao >= now.AddDays(-90));

            int soLanChinhSua = h.BuocXuLyHoSos.Count(b => b.KetQua == KetQuaXuLy.YeuCauChinhSua);

            var sb = new StringBuilder();
            sb.AppendLine("Bạn là trợ lý hỗ trợ người phê duyệt đề nghị nội bộ của một công ty.");
            sb.AppendLine("Chỉ dựa vào dữ liệu bên dưới, không suy đoán thêm và không bịa số liệu.");
            sb.AppendLine("Trả lời bằng tiếng Việt, tối đa 120 từ, theo đúng hai phần:");
            sb.AppendLine("1) Tóm tắt: 2 câu nêu đề nghị xin gì, trong thời gian nào.");
            sb.AppendLine("2) Điểm cần lưu ý: gạch đầu dòng, chỉ nêu điểm có căn cứ trong dữ liệu (thiếu thông tin, kinh phí cao so với mặt bằng, trùng lịch, gấp, từng phải chỉnh sửa). Nếu không có điểm nào, ghi: Không phát hiện điểm bất thường.");
            sb.AppendLine("Không được đưa ra quyết định duyệt hay từ chối.");
            sb.AppendLine();
            sb.AppendLine("DỮ LIỆU ĐỀ NGHỊ:");
            sb.AppendLine($"- Loại đề nghị: {h.LoaiDeNghi?.TenLoai}");
            sb.AppendLine($"- Chức danh người đề nghị: {h.NguoiDeNghi?.ChucDanh?.TenChucDanh ?? "không rõ"}");
            sb.AppendLine($"- Từ ngày: {h.NgayBatDau:dd/MM/yyyy}, đến ngày: {h.NgayKetThuc:dd/MM/yyyy}");
            if (h.LoaiDeNghi?.MaKyThuat != "NGHI_PHEP")
            {
                sb.AppendLine($"- Địa điểm: {(string.IsNullOrWhiteSpace(h.DiaDiem) ? "(để trống)" : h.DiaDiem)}");
                sb.AppendLine($"- Kinh phí dự kiến: {(h.KinhPhi.HasValue ? h.KinhPhi.Value.ToString("N0") + " đồng" : "(để trống)")}");
            }
            sb.AppendLine($"- Nội dung ({(h.NoiDung ?? "").Trim().Length} ký tự): {(string.IsNullOrWhiteSpace(h.NoiDung) ? "(để trống)" : h.NoiDung.Trim())}");
            sb.AppendLine();
            sb.AppendLine("SỐ LIỆU ĐỐI CHIẾU (do hệ thống tính):");

            if (h.NgayBatDau.HasValue)
            {
                int ngayConLai = (int)(h.NgayBatDau.Value.Date - now.Date).TotalDays;
                sb.AppendLine($"- Ngày bắt đầu cách hôm nay {ngayConLai} ngày{(ngayConLai <= 2 ? " (gấp)" : "")}.");
            }
            if (h.KinhPhi.HasValue && kinhPhi.Count >= 2)
            {
                double tb = kinhPhi.Average();
                sb.AppendLine($"- Kinh phí trung bình của {kinhPhi.Count} đề nghị cùng loại trước đó: {tb:N0} đồng; đề nghị này bằng {(tb > 0 ? h.KinhPhi.Value / tb : 0):0.0} lần mức trung bình.");
            }
            else if (h.KinhPhi.HasValue)
            {
                sb.AppendLine("- Chưa đủ dữ liệu cùng loại để so sánh kinh phí.");
            }
            sb.AppendLine(trungLich.Count == 0
                ? "- Không trùng lịch với đề nghị nào khác của người này."
                : "- Trùng lịch với: " + string.Join("; ", trungLich.Select(t => $"{t.SoHoSo} ({t.NgayBatDau:dd/MM}-{t.NgayKetThuc:dd/MM})")) + ".");
            sb.AppendLine($"- Số đề nghị cùng loại người này đã gửi trong 90 ngày qua: {soLanCungLoai90Ngay}.");
            sb.AppendLine($"- Số lần đề nghị này đã bị yêu cầu chỉnh sửa: {soLanChinhSua}.");
            return sb.ToString();
        }
    }
}