using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;
using InternalRequest_Management.Services;

namespace InternalRequest_Management.Controllers
{
    [Authorize(Roles = TenVaiTro.NguoiPheDuyet)]
    public class PheDuyetController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly QuyTrinhService _qt;

        public PheDuyetController(ApplicationDbContext db, QuyTrinhService qt)
        {
            _db = db;
            _qt = qt;
        }

        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> Duyet(int id, string? yKien) => XuLy(id, "duyet", yKien);

        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> TuChoi(int id, string? yKien) => XuLy(id, "tuchoi", yKien);

        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> YeuCauChinhSua(int id, string? yKien) => XuLy(id, "chinhsua", yKien);

        private async Task<IActionResult> XuLy(int id, string hanhDong, string? yKien)
        {
            var h = await _db.HoSoDeNghis
                .Include(x => x.BuocXuLyHoSos)
                .Include(x => x.LichSuXuLys)
                .FirstOrDefaultAsync(x => x.MaHoSo == id);
            if (h == null) return NotFound();

            var loi = await _qt.XuLyAsync(h, Me, hanhDong, yKien);
            if (loi != null)
            {
                TempData["Loi"] = loi;
                return RedirectToAction("Details", "HoSoDeNghi", new { id });
            }

            await _db.SaveChangesAsync();

            TempData["ThongBao"] = hanhDong switch
            {
                "duyet" => "Bạn đã phê duyệt đề nghị này!",
                "tuchoi" => "Bạn đã từ chối đề nghị này.",
                _ => "Đã gửi yêu cầu chỉnh sửa cho người đề nghị."
            };
            return RedirectToAction("Index", "HoSoDeNghi", new { muc = "canxuly" });
        }
    }
}