using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Controllers
{
    [Authorize]
    public class TaiKhoanController : Controller
    {
        private const int MatKhauToiThieu = 6;
        private readonly ApplicationDbContext _db;
        private readonly PasswordHasher<NguoiDung> _hasher = new PasswordHasher<NguoiDung>();

        public TaiKhoanController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int Me => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<NguoiDung?> LayToi()
        {
            return await _db.NguoiDungs
                .Include(u => u.PhongBan)
                .Include(u => u.ChucDanh)
                .Include(u => u.NguoiQuanLy)
                .Include(u => u.NguoiDungVaiTros).ThenInclude(x => x.VaiTro)
                .FirstOrDefaultAsync(u => u.MaNguoiDung == Me);
        }

        private static void DocChiXem(TaiKhoanVm vm, NguoiDung u)
        {
            vm.TenDangNhap = u.TenDangNhap;
            vm.VaiTro = string.Join(", ", u.NguoiDungVaiTros.Select(x => x.VaiTro?.TenVaiTro));
            vm.PhongBan = u.PhongBan?.TenPhongBan;
            vm.ChucDanh = u.ChucDanh?.TenChucDanh;
            vm.QuanLy = u.NguoiQuanLy?.HoTen;
            vm.TrangThai = u.TrangThai ? "Đang hoạt động" : "Đã khóa";
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var u = await LayToi();
            if (u == null) return NotFound();

            var vm = new TaiKhoanVm { HoTen = u.HoTen, Email = u.Email };
            DocChiXem(vm, u);
            return View(vm);
        }

        // Chỉ cập nhật họ tên và email
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhat(TaiKhoanVm vm)
        {
            var u = await LayToi();
            if (u == null) return NotFound();

            if (!ModelState.IsValid)
            {
                DocChiXem(vm, u);
                return View("Index", vm);
            }

            u.HoTen = vm.HoTen.Trim();
            u.Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();
            await _db.SaveChangesAsync();

            await LamMoiCookie(u); // để tên trên thanh trên đổi ngay
            TempData["ThongBao"] = "Đã cập nhật thông tin cá nhân.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiMatKhau(string? matKhauCu, string? matKhauMoi, string? xacNhan)
        {
            var u = await LayToi();
            if (u == null) return NotFound();

            if (string.IsNullOrEmpty(matKhauCu) || string.IsNullOrEmpty(matKhauMoi) || string.IsNullOrEmpty(xacNhan))
            {
                TempData["LoiMk"] = "Vui lòng nhập đầy đủ ba ô mật khẩu.";
                return RedirectToAction(nameof(Index));
            }
            if (string.IsNullOrEmpty(u.MatKhau)
                || _hasher.VerifyHashedPassword(u, u.MatKhau, matKhauCu) == PasswordVerificationResult.Failed)
            {
                TempData["LoiMk"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction(nameof(Index));
            }
            if (matKhauMoi.Length < MatKhauToiThieu)
            {
                TempData["LoiMk"] = $"Mật khẩu mới phải có ít nhất {MatKhauToiThieu} ký tự.";
                return RedirectToAction(nameof(Index));
            }
            if (matKhauMoi != xacNhan)
            {
                TempData["LoiMk"] = "Mật khẩu mới và ô xác nhận không khớp.";
                return RedirectToAction(nameof(Index));
            }
            if (matKhauMoi == matKhauCu)
            {
                TempData["LoiMk"] = "Mật khẩu mới phải khác mật khẩu hiện tại.";
                return RedirectToAction(nameof(Index));
            }

            u.MatKhau = _hasher.HashPassword(u, matKhauMoi);
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Index));
        }

        // Đăng nhập lại cookie với thông tin mới nhất
        private async Task LamMoiCookie(NguoiDung u)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, u.MaNguoiDung.ToString()),
                new Claim(ClaimTypes.Name, u.HoTen),
                new Claim("TenDangNhap", u.TenDangNhap)
            };
            foreach (var nv in u.NguoiDungVaiTros)
                if (nv.VaiTro != null) claims.Add(new Claim(ClaimTypes.Role, nv.VaiTro.TenVaiTro));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });
        }
    }
}