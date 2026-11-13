using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Controllers
{
    [Authorize(Roles = TenVaiTro.NguoiPheDuyet + "," + TenVaiTro.QuanTriVien)]
    public class GiamSatController : Controller
    {
        private readonly ApplicationDbContext _db;

        public GiamSatController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> QuaHan(int? maLoai)
        {
            var now = DateTime.Now;

            // Các bước đang mở của hồ sơ đang Chờ duyệt
            var dangMo = _db.BuocXuLyHoSos
                .Where(b => b.ThoiDiemHoanTat == null
                         && b.HoSoDeNghi!.TrangThai == TrangThaiHoSo.ChoDuyet);

            ViewBag.DangCho = await dangMo.CountAsync();
            ViewBag.SapHetHan = await dangMo.CountAsync(b => b.HanXuLy >= now && b.HanXuLy <= now.AddHours(4));

            var quaHan = dangMo.Where(b => b.HanXuLy < now);
            ViewBag.SoQuaHan = await quaHan.CountAsync();

            if (maLoai.HasValue && maLoai > 0)
                quaHan = quaHan.Where(b => b.HoSoDeNghi!.MaLoai == maLoai);

            var ds = await quaHan
                .Include(b => b.HoSoDeNghi).ThenInclude(h => h!.LoaiDeNghi)
                .Include(b => b.HoSoDeNghi).ThenInclude(h => h!.NguoiDeNghi)
                .Include(b => b.BuocQuyTrinh)
                .Include(b => b.NguoiXuLy)
                .OrderBy(b => b.HanXuLy) // trễ nhất lên đầu
                .ToListAsync();

            ViewBag.LoaiList = await _db.LoaiDeNghis.OrderBy(x => x.TenLoai).ToListAsync();
            ViewBag.MaLoai = maLoai;
            return View(ds);
        }
    }
}