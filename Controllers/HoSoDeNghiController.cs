using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;
using InternalRequest_Management.Services;

namespace InternalRequest_Management.Controllers
{
    [Authorize]
    public class HoSoDeNghiController : Controller
    {
        private static readonly string[] DuoiChoPhep = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg" };
        private const long TepToiDa = 10 * 1024 * 1024; // 10 MB
        private const int SoTepToiDa = 5;

        private readonly ApplicationDbContext _db;
        private readonly QuyTrinhService _qt;

        public HoSoDeNghiController(ApplicationDbContext db, QuyTrinhService qt)
        {
            _db = db;
            _qt = qt;
        }

        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ===================== DANH SÁCH + TÌM KIẾM + LỌC =====================
        public async Task<IActionResult> Index(string? muc, string? tuKhoa, int? maLoai,
            string? trangThai, DateTime? tuNgay, DateTime? denNgay)
        {
            int me = Me;
            bool laPheDuyet = User.IsInRole(TenVaiTro.NguoiPheDuyet);
            bool laQuanTri = User.IsInRole(TenVaiTro.QuanTriVien);
            if (muc == "tatca" && laQuanTri) { }
            else if (muc == "canxuly" && laPheDuyet) { }
            else muc = (laQuanTri && !laPheDuyet && !User.IsInRole(TenVaiTro.NguoiDeNghi)) ? "tatca" : "cuatoi";

            IQueryable<HoSoDeNghi> q = _db.HoSoDeNghis
                .Include(h => h.LoaiDeNghi)
                .Include(h => h.NguoiDeNghi)
                .Include(h => h.BuocHienTai)
                .Include(h => h.BuocXuLyHoSos);

            // Phạm vi quyền
            if (muc == "canxuly")
                q = q.Where(h => h.TrangThai == TrangThaiHoSo.ChoDuyet
                              && h.BuocHienTai != null && h.BuocHienTai.MaNguoiDung == me);
            else if (muc == "tatca")
            {
                // quản trị viên xem tất cả, không giới hạn
            }
            else
                q = q.Where(h => h.MaNguoiDung == me);

            // Tìm kiếm và lọc
            bool coLoc = false;
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var k = tuKhoa.Trim();
                q = q.Where(h => h.SoHoSo.Contains(k)
                              || (h.NoiDung != null && h.NoiDung.Contains(k))
                              || h.NguoiDeNghi!.HoTen.Contains(k));
                coLoc = true;
            }
            if (maLoai.HasValue && maLoai > 0) { q = q.Where(h => h.MaLoai == maLoai); coLoc = true; }
            if (!string.IsNullOrEmpty(trangThai)) { q = q.Where(h => h.TrangThai == trangThai); coLoc = true; }
            if (tuNgay.HasValue) { q = q.Where(h => h.NgayTao >= tuNgay.Value.Date); coLoc = true; }
            if (denNgay.HasValue) { q = q.Where(h => h.NgayTao < denNgay.Value.Date.AddDays(1)); coLoc = true; }

            // Hồ sơ chờ duyệt xếp lên trên, rồi mới nhất trước
            var ds = await q
                .OrderBy(h => h.TrangThai == TrangThaiHoSo.ChoDuyet ? 0 : 1)
                .ThenByDescending(h => h.NgayTao)
                .ToListAsync();

            ViewBag.Muc = muc;
            ViewBag.LaPheDuyet = laPheDuyet;
            ViewBag.LaQuanTri = laQuanTri;
            ViewBag.CoLoc = coLoc;
            ViewBag.LoaiList = await _db.LoaiDeNghis.OrderBy(x => x.TenLoai).ToListAsync();
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.MaLoai = maLoai;
            ViewBag.TrangThai = trangThai;
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");
            return View(ds);
        }

        // ===================== TẠO =====================
        [Authorize(Roles = TenVaiTro.NguoiDeNghi)]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await NapForm("tao", null, 0, null);
            return View("Form", new HoSoVm());
        }

        [Authorize(Roles = TenVaiTro.NguoiDeNghi)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HoSoVm vm, string? hanhDong)
        {
            bool gui = hanhDong == "gui";
            var loai = await _db.LoaiDeNghis.FirstOrDefaultAsync(x => x.MaLoai == vm.MaLoai && x.TrangThaiSuDung);
            if (loai == null)
                ModelState.AddModelError(nameof(vm.MaLoai), "Vui lòng chọn loại đề nghị hợp lệ.");

            KiemTra(vm, loai, gui, 0);
            if (!ModelState.IsValid)
            {
                await NapForm("tao", null, 0, null);
                return View("Form", vm);
            }

            var h = new HoSoDeNghi
            {
                MaLoai = loai!.MaLoai,
                MaNguoiDung = Me,
                TrangThai = TrangThaiHoSo.Nhap,
                NgayTao = DateTime.Now,
                SoHoSo = await SinhSoHoSo(loai)
            };
            GanDuLieu(h, vm, loai);
            await LuuTep(h, vm.TepMoi);
            _db.HoSoDeNghis.Add(h);
            _qt.GhiLichSu(h, Me, "Tạo đề nghị (nháp)");

            if (gui)
            {
                var loi = await _qt.GuiAsync(h, Me);
                if (loi != null)
                {
                    ModelState.AddModelError("", loi);
                    await NapForm("tao", null, 0, null);
                    return View("Form", vm);
                }
            }

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = gui ? "Gửi đề nghị thành công!" : "Đã lưu đề nghị ở trạng thái nháp.";
            return RedirectToAction(nameof(Details), new { id = h.MaHoSo });
        }

        // ===================== SỬA =====================
        [Authorize(Roles = TenVaiTro.NguoiDeNghi)]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var h = await _db.HoSoDeNghis.Include(x => x.LoaiDeNghi).FirstOrDefaultAsync(x => x.MaHoSo == id);
            if (h == null) return NotFound();
            if (h.MaNguoiDung != Me) return Forbid();
            if (!await CoTheSua(h))
            {
                TempData["Loi"] = "Đề nghị đã được xử lý hoặc đã kết thúc, không thể chỉnh sửa.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var vm = new HoSoVm
            {
                MaHoSo = h.MaHoSo,
                MaLoai = h.MaLoai,
                NoiDung = h.NoiDung,
                NgayBatDau = h.NgayBatDau,
                NgayKetThuc = h.NgayKetThuc,
                DiaDiem = h.DiaDiem,
                KinhPhi = h.KinhPhi
            };
            var tep = await LayTep(id);
            await NapForm(CheTheoTrangThai(h), h, tep.Count, tep);
            vm.TepHienCo = tep;
            return View("Form", vm);
        }

        [Authorize(Roles = TenVaiTro.NguoiDeNghi)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HoSoVm vm, string? hanhDong)
        {
            var h = await _db.HoSoDeNghis.Include(x => x.LoaiDeNghi).FirstOrDefaultAsync(x => x.MaHoSo == vm.MaHoSo);
            if (h == null) return NotFound();
            if (h.MaNguoiDung != Me) return Forbid();
            if (!await CoTheSua(h))
            {
                TempData["Loi"] = "Đề nghị đã được xử lý hoặc đã kết thúc, không thể chỉnh sửa.";
                return RedirectToAction(nameof(Details), new { id = h.MaHoSo });
            }

            vm.MaLoai = h.MaLoai;
            bool gui = hanhDong == "gui";
            var tepCu = await LayTep(h.MaHoSo);
            int soTepConLai = tepCu.Count(t => vm.TepXoa == null || !vm.TepXoa.Contains(t.MaTep));

            KiemTra(vm, h.LoaiDeNghi, gui, soTepConLai);
            if (!ModelState.IsValid)
            {
                vm.TepHienCo = tepCu;
                await NapForm(CheTheoTrangThai(h), h, tepCu.Count, tepCu);
                return View("Form", vm);
            }

            GanDuLieu(h, vm, h.LoaiDeNghi!);
            await XoaTep(h, vm.TepXoa);
            await LuuTep(h, vm.TepMoi);
            _qt.GhiLichSu(h, Me, "Cập nhật nội dung đề nghị");

            if (gui)
            {
                var loi = h.TrangThai == TrangThaiHoSo.ChoChinhSua
                    ? await _qt.GuiLaiAsync(h, Me)
                    : await _qt.GuiAsync(h, Me);
                if (loi != null)
                {
                    ModelState.AddModelError("", loi);
                    vm.TepHienCo = tepCu;
                    await NapForm(CheTheoTrangThai(h), h, tepCu.Count, tepCu);
                    return View("Form", vm);
                }
            }

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = gui ? "Đã gửi đề nghị thành công!" : "Cập nhật thành công.";
            return RedirectToAction(nameof(Details), new { id = h.MaHoSo });
        }

        // ===================== HỦY =====================
        [Authorize(Roles = TenVaiTro.NguoiDeNghi)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Huy(int id)
        {
            var h = await _db.HoSoDeNghis.Include(x => x.BuocXuLyHoSos).FirstOrDefaultAsync(x => x.MaHoSo == id);
            if (h == null) return NotFound();
            if (h.MaNguoiDung != Me) return Forbid();

            if (!await CoTheHuy(h))
            {
                TempData["Loi"] = "Đề nghị đã qua ít nhất một bước duyệt hoặc đã kết thúc nên không thể hủy.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Bỏ dòng xử lý đang mở (chưa ai xử lý) để không bị tính là quá hạn
            _db.BuocXuLyHoSos.RemoveRange(h.BuocXuLyHoSos.Where(b => b.ThoiDiemHoanTat == null).ToList());
            h.TrangThai = TrangThaiHoSo.DaHuy;
            h.MaBuoc = null;
            _qt.GhiLichSu(h, Me, "Hủy đề nghị");

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã hủy đề nghị.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== CHI TIẾT =====================
        public async Task<IActionResult> Details(int id)
        {
            var h = await _db.HoSoDeNghis
                .Include(x => x.LoaiDeNghi)
                .Include(x => x.NguoiDeNghi)
                .Include(x => x.QuyTrinhPheDuyet).ThenInclude(q => q!.BuocQuyTrinhs).ThenInclude(b => b.NguoiXuLy)
                .Include(x => x.BuocHienTai).ThenInclude(b => b!.NguoiXuLy)
                .Include(x => x.BuocXuLyHoSos).ThenInclude(b => b.BuocQuyTrinh)
                .Include(x => x.BuocXuLyHoSos).ThenInclude(b => b.NguoiXuLy)
                .Include(x => x.LichSuXuLys).ThenInclude(l => l.NguoiThucHien)
                .FirstOrDefaultAsync(x => x.MaHoSo == id);
            if (h == null) return NotFound();
            if (!await CoQuyenXem(h)) return Forbid();

            bool chu = h.MaNguoiDung == Me && User.IsInRole(TenVaiTro.NguoiDeNghi);
            ViewBag.ChuHoSo = chu;
            ViewBag.CoTheSua = chu && await CoTheSua(h);
            ViewBag.CoTheHuy = chu && await CoTheHuy(h);
            ViewBag.Tep = await LayTep(id);

            var buocDangMo = h.BuocXuLyHoSos
                .Where(b => b.ThoiDiemHoanTat == null)
                .OrderByDescending(b => b.ThoiDiemNhan)
                .FirstOrDefault();

            bool canXuLy = User.IsInRole(TenVaiTro.NguoiPheDuyet)
                && h.MaNguoiDung != Me
                && h.TrangThai == TrangThaiHoSo.ChoDuyet
                && buocDangMo != null && buocDangMo.MaNguoiDung == Me;
            ViewBag.CanXuLy = canXuLy;

            // Người có trong quy trình nhưng chưa đến lượt
            ViewBag.ChuaDenLuot = !canXuLy
                && h.MaNguoiDung != Me
                && h.TrangThai == TrangThaiHoSo.ChoDuyet
                && h.MaQuyTrinh != null
                && await _db.BuocQuyTrinhs.AnyAsync(b => b.MaQuyTrinh == h.MaQuyTrinh && b.MaNguoiDung == Me);

            // Lý do từ chối / nội dung yêu cầu chỉnh sửa gần nhất
            if (h.TrangThai == TrangThaiHoSo.DaTuChoi || h.TrangThai == TrangThaiHoSo.ChoChinhSua)
            {
                var kq = h.TrangThai == TrangThaiHoSo.DaTuChoi ? KetQuaXuLy.TuChoi : KetQuaXuLy.YeuCauChinhSua;
                ViewBag.LyDo = h.BuocXuLyHoSos
                    .Where(b => b.KetQua == kq)
                    .OrderByDescending(b => b.ThoiDiemHoanTat)
                    .FirstOrDefault();
            }
            return View(h);
        }

        // ===================== TẢI TỆP =====================
        public async Task<IActionResult> Tai(int maHoSo, int maTep)
        {
            var h = await _db.HoSoDeNghis.FindAsync(maHoSo);
            if (h == null) return NotFound();
            if (!await CoQuyenXem(h)) return Forbid();

            var lk = await _db.DeNghiDinhKems.Include(x => x.TepDinhKem)
                .FirstOrDefaultAsync(x => x.MaHoSo == maHoSo && x.MaTep == maTep);
            if (lk?.TepDinhKem?.DuLieuTep == null) return NotFound();

            return File(lk.TepDinhKem.DuLieuTep,
                lk.TepDinhKem.LoaiTep ?? "application/octet-stream",
                lk.TepDinhKem.TenTep);
        }

        // ===================== HÀM DÙNG CHUNG =====================
        private async Task<bool> DaXuLyBuocNao(int maHoSo) =>
            await _db.BuocXuLyHoSos.AnyAsync(x => x.MaHoSo == maHoSo && x.ThoiDiemHoanTat != null);

        private async Task<bool> DaQuaBuocDuyet(int maHoSo) =>
            await _db.BuocXuLyHoSos.AnyAsync(x => x.MaHoSo == maHoSo && x.KetQua == KetQuaXuLy.PheDuyet);

        private async Task<bool> CoTheSua(HoSoDeNghi h)
        {
            if (h.TrangThai == TrangThaiHoSo.Nhap || h.TrangThai == TrangThaiHoSo.ChoChinhSua) return true;
            if (h.TrangThai == TrangThaiHoSo.ChoDuyet) return !await DaXuLyBuocNao(h.MaHoSo);
            return false;
        }

        private async Task<bool> CoTheHuy(HoSoDeNghi h)
        {
            if (h.TrangThai == TrangThaiHoSo.Nhap) return true;
            if (h.TrangThai == TrangThaiHoSo.ChoDuyet || h.TrangThai == TrangThaiHoSo.ChoChinhSua)
                return !await DaQuaBuocDuyet(h.MaHoSo);
            return false;
        }

        private async Task<bool> CoQuyenXem(HoSoDeNghi h)
        {
            if (User.IsInRole(TenVaiTro.QuanTriVien)) return true;
            int me = Me;
            if (h.MaNguoiDung == me) return true;
            if (h.MaQuyTrinh != null &&
                await _db.BuocQuyTrinhs.AnyAsync(b => b.MaQuyTrinh == h.MaQuyTrinh && b.MaNguoiDung == me))
                return true;
            return await _db.BuocXuLyHoSos.AnyAsync(b => b.MaHoSo == h.MaHoSo && b.MaNguoiDung == me);
        }

        private static string CheTheoTrangThai(HoSoDeNghi h) => h.TrangThai switch
        {
            TrangThaiHoSo.ChoChinhSua => "chochinhsua",
            TrangThaiHoSo.ChoDuyet => "choduyet",
            _ => "nhap"
        };

        private async Task NapForm(string che, HoSoDeNghi? h, int soTep, List<TepInfo>? tep)
        {
            ViewBag.Che = che;
            ViewBag.LoaiList = await _db.LoaiDeNghis.Where(x => x.TrangThaiSuDung).OrderBy(x => x.TenLoai).ToListAsync();
            ViewBag.TenLoai = h?.LoaiDeNghi?.TenLoai ?? "";
            ViewBag.MaKyThuat = h?.LoaiDeNghi?.MaKyThuat ?? "";
            ViewBag.SoHoSo = h?.SoHoSo ?? "";
        }

        private async Task<List<TepInfo>> LayTep(int maHoSo)
        {
            return await _db.DeNghiDinhKems
                .Where(x => x.MaHoSo == maHoSo)
                .Select(x => new TepInfo
                {
                    MaTep = x.MaTep,
                    TenTep = x.TepDinhKem!.TenTep,
                    KichThuoc = x.TepDinhKem.KichThuoc
                })
                .ToListAsync();
        }

        private void KiemTra(HoSoVm vm, LoaiDeNghi? loai, bool gui, int soTepDangCo)
        {
            if (vm.NgayBatDau.HasValue && vm.NgayKetThuc.HasValue && vm.NgayKetThuc < vm.NgayBatDau)
                ModelState.AddModelError(nameof(vm.NgayKetThuc), "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");

            if (vm.KinhPhi.HasValue && vm.KinhPhi < 0)
                ModelState.AddModelError(nameof(vm.KinhPhi), "Kinh phí không được âm.");

            if (gui && loai != null)
            {
                if (string.IsNullOrWhiteSpace(vm.NoiDung))
                    ModelState.AddModelError(nameof(vm.NoiDung), "Vui lòng nhập nội dung / lý do.");
                if (!vm.NgayBatDau.HasValue)
                    ModelState.AddModelError(nameof(vm.NgayBatDau), "Vui lòng chọn ngày bắt đầu.");
                if (!vm.NgayKetThuc.HasValue)
                    ModelState.AddModelError(nameof(vm.NgayKetThuc), "Vui lòng chọn ngày kết thúc.");

                bool canDiaDiem = loai.MaKyThuat == "CONG_TAC" || loai.MaKyThuat == "HOAT_DONG";
                if (canDiaDiem && string.IsNullOrWhiteSpace(vm.DiaDiem))
                    ModelState.AddModelError(nameof(vm.DiaDiem), "Vui lòng nhập địa điểm.");
                if (loai.MaKyThuat == "HOAT_DONG" && !vm.KinhPhi.HasValue)
                    ModelState.AddModelError(nameof(vm.KinhPhi), "Vui lòng nhập kinh phí dự kiến.");
            }

            var tepMoi = vm.TepMoi?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();
            if (soTepDangCo + tepMoi.Count > SoTepToiDa)
                ModelState.AddModelError("", $"Mỗi đề nghị chỉ được đính kèm tối đa {SoTepToiDa} tệp.");
            foreach (var f in tepMoi)
            {
                var duoi = Path.GetExtension(f.FileName).ToLowerInvariant();
                if (!DuoiChoPhep.Contains(duoi))
                    ModelState.AddModelError("", $"Tệp \"{f.FileName}\" không được hỗ trợ (chỉ nhận pdf, doc, docx, xls, xlsx, png, jpg).");
                else if (f.Length > TepToiDa)
                    ModelState.AddModelError("", $"Tệp \"{f.FileName}\" vượt quá 10 MB.");
            }
        }

        private static void GanDuLieu(HoSoDeNghi h, HoSoVm vm, LoaiDeNghi loai)
        {
            bool nghiPhep = loai.MaKyThuat == "NGHI_PHEP";
            h.NoiDung = vm.NoiDung?.Trim();
            h.NgayBatDau = vm.NgayBatDau;
            h.NgayKetThuc = vm.NgayKetThuc;
            h.DiaDiem = nghiPhep ? null : vm.DiaDiem?.Trim();
            h.KinhPhi = nghiPhep ? null : vm.KinhPhi;
        }

        private async Task<string> SinhSoHoSo(LoaiDeNghi loai)
        {
            string tienTo = loai.MaKyThuat switch
            {
                "NGHI_PHEP" => "NP",
                "CONG_TAC" => "CT",
                "HOAT_DONG" => "HD",
                _ => "DN"
            };
            string dau = $"{tienTo}-{DateTime.Now:yyyy}-";

            var cacSo = await _db.HoSoDeNghis.Where(x => x.SoHoSo.StartsWith(dau)).Select(x => x.SoHoSo).ToListAsync();
            int max = 0;
            foreach (var s in cacSo)
                if (int.TryParse(s.Substring(dau.Length), out var n) && n > max) max = n;

            return dau + (max + 1).ToString("D4");
        }

        private async Task LuuTep(HoSoDeNghi h, List<IFormFile>? files)
        {
            if (files == null) return;
            foreach (var f in files.Where(x => x.Length > 0))
            {
                using var ms = new MemoryStream();
                await f.CopyToAsync(ms);
                var tep = new TepDinhKem
                {
                    TenTep = Path.GetFileName(f.FileName),
                    LoaiTep = f.ContentType,
                    KichThuoc = (int)f.Length,
                    DuLieuTep = ms.ToArray(),
                    NgayTaiLen = DateTime.Now
                };
                h.DeNghiDinhKems.Add(new DeNghiDinhKem { TepDinhKem = tep });
            }
        }

        private async Task XoaTep(HoSoDeNghi h, List<int>? ids)
        {
            if (ids == null || ids.Count == 0) return;
            var lk = await _db.DeNghiDinhKems.Include(x => x.TepDinhKem)
                .Where(x => x.MaHoSo == h.MaHoSo && ids.Contains(x.MaTep))
                .ToListAsync();
            foreach (var x in lk)
            {
                _db.DeNghiDinhKems.Remove(x);
                if (x.TepDinhKem != null) _db.TepDinhKems.Remove(x.TepDinhKem);
            }
        }
    }
}