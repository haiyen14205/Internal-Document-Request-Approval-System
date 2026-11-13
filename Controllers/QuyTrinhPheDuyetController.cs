using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Controllers
{
    [Authorize(Roles = TenVaiTro.QuanTriVien)]
    public class QuyTrinhPheDuyetController : Controller
    {
        private readonly ApplicationDbContext _db;

        public QuyTrinhPheDuyetController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var ds = await _db.QuyTrinhPheDuyets
                .Include(q => q.LoaiDeNghi)
                .Include(q => q.BuocQuyTrinhs).ThenInclude(b => b.NguoiXuLy)
                .OrderBy(q => q.MaLoai).ThenByDescending(q => q.PhienBan)
                .ToListAsync();

            // Số đề nghị đang xử lý theo từng quy trình
            ViewBag.DangChay = await _db.HoSoDeNghis
                .Where(h => h.MaQuyTrinh != null
                    && (h.TrangThai == TrangThaiHoSo.ChoDuyet || h.TrangThai == TrangThaiHoSo.ChoChinhSua))
                .GroupBy(h => h.MaQuyTrinh!.Value)
                .Select(g => new { MaQT = g.Key, Dem = g.Count() })
                .ToDictionaryAsync(x => x.MaQT, x => x.Dem);

            return View(ds);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new QuyTrinhVm();
            vm.Buocs.Add(new BuocVm());
            await NapDanhSach(vm, daDung: false);
            return View("Form", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuyTrinhVm vm)
        {
            await KiemTra(vm);
            if (!ModelState.IsValid)
            {
                await NapDanhSach(vm, daDung: false);
                return View("Form", vm);
            }

            await TatQuyTrinhDangDung(vm.MaLoai, 0);
            var moi = TaoQuyTrinh(vm, await PhienBanTiepTheo(vm.MaLoai));
            _db.QuyTrinhPheDuyets.Add(moi);
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã tạo quy trình mới (các quy trình khác của loại này đã chuyển sang ngừng áp dụng).";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var qt = await _db.QuyTrinhPheDuyets
                .Include(q => q.BuocQuyTrinhs)
                .FirstOrDefaultAsync(q => q.MaQuyTrinh == id);
            if (qt == null) return NotFound();

            var vm = new QuyTrinhVm
            {
                MaQuyTrinh = qt.MaQuyTrinh,
                MaLoai = qt.MaLoai,
                TenQuyTrinh = qt.TenQuyTrinh,
                PhienBan = qt.PhienBan,
                Buocs = qt.BuocQuyTrinhs.OrderBy(b => b.ThuTuBuoc)
                    .Select(b => new BuocVm
                    {
                        TenBuoc = b.TenBuoc,
                        MaNguoiDung = b.MaNguoiDung,
                        ThoiHanXuLyGio = b.ThoiHanXuLyGio
                    }).ToList()
            };

            bool daDung = await _db.HoSoDeNghis.AnyAsync(h => h.MaQuyTrinh == id);
            await NapDanhSach(vm, daDung);
            return View("Form", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(QuyTrinhVm vm)
        {
            var qt = await _db.QuyTrinhPheDuyets
                .Include(q => q.BuocQuyTrinhs)
                .FirstOrDefaultAsync(q => q.MaQuyTrinh == vm.MaQuyTrinh);
            if (qt == null) return NotFound();

            vm.MaLoai = qt.MaLoai; // không cho đổi loại
            bool daDung = await _db.HoSoDeNghis.AnyAsync(h => h.MaQuyTrinh == qt.MaQuyTrinh);

            await KiemTra(vm);
            if (!ModelState.IsValid)
            {
                await NapDanhSach(vm, daDung);
                return View("Form", vm);
            }

            if (daDung)
            {
                // Đã có hồ sơ dùng: tạo phiên bản mới, giữ nguyên bản cũ cho hồ sơ cũ
                await TatQuyTrinhDangDung(qt.MaLoai, 0);
                qt.TrangThaiApDung = false;
                var moi = TaoQuyTrinh(vm, await PhienBanTiepTheo(qt.MaLoai));
                _db.QuyTrinhPheDuyets.Add(moi);
                await _db.SaveChangesAsync();

                TempData["ThongBao"] = $"Quy trình đã có hồ sơ sử dụng nên hệ thống tạo phiên bản mới (v{moi.PhienBan}). Hồ sơ đã gửi vẫn theo phiên bản cũ.";
            }
            else
            {
                // Chưa có hồ sơ dùng: sửa trực tiếp
                qt.TenQuyTrinh = vm.TenQuyTrinh.Trim();
                _db.BuocQuyTrinhs.RemoveRange(qt.BuocQuyTrinhs);
                for (int i = 0; i < vm.Buocs.Count; i++)
                {
                    qt.BuocQuyTrinhs.Add(new BuocQuyTrinh
                    {
                        ThuTuBuoc = i + 1,
                        TenBuoc = vm.Buocs[i].TenBuoc.Trim(),
                        MaNguoiDung = vm.Buocs[i].MaNguoiDung,
                        ThoiHanXuLyGio = vm.Buocs[i].ThoiHanXuLyGio
                    });
                }
                await _db.SaveChangesAsync();
                TempData["ThongBao"] = "Đã cập nhật quy trình.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Ngừng áp dụng / bật lại
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiTrangThai(int id)
        {
            var qt = await _db.QuyTrinhPheDuyets.FindAsync(id);
            if (qt == null) return NotFound();

            if (qt.TrangThaiApDung)
            {
                qt.TrangThaiApDung = false;
                TempData["ThongBao"] = "Đã ngừng áp dụng quy trình. Đề nghị đang xử lý vẫn tiếp tục theo quy trình này.";
            }
            else
            {
                await TatQuyTrinhDangDung(qt.MaLoai, qt.MaQuyTrinh);
                qt.TrangThaiApDung = true;
                TempData["ThongBao"] = "Đã bật lại quy trình (các quy trình khác của loại này đã ngừng áp dụng).";
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ---------- Hàm dùng chung ----------

        private async Task<List<NguoiDung>> LayNguoiPheDuyet()
        {
            return await _db.NguoiDungs
                .Where(u => u.TrangThai
                    && u.NguoiDungVaiTros.Any(r => r.VaiTro!.TenVaiTro == TenVaiTro.NguoiPheDuyet))
                .OrderBy(u => u.HoTen)
                .ToListAsync();
        }

        private async Task NapDanhSach(QuyTrinhVm vm, bool daDung)
        {
            ViewBag.LoaiList = await _db.LoaiDeNghis.Where(x => x.TrangThaiSuDung).OrderBy(x => x.TenLoai).ToListAsync();
            ViewBag.NguoiList = await LayNguoiPheDuyet();
            ViewBag.DaDung = daDung;
            vm.TenLoai = (await _db.LoaiDeNghis.FindAsync(vm.MaLoai))?.TenLoai ?? "";
        }

        private async Task KiemTra(QuyTrinhVm vm)
        {
            if (vm.Buocs.Count == 0)
                ModelState.AddModelError("", "Quy trình phải có ít nhất một bước xử lý.");

            if (vm.MaLoai > 0 && !await _db.LoaiDeNghis.AnyAsync(x => x.MaLoai == vm.MaLoai))
                ModelState.AddModelError(nameof(vm.MaLoai), "Loại đề nghị không tồn tại.");

            var hopLe = (await LayNguoiPheDuyet()).Select(u => u.MaNguoiDung).ToHashSet();
            for (int i = 0; i < vm.Buocs.Count; i++)
            {
                var id = vm.Buocs[i].MaNguoiDung;
                if (id > 0 && !hopLe.Contains(id))
                    ModelState.AddModelError("", $"Người xử lý ở bước {i + 1} không hợp lệ (phải là người có vai trò Người phê duyệt, đang hoạt động).");
            }
        }

        private async Task TatQuyTrinhDangDung(int maLoai, int except)
        {
            var dsDang = await _db.QuyTrinhPheDuyets
                .Where(q => q.MaLoai == maLoai && q.TrangThaiApDung && q.MaQuyTrinh != except)
                .ToListAsync();
            foreach (var q in dsDang) q.TrangThaiApDung = false;
        }

        private async Task<int> PhienBanTiepTheo(int maLoai)
        {
            var max = await _db.QuyTrinhPheDuyets.Where(q => q.MaLoai == maLoai).MaxAsync(q => (int?)q.PhienBan);
            return (max ?? 0) + 1;
        }

        private static QuyTrinhPheDuyet TaoQuyTrinh(QuyTrinhVm vm, int phienBan)
        {
            var qt = new QuyTrinhPheDuyet
            {
                MaLoai = vm.MaLoai,
                TenQuyTrinh = vm.TenQuyTrinh.Trim(),
                PhienBan = phienBan,
                TrangThaiApDung = true
            };
            for (int i = 0; i < vm.Buocs.Count; i++)
            {
                qt.BuocQuyTrinhs.Add(new BuocQuyTrinh
                {
                    ThuTuBuoc = i + 1,
                    TenBuoc = vm.Buocs[i].TenBuoc.Trim(),
                    MaNguoiDung = vm.Buocs[i].MaNguoiDung,
                    ThoiHanXuLyGio = vm.Buocs[i].ThoiHanXuLyGio
                });
            }
            return qt;
        }
    }
}