using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            int me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var now = DateTime.Now;

            bool laDeNghi = User.IsInRole(TenVaiTro.NguoiDeNghi);
            bool laPheDuyet = User.IsInRole(TenVaiTro.NguoiPheDuyet);
            bool laQuanTri = User.IsInRole(TenVaiTro.QuanTriVien);

            if (laDeNghi)
            {
                ViewBag.CuaToi = await _db.HoSoDeNghis
                    .Where(h => h.MaNguoiDung == me)
                    .GroupBy(h => h.TrangThai)
                    .Select(g => new { g.Key, Dem = g.Count() })
                    .ToDictionaryAsync(x => x.Key, x => x.Dem);
            }

            if (laPheDuyet)
            {
                var buoc = _db.BuocXuLyHoSos.Where(b => b.MaNguoiDung == me
                    && b.ThoiDiemHoanTat == null && b.HoSoDeNghi!.TrangThai == TrangThaiHoSo.ChoDuyet);
                ViewBag.CanXuLy = await buoc.CountAsync();
                ViewBag.QuaHanCuaToi = await buoc.CountAsync(b => b.HanXuLy < now);
            }

            if (laPheDuyet || laQuanTri)
            {
                var mo = _db.BuocXuLyHoSos.Where(b => b.ThoiDiemHoanTat == null
                    && b.HoSoDeNghi!.TrangThai == TrangThaiHoSo.ChoDuyet);
                ViewBag.HeThongDangCho = await mo.CountAsync();
                ViewBag.HeThongQuaHan = await mo.CountAsync(b => b.HanXuLy < now);
                ViewBag.TongHoSo = await _db.HoSoDeNghis.CountAsync();
            }

            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}