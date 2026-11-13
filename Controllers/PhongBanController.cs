using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Controllers
{
    [Authorize(Roles = TenVaiTro.QuanTriVien)]
    public class PhongBanController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PhongBanController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ===================== DANH SÁCH DẠNG CÂY =====================
        public async Task<IActionResult> Index()
        {
            var all = await _db.PhongBans.AsNoTracking().ToListAsync();

            var nguoi = await _db.NguoiDungs.Where(u => u.MaPhongBan != null)
                .GroupBy(u => u.MaPhongBan!.Value)
                .Select(g => new { g.Key, Tong = g.Count(), HoatDong = g.Count(u => u.TrangThai) })
                .ToDictionaryAsync(x => x.Key, x => (x.Tong, x.HoatDong));

            var ten = all.ToDictionary(p => p.MaPhongBan, p => p.TenPhongBan);
            var rows = new List<PhongBanRow>();
            var daDuyet = new HashSet<int>();

            void Duyet(int? cha, int cap)
            {
                foreach (var p in all.Where(x => x.MaPhongBanCha == cha).OrderBy(x => x.TenPhongBan))
                {
                    if (!daDuyet.Add(p.MaPhongBan)) continue;
                    nguoi.TryGetValue(p.MaPhongBan, out var n);
                    var con = all.Where(x => x.MaPhongBanCha == p.MaPhongBan).ToList();
                    rows.Add(new PhongBanRow
                    {
                        PhongBan = p,
                        Cap = cap,
                        TenCha = p.MaPhongBanCha.HasValue && ten.ContainsKey(p.MaPhongBanCha.Value) ? ten[p.MaPhongBanCha.Value] : null,
                        SoNguoi = n.Tong,
                        SoNguoiHoatDong = n.HoatDong,
                        SoCon = con.Count,
                        SoConHoatDong = con.Count(c => c.TrangThai)
                    });
                    Duyet(p.MaPhongBan, cap + 1);
                }
            }
            Duyet(null, 0);

            return View(rows);
        }

        // ===================== THÊM =====================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await NapDanhSach(0);
            return View("Form", new PhongBanVm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhongBanVm vm)
        {
            await KiemTra(vm);
            if (!ModelState.IsValid)
            {
                await NapDanhSach(0);
                return View("Form", vm);
            }

            _db.PhongBans.Add(new PhongBan
            {
                TenPhongBan = vm.TenPhongBan.Trim(),
                MaPhongBanCha = vm.MaPhongBanCha,
                TrangThai = true
            });
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã thêm phòng ban.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== SỬA =====================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _db.PhongBans.FindAsync(id);
            if (p == null) return NotFound();

            await NapDanhSach(id);
            return View("Form", new PhongBanVm
            {
                MaPhongBan = p.MaPhongBan,
                TenPhongBan = p.TenPhongBan,
                MaPhongBanCha = p.MaPhongBanCha
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PhongBanVm vm)
        {
            var p = await _db.PhongBans.FindAsync(vm.MaPhongBan);
            if (p == null) return NotFound();

            await KiemTra(vm);
            if (!ModelState.IsValid)
            {
                await NapDanhSach(vm.MaPhongBan);
                return View("Form", vm);
            }

            p.TenPhongBan = vm.TenPhongBan.Trim();
            p.MaPhongBanCha = vm.MaPhongBanCha;
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã cập nhật phòng ban.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== NGỪNG HOẠT ĐỘNG / BẬT LẠI =====================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiTrangThai(int id)
        {
            var p = await _db.PhongBans.FindAsync(id);
            if (p == null) return NotFound();

            p.TrangThai = !p.TrangThai;
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = p.TrangThai ? "Đã bật lại phòng ban." : "Đã ngừng hoạt động phòng ban.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== XÓA (chỉ khi trống) =====================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Xoa(int id)
        {
            var p = await _db.PhongBans.FindAsync(id);
            if (p == null) return NotFound();

            bool coNguoi = await _db.NguoiDungs.AnyAsync(u => u.MaPhongBan == id);
            bool coCon = await _db.PhongBans.AnyAsync(x => x.MaPhongBanCha == id);
            if (coNguoi || coCon)
            {
                TempData["Loi"] = "Không thể xóa phòng ban còn người dùng hoặc còn phòng ban con. Bạn có thể ngừng hoạt động thay vì xóa.";
                return RedirectToAction(nameof(Index));
            }

            _db.PhongBans.Remove(p);
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã xóa phòng ban.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== HÀM DÙNG CHUNG =====================
        private async Task<HashSet<int>> LayConChau(int maGoc)
        {
            var cap = await _db.PhongBans.Select(p => new { p.MaPhongBan, p.MaPhongBanCha }).ToListAsync();
            var kq = new HashSet<int>();
            var hangDoi = new Queue<int>();
            hangDoi.Enqueue(maGoc);
            while (hangDoi.Count > 0)
            {
                var cur = hangDoi.Dequeue();
                foreach (var c in cap.Where(x => x.MaPhongBanCha == cur))
                    if (kq.Add(c.MaPhongBan)) hangDoi.Enqueue(c.MaPhongBan);
            }
            return kq;
        }

        private async Task NapDanhSach(int maHienTai)
        {
            var ds = await _db.PhongBans.OrderBy(p => p.TenPhongBan).ToListAsync();
            if (maHienTai > 0)
            {
                var loai = await LayConChau(maHienTai);
                loai.Add(maHienTai);
                ds = ds.Where(p => !loai.Contains(p.MaPhongBan)).ToList();
            }
            ViewBag.ChaList = ds;
        }

        private async Task KiemTra(PhongBanVm vm)
        {
            var ten = vm.TenPhongBan?.Trim() ?? "";
            if (ten != "" && await _db.PhongBans.AnyAsync(x => x.TenPhongBan == ten && x.MaPhongBan != vm.MaPhongBan))
                ModelState.AddModelError(nameof(vm.TenPhongBan), "Tên phòng ban đã tồn tại.");

            if (vm.MaPhongBanCha.HasValue)
            {
                if (!await _db.PhongBans.AnyAsync(x => x.MaPhongBan == vm.MaPhongBanCha))
                    ModelState.AddModelError(nameof(vm.MaPhongBanCha), "Phòng ban cấp trên không tồn tại.");
                else if (vm.MaPhongBan != 0)
                {
                    if (vm.MaPhongBanCha == vm.MaPhongBan)
                        ModelState.AddModelError(nameof(vm.MaPhongBanCha), "Không thể chọn chính nó làm cấp trên.");
                    else if ((await LayConChau(vm.MaPhongBan)).Contains(vm.MaPhongBanCha.Value))
                        ModelState.AddModelError(nameof(vm.MaPhongBanCha), "Lựa chọn này tạo vòng (đó là phòng ban trực thuộc của chính phòng ban này).");
                }
            }
        }
    }
}