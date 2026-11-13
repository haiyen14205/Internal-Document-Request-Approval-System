using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Controllers
{
    [Authorize(Roles = TenVaiTro.QuanTriVien)]
    public class DemoController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DemoController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Xóa toàn bộ đề nghị hiện có rồi nạp bộ dữ liệu mẫu
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NapDuLieu()
        {
            await using var tran = await _db.Database.BeginTransactionAsync();
            await DbSeeder.XoaHoSoAsync(_db);
            DbSeeder.SeedDemo(_db);
            await tran.CommitAsync();

            TempData["ThongBao"] = "Đã nạp dữ liệu demo (16 đề nghị mẫu). Các đề nghị trước đó đã bị xóa.";
            return RedirectToAction("Index", "Home");
        }

        // Chỉ xóa đề nghị, không nạp lại
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaDeNghi()
        {
            await DbSeeder.XoaHoSoAsync(_db);
            TempData["ThongBao"] = "Đã xóa toàn bộ đề nghị.";
            return RedirectToAction("Index", "Home");
        }
    }
}