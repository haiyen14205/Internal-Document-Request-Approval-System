using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Controllers
{
    [Authorize(Roles = TenVaiTro.QuanTriVien)]
    public class LoaiDeNghiController : Controller
    {
        private readonly ApplicationDbContext _db;

        public LoaiDeNghiController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var ds = await _db.LoaiDeNghis.OrderBy(x => x.MaLoai).ToListAsync();

            // Số đề nghị chưa xử lý xong theo từng loại (dùng để cảnh báo khi ngừng sử dụng)
            ViewBag.ChuaXong = await _db.HoSoDeNghis
                .Where(h => h.TrangThai == TrangThaiHoSo.ChoDuyet || h.TrangThai == TrangThaiHoSo.ChoChinhSua)
                .GroupBy(h => h.MaLoai)
                .Select(g => new { MaLoai = g.Key, Dem = g.Count() })
                .ToDictionaryAsync(x => x.MaLoai, x => x.Dem);

            return View(ds);
        }

        [HttpGet]
        public IActionResult Create() => View("Form", new LoaiDeNghiVm());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoaiDeNghiVm vm)
        {
            await KiemTraTrung(vm);
            if (!ModelState.IsValid) return View("Form", vm);

            _db.LoaiDeNghis.Add(new LoaiDeNghi
            {
                TenLoai = vm.TenLoai.Trim(),
                MaKyThuat = vm.MaKyThuat.Trim().ToUpper(),
                TrangThaiSuDung = vm.TrangThaiSuDung
            });
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã tạo loại đề nghị mới.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var x = await _db.LoaiDeNghis.FindAsync(id);
            if (x == null) return NotFound();

            return View("Form", new LoaiDeNghiVm
            {
                MaLoai = x.MaLoai,
                TenLoai = x.TenLoai,
                MaKyThuat = x.MaKyThuat,
                TrangThaiSuDung = x.TrangThaiSuDung
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LoaiDeNghiVm vm)
        {
            var x = await _db.LoaiDeNghis.FindAsync(vm.MaLoai);
            if (x == null) return NotFound();

            await KiemTraTrung(vm);
            if (!ModelState.IsValid) return View("Form", vm);

            x.TenLoai = vm.TenLoai.Trim();
            x.MaKyThuat = vm.MaKyThuat.Trim().ToUpper();
            x.TrangThaiSuDung = vm.TrangThaiSuDung;
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã cập nhật loại đề nghị.";
            return RedirectToAction(nameof(Index));
        }

        // Ngừng sử dụng / bật lại
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiTrangThai(int id)
        {
            var x = await _db.LoaiDeNghis.FindAsync(id);
            if (x == null) return NotFound();

            x.TrangThaiSuDung = !x.TrangThaiSuDung;
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = x.TrangThaiSuDung ? "Đã bật lại loại đề nghị." : "Đã ngừng sử dụng loại đề nghị.";
            return RedirectToAction(nameof(Index));
        }

        private async Task KiemTraTrung(LoaiDeNghiVm vm)
        {
            if (string.IsNullOrWhiteSpace(vm.MaKyThuat)) return;
            var ma = vm.MaKyThuat.Trim().ToUpper();
            bool trung = await _db.LoaiDeNghis.AnyAsync(x => x.MaKyThuat == ma && x.MaLoai != vm.MaLoai);
            if (trung) ModelState.AddModelError(nameof(vm.MaKyThuat), "Mã kỹ thuật đã tồn tại.");
        }
    }
}