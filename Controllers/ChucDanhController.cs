using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Controllers
{
    [Authorize(Roles = TenVaiTro.QuanTriVien)]
    public class ChucDanhController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ChucDanhController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var ds = await _db.ChucDanhs.OrderBy(c => c.TenChucDanh).ToListAsync();
            ViewBag.SoNguoi = await _db.NguoiDungs.Where(u => u.MaChucDanh != null)
                .GroupBy(u => u.MaChucDanh!.Value)
                .Select(g => new { g.Key, Dem = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Dem);
            return View(ds);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string? tenChucDanh)
        {
            var ten = tenChucDanh?.Trim() ?? "";
            if (ten == "") TempData["Loi"] = "Vui lòng nhập tên chức danh.";
            else if (ten.Length > 255) TempData["Loi"] = "Tên chức danh tối đa 255 ký tự.";
            else if (await _db.ChucDanhs.AnyAsync(c => c.TenChucDanh == ten)) TempData["Loi"] = "Tên chức danh đã tồn tại.";
            else
            {
                _db.ChucDanhs.Add(new ChucDanh { TenChucDanh = ten });
                await _db.SaveChangesAsync();
                TempData["ThongBao"] = "Đã thêm chức danh.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.ChucDanhs.FindAsync(id);
            if (c == null) return NotFound();
            return View(new ChucDanhVm { MaChucDanh = c.MaChucDanh, TenChucDanh = c.TenChucDanh });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ChucDanhVm vm)
        {
            var c = await _db.ChucDanhs.FindAsync(vm.MaChucDanh);
            if (c == null) return NotFound();

            var ten = vm.TenChucDanh?.Trim() ?? "";
            if (ten != "" && await _db.ChucDanhs.AnyAsync(x => x.TenChucDanh == ten && x.MaChucDanh != vm.MaChucDanh))
                ModelState.AddModelError(nameof(vm.TenChucDanh), "Tên chức danh đã tồn tại.");
            if (!ModelState.IsValid) return View(vm);

            c.TenChucDanh = ten;
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã cập nhật chức danh.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Xoa(int id)
        {
            var c = await _db.ChucDanhs.FindAsync(id);
            if (c == null) return NotFound();

            if (await _db.NguoiDungs.AnyAsync(u => u.MaChucDanh == id))
            {
                TempData["Loi"] = "Không thể xóa chức danh đang có người giữ.";
                return RedirectToAction(nameof(Index));
            }

            _db.ChucDanhs.Remove(c);
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã xóa chức danh.";
            return RedirectToAction(nameof(Index));
        }
    }
}