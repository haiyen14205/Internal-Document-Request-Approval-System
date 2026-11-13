using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var user = await _db.NguoiDungs
                .Include(u => u.NguoiDungVaiTros).ThenInclude(x => x.VaiTro)
                .FirstOrDefaultAsync(u => u.TenDangNhap == model.TenDangNhap);

            var hasher = new PasswordHasher<NguoiDung>();
            bool dungMatKhau = user != null
                && !string.IsNullOrEmpty(user.MatKhau)
                && hasher.VerifyHashedPassword(user, user.MatKhau, model.MatKhau) != PasswordVerificationResult.Failed;

            if (!dungMatKhau)
            {
                ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu.");
                return View(model);
            }

            if (!user!.TrangThai)
            {
                ModelState.AddModelError("", "Tài khoản đã bị khóa, vui lòng liên hệ quản trị viên.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen),
                new Claim("TenDangNhap", user.TenDangNhap)
            };
            foreach (var nv in user.NguoiDungVaiTros)
            {
                if (nv.VaiTro != null)
                    claims.Add(new Claim(ClaimTypes.Role, nv.VaiTro.TenVaiTro));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return Content("Bạn không có quyền truy cập trang này.");
        }
    }
}