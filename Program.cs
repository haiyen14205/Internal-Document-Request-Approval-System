using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using InternalRequest_Management.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Kết nối SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<QuyTrinhService>();

builder.Services.AddHttpClient<GeminiService>(c => c.Timeout = TimeSpan.FromSeconds(40));

// Đăng nhập bằng Cookie (sẽ dùng ở bước sau)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Events = new CookieAuthenticationEvents
            {
                // Mỗi lần gửi yêu cầu, kiểm tra tài khoản còn tồn tại và chưa bị khóa
                OnValidatePrincipal = async ctx =>
                {
                    var idStr = ctx.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var db = ctx.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    bool conHoatDong = int.TryParse(idStr, out var id)
                        && await db.NguoiDungs.AnyAsync(u => u.MaNguoiDung == id && u.TrangThai);
                    if (!conHoatDong)
                    {
                        ctx.RejectPrincipal();
                        await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }
            };
        });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DbSeeder.Seed(db);
    DbSeeder.BoSungVaiTro(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();