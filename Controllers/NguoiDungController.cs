using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Controllers
{
    [Authorize(Roles = TenVaiTro.QuanTriVien)]
    public class NguoiDungController : Controller
    {
        private const int MatKhauToiThieu = 6;
        private readonly ApplicationDbContext _db;
        private readonly PasswordHasher<NguoiDung> _hasher = new PasswordHasher<NguoiDung>();

        public NguoiDungController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ===================== DANH SÁCH =====================
        public async Task<IActionResult> Index(string? tuKhoa)
        {
            var q = _db.NguoiDungs
                .Include(u => u.PhongBan)
                .Include(u => u.ChucDanh)
                .Include(u => u.NguoiDungVaiTros).ThenInclude(x => x.VaiTro)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var k = tuKhoa.Trim();
                q = q.Where(u => u.TenDangNhap.Contains(k) || u.HoTen.Contains(k)
                              || (u.Email != null && u.Email.Contains(k)));
            }

            var ds = await q.OrderBy(u => u.HoTen).ToListAsync();

            // Số bước được gán trong quy trình đang áp dụng
            ViewBag.BuocDuocGan = await _db.BuocQuyTrinhs
                .Where(b => b.QuyTrinhPheDuyet!.TrangThaiApDung)
                .GroupBy(b => b.MaNguoiDung)
                .Select(g => new { g.Key, Dem = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Dem);

            // Số hồ sơ đang chờ người đó xử lý
            ViewBag.HoSoDangCho = await _db.BuocXuLyHoSos
                .Where(b => b.ThoiDiemHoanTat == null && b.HoSoDeNghi!.TrangThai == TrangThaiHoSo.ChoDuyet)
                .GroupBy(b => b.MaNguoiDung)
                .Select(g => new { g.Key, Dem = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Dem);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaToi = Me;
            return View(ds);
        }

        // ===================== THÊM =====================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await NapDanhSach(0);
            return View("Form", new NguoiDungVm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NguoiDungVm vm)
        {
            await KiemTra(vm, laTao: true);
            if (!ModelState.IsValid)
            {
                await NapDanhSach(0);
                return View("Form", vm);
            }

            var u = new NguoiDung
            {
                TenDangNhap = vm.TenDangNhap.Trim(),
                HoTen = vm.HoTen.Trim(),
                Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim(),
                MaPhongBan = vm.MaPhongBan,
                MaChucDanh = vm.MaChucDanh,
                MaNguoiQuanLy = vm.MaNguoiQuanLy,
                TrangThai = vm.TrangThai
            };
            u.MatKhau = _hasher.HashPassword(u, vm.MatKhau!);
            foreach (var id in vm.VaiTroIds.Distinct())
                u.NguoiDungVaiTros.Add(new NguoiDungVaiTro { MaVaiTro = id });

            _db.NguoiDungs.Add(u);
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã tạo tài khoản mới.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== SỬA =====================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var u = await _db.NguoiDungs.Include(x => x.NguoiDungVaiTros).FirstOrDefaultAsync(x => x.MaNguoiDung == id);
            if (u == null) return NotFound();

            var vm = new NguoiDungVm
            {
                MaNguoiDung = u.MaNguoiDung,
                TenDangNhap = u.TenDangNhap,
                HoTen = u.HoTen,
                Email = u.Email,
                MaPhongBan = u.MaPhongBan,
                MaChucDanh = u.MaChucDanh,
                MaNguoiQuanLy = u.MaNguoiQuanLy,
                TrangThai = u.TrangThai,
                VaiTroIds = u.NguoiDungVaiTros.Select(x => x.MaVaiTro).ToList()
            };
            await NapDanhSach(id);
            return View("Form", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(NguoiDungVm vm)
        {
            var u = await _db.NguoiDungs.Include(x => x.NguoiDungVaiTros).FirstOrDefaultAsync(x => x.MaNguoiDung == vm.MaNguoiDung);
            if (u == null) return NotFound();

            await KiemTra(vm, laTao: false);
            if (!ModelState.IsValid)
            {
                await NapDanhSach(vm.MaNguoiDung);
                return View("Form", vm);
            }

            u.TenDangNhap = vm.TenDangNhap.Trim();
            u.HoTen = vm.HoTen.Trim();
            u.Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();
            u.MaPhongBan = vm.MaPhongBan;
            u.MaChucDanh = vm.MaChucDanh;
            u.MaNguoiQuanLy = vm.MaNguoiQuanLy;
            u.TrangThai = vm.TrangThai;
            if (!string.IsNullOrEmpty(vm.MatKhau))
                u.MatKhau = _hasher.HashPassword(u, vm.MatKhau);

            _db.NguoiDungVaiTros.RemoveRange(u.NguoiDungVaiTros);
            foreach (var id in vm.VaiTroIds.Distinct())
                u.NguoiDungVaiTros.Add(new NguoiDungVaiTro { MaNguoiDung = u.MaNguoiDung, MaVaiTro = id });

            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã cập nhật tài khoản. Nếu vai trò thay đổi, người dùng cần đăng nhập lại để áp dụng.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== KHÓA / MỞ KHÓA =====================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiTrangThai(int id)
        {
            var u = await _db.NguoiDungs.Include(x => x.NguoiDungVaiTros).ThenInclude(x => x.VaiTro)
                .FirstOrDefaultAsync(x => x.MaNguoiDung == id);
            if (u == null) return NotFound();

            if (u.TrangThai)
            {
                if (u.MaNguoiDung == Me)
                {
                    TempData["Loi"] = "Bạn không thể tự khóa tài khoản của chính mình.";
                    return RedirectToAction(nameof(Index));
                }
                bool laAdmin = u.NguoiDungVaiTros.Any(x => x.VaiTro!.TenVaiTro == TenVaiTro.QuanTriVien);
                if (laAdmin && await SoAdminKhac(u.MaNguoiDung) == 0)
                {
                    TempData["Loi"] = "Phải còn ít nhất một quản trị viên đang hoạt động.";
                    return RedirectToAction(nameof(Index));
                }
            }

            u.TrangThai = !u.TrangThai;
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = u.TrangThai ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== HÀM DÙNG CHUNG =====================
        private async Task<int> SoAdminKhac(int maNguoiBoQua)
        {
            return await _db.NguoiDungs.CountAsync(u => u.TrangThai && u.MaNguoiDung != maNguoiBoQua
                && u.NguoiDungVaiTros.Any(x => x.VaiTro!.TenVaiTro == TenVaiTro.QuanTriVien));
        }

        private async Task NapDanhSach(int maHienTai)
        {
            // Giữ cả phòng ban hiện tại của người đang sửa, kể cả khi nó đã ngừng hoạt động
            int? pbHienTai = maHienTai > 0
                ? await _db.NguoiDungs.Where(x => x.MaNguoiDung == maHienTai).Select(x => x.MaPhongBan).FirstOrDefaultAsync()
                : null;

            ViewBag.VaiTroList = await _db.VaiTros.OrderBy(v => v.MaVaiTro).ToListAsync();
            ViewBag.PhongBanList = await _db.PhongBans
                .Where(p => p.TrangThai || p.MaPhongBan == pbHienTai)
                .OrderBy(p => p.TenPhongBan).ToListAsync();
            ViewBag.ChucDanhList = await _db.ChucDanhs.OrderBy(c => c.TenChucDanh).ToListAsync();
            ViewBag.QuanLyList = await _db.NguoiDungs
                .Where(u => u.TrangThai && u.MaNguoiDung != maHienTai)
                .OrderBy(u => u.HoTen).ToListAsync();
        }

        private async Task KiemTra(NguoiDungVm vm, bool laTao)
        {
            var ten = vm.TenDangNhap?.Trim() ?? "";
            if (ten != "" && await _db.NguoiDungs.AnyAsync(u => u.TenDangNhap == ten && u.MaNguoiDung != vm.MaNguoiDung))
                ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại.");

            if (laTao && string.IsNullOrEmpty(vm.MatKhau))
                ModelState.AddModelError(nameof(vm.MatKhau), "Vui lòng nhập mật khẩu.");
            if (!string.IsNullOrEmpty(vm.MatKhau) && vm.MatKhau.Length < MatKhauToiThieu)
                ModelState.AddModelError(nameof(vm.MatKhau), $"Mật khẩu phải có ít nhất {MatKhauToiThieu} ký tự.");

            // Vai trò
            var ids = vm.VaiTroIds.Distinct().ToList();
            if (ids.Count == 0)
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một vai trò.");
            else
            {
                int hopLe = await _db.VaiTros.CountAsync(v => ids.Contains(v.MaVaiTro));
                if (hopLe != ids.Count) ModelState.AddModelError("", "Vai trò không hợp lệ.");
            }

            // Người quản lý trực tiếp: không chọn chính mình, không tạo vòng
            if (vm.MaNguoiQuanLy.HasValue && vm.MaNguoiDung != 0)
            {
                if (vm.MaNguoiQuanLy == vm.MaNguoiDung)
                    ModelState.AddModelError(nameof(vm.MaNguoiQuanLy), "Không thể chọn chính mình làm người quản lý.");
                else if (await CoVong(vm.MaNguoiDung, vm.MaNguoiQuanLy.Value))
                    ModelState.AddModelError(nameof(vm.MaNguoiQuanLy), "Lựa chọn này tạo vòng quản lý (người đó đang chịu sự quản lý của người này).");
            }

            // Bảo vệ quản trị viên (chỉ khi sửa)
            if (!laTao)
            {
                var adminId = await _db.VaiTros.Where(v => v.TenVaiTro == TenVaiTro.QuanTriVien)
                    .Select(v => (int?)v.MaVaiTro).FirstOrDefaultAsync();
                bool conLaAdmin = adminId.HasValue && ids.Contains(adminId.Value);

                if (vm.MaNguoiDung == Me)
                {
                    if (!vm.TrangThai)
                        ModelState.AddModelError(nameof(vm.TrangThai), "Bạn không thể tự khóa tài khoản của chính mình.");
                    if (!conLaAdmin)
                        ModelState.AddModelError("", "Bạn không thể tự bỏ vai trò Quản trị viên của chính mình.");
                }
                else if ((!conLaAdmin || !vm.TrangThai) && await DangLaAdmin(vm.MaNguoiDung)
                         && await SoAdminKhac(vm.MaNguoiDung) == 0)
                {
                    ModelState.AddModelError("", "Phải còn ít nhất một quản trị viên đang hoạt động.");
                }
            }
        }

        private async Task<bool> DangLaAdmin(int maNguoi) =>
            await _db.NguoiDungVaiTros.AnyAsync(x => x.MaNguoiDung == maNguoi && x.VaiTro!.TenVaiTro == TenVaiTro.QuanTriVien);

        // Đi ngược chuỗi quản lý từ người được chọn, nếu gặp lại chính người đang sửa thì có vòng
        private async Task<bool> CoVong(int maNguoi, int maQuanLyMoi)
        {
            int? cur = maQuanLyMoi;
            for (int i = 0; i < 50 && cur != null; i++)
            {
                if (cur == maNguoi) return true;
                cur = await _db.NguoiDungs.Where(u => u.MaNguoiDung == cur).Select(u => u.MaNguoiQuanLy).FirstOrDefaultAsync();
            }
            return false;
        }
    }
}