using System.Linq;
using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<PhongBan> PhongBans { get; set; }
        public DbSet<ChucDanh> ChucDanhs { get; set; }
        public DbSet<VaiTro> VaiTros { get; set; }
        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<NguoiDungVaiTro> NguoiDungVaiTros { get; set; }
        public DbSet<LoaiDeNghi> LoaiDeNghis { get; set; }
        public DbSet<QuyTrinhPheDuyet> QuyTrinhPheDuyets { get; set; }
        public DbSet<BuocQuyTrinh> BuocQuyTrinhs { get; set; }
        public DbSet<HoSoDeNghi> HoSoDeNghis { get; set; }
        public DbSet<BuocXuLyHoSo> BuocXuLyHoSos { get; set; }
        public DbSet<LichSuXuLy> LichSuXuLys { get; set; }
        public DbSet<TepDinhKem> TepDinhKems { get; set; }
        public DbSet<DeNghiDinhKem> DeNghiDinhKems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Khóa chính phức hợp cho 2 bảng trung gian
            modelBuilder.Entity<NguoiDungVaiTro>()
                .HasKey(x => new { x.MaNguoiDung, x.MaVaiTro });

            modelBuilder.Entity<DeNghiDinhKem>()
                .HasKey(x => new { x.MaTep, x.MaHoSo });

            // Tránh lỗi "multiple cascade paths" của SQL Server
            foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}