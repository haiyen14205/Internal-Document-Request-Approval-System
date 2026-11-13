using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext db)
        {
            if (db.VaiTros.Any()) return; // đã có dữ liệu thì bỏ qua

            var hasher = new PasswordHasher<NguoiDung>();
            const string matKhauMau = "123";

            // 1. Vai trò
            var vtDeNghi = new VaiTro { TenVaiTro = TenVaiTro.NguoiDeNghi };
            var vtPheDuyet = new VaiTro { TenVaiTro = TenVaiTro.NguoiPheDuyet };
            var vtQuanTri = new VaiTro { TenVaiTro = TenVaiTro.QuanTriVien };
            db.VaiTros.AddRange(vtDeNghi, vtPheDuyet, vtQuanTri);

            // 2. Phòng ban, chức danh
            var pbGiamDoc = new PhongBan { TenPhongBan = "Ban Giám đốc" };
            var pbKyThuat = new PhongBan { TenPhongBan = "Phòng Kỹ thuật", PhongBanCapTren = pbGiamDoc };
            var pbHanhChinh = new PhongBan { TenPhongBan = "Phòng Hành chính - Nhân sự", PhongBanCapTren = pbGiamDoc };
            db.PhongBans.AddRange(pbGiamDoc, pbKyThuat, pbHanhChinh);

            var cdNhanVien = new ChucDanh { TenChucDanh = "Nhân viên" };
            var cdTruongPhong = new ChucDanh { TenChucDanh = "Trưởng phòng" };
            var cdGiamDoc = new ChucDanh { TenChucDanh = "Giám đốc" };
            db.ChucDanhs.AddRange(cdNhanVien, cdTruongPhong, cdGiamDoc);

            // 3. Người dùng (mật khẩu chung: 123, được băm trước khi lưu)
            NguoiDung Tao(string tenDN, string hoTen, string email, PhongBan pb, ChucDanh cd,
              NguoiDung? quanLy, params VaiTro[] cacVaiTro)
            {
                var u = new NguoiDung
                {
                    TenDangNhap = tenDN,
                    HoTen = hoTen,
                    Email = email,
                    PhongBan = pb,
                    ChucDanh = cd,
                    NguoiQuanLy = quanLy,
                    TrangThai = true
                };
                u.MatKhau = hasher.HashPassword(u, matKhauMau);
                foreach (var vt in cacVaiTro)
                    u.NguoiDungVaiTros.Add(new NguoiDungVaiTro { VaiTro = vt });
                db.NguoiDungs.Add(u);
                return u;
            }

            var admin = Tao("admin", "Quản trị hệ thống", "admin@congty.vn", pbHanhChinh, cdTruongPhong, null, vtQuanTri);
            var giamDoc = Tao("giamdoc", "Giám đốc", "giamdoc@congty.vn", pbGiamDoc, cdGiamDoc, null, vtPheDuyet, vtDeNghi);
            var truongPhong = Tao("truongphong", "Trưởng phòng Kỹ thuật", "truongphong@congty.vn", pbKyThuat, cdTruongPhong, giamDoc, vtPheDuyet, vtDeNghi);
            var nhanVien = Tao("nhanvien", "Nhân viên A", "nhanvien@congty.vn", pbKyThuat, cdNhanVien, truongPhong, vtDeNghi);
            var nhanVien2 = Tao("nhanvien2", "Nhân viên B", "nhanvien2@congty.vn", pbKyThuat, cdNhanVien, truongPhong, vtDeNghi);

            // 4. Loại đề nghị
            var loaiNghiPhep = new LoaiDeNghi { TenLoai = "Nghỉ phép", MaKyThuat = "NGHI_PHEP" };
            var loaiCongTac = new LoaiDeNghi { TenLoai = "Công tác", MaKyThuat = "CONG_TAC" };
            var loaiHoatDong = new LoaiDeNghi { TenLoai = "Tổ chức hoạt động", MaKyThuat = "HOAT_DONG" };
            db.LoaiDeNghis.AddRange(loaiNghiPhep, loaiCongTac, loaiHoatDong);

            // 5. Quy trình phê duyệt mẫu
            var qtNghiPhep = new QuyTrinhPheDuyet { TenQuyTrinh = "Duyệt nghỉ phép (1 cấp)", LoaiDeNghi = loaiNghiPhep };
            qtNghiPhep.BuocQuyTrinhs.Add(new BuocQuyTrinh { TenBuoc = "Trưởng phòng duyệt", ThuTuBuoc = 1, ThoiHanXuLyGio = 24, NguoiXuLy = truongPhong });

            var qtCongTac = new QuyTrinhPheDuyet { TenQuyTrinh = "Duyệt công tác (2 cấp)", LoaiDeNghi = loaiCongTac };
            qtCongTac.BuocQuyTrinhs.Add(new BuocQuyTrinh { TenBuoc = "Trưởng phòng duyệt", ThuTuBuoc = 1, ThoiHanXuLyGio = 24, NguoiXuLy = truongPhong });
            qtCongTac.BuocQuyTrinhs.Add(new BuocQuyTrinh { TenBuoc = "Giám đốc duyệt", ThuTuBuoc = 2, ThoiHanXuLyGio = 48, NguoiXuLy = giamDoc });

            var qtHoatDong = new QuyTrinhPheDuyet { TenQuyTrinh = "Duyệt tổ chức hoạt động (2 cấp)", LoaiDeNghi = loaiHoatDong };
            qtHoatDong.BuocQuyTrinhs.Add(new BuocQuyTrinh { TenBuoc = "Trưởng phòng thẩm định", ThuTuBuoc = 1, ThoiHanXuLyGio = 24, NguoiXuLy = truongPhong });
            qtHoatDong.BuocQuyTrinhs.Add(new BuocQuyTrinh { TenBuoc = "Giám đốc duyệt kinh phí", ThuTuBuoc = 2, ThoiHanXuLyGio = 48, NguoiXuLy = giamDoc });

            db.QuyTrinhPheDuyets.AddRange(qtNghiPhep, qtCongTac, qtHoatDong);

            db.SaveChanges();
        }

        // Gán thêm vai trò "Người đề nghị" cho trưởng phòng và giám đốc (chạy an toàn nhiều lần)
        public static void BoSungVaiTro(ApplicationDbContext db)
        {
            var vtDeNghi = db.VaiTros.FirstOrDefault(v => v.TenVaiTro == TenVaiTro.NguoiDeNghi);
            if (vtDeNghi == null) return;

            foreach (var tenDN in new[] { "truongphong", "giamdoc" })
            {
                var u = db.NguoiDungs
                    .Include(x => x.NguoiDungVaiTros)
                    .FirstOrDefault(x => x.TenDangNhap == tenDN);

                if (u != null && !u.NguoiDungVaiTros.Any(r => r.MaVaiTro == vtDeNghi.MaVaiTro))
                    u.NguoiDungVaiTros.Add(new NguoiDungVaiTro { MaVaiTro = vtDeNghi.MaVaiTro });
            }
            db.SaveChanges();
        }

        // Xóa toàn bộ đề nghị và dữ liệu liên quan (giữ nguyên người dùng, loại, quy trình)
        public static async Task XoaHoSoAsync(ApplicationDbContext db)
        {
            await db.LichSuXuLys.ExecuteDeleteAsync();
            await db.BuocXuLyHoSos.ExecuteDeleteAsync();
            var tepIds = await db.DeNghiDinhKems.Select(x => x.MaTep).ToListAsync();
            await db.DeNghiDinhKems.ExecuteDeleteAsync();
            await db.TepDinhKems.Where(t => tepIds.Contains(t.MaTep)).ExecuteDeleteAsync();
            await db.HoSoDeNghis.ExecuteDeleteAsync();
        }

        // Nạp một loạt đề nghị mẫu để demo thống kê (chỉ chạy khi chưa có đề nghị nào)
        public static void SeedDemo(ApplicationDbContext db)
        {
            if (db.HoSoDeNghis.Any()) return;

            var users = db.NguoiDungs.ToDictionary(u => u.TenDangNhap);
            var dsQt = db.QuyTrinhPheDuyets
                .Include(q => q.LoaiDeNghi)
                .Include(q => q.BuocQuyTrinhs)
                .Where(q => q.TrangThaiApDung)
                .ToList();

            var now = DateTime.Now;
            var nam = now.Year;
            var demSo = new Dictionary<string, int>();

            string SinhSo(string maKT)
            {
                string tt = maKT switch { "NGHI_PHEP" => "NP", "CONG_TAC" => "CT", "HOAT_DONG" => "HD", _ => "DN" };
                demSo[tt] = demSo.GetValueOrDefault(tt) + 1;
                return $"{tt}-{nam}-{demSo[tt]:D4}";
            }

            (double, string, string?) Pd(double g) => (g, KetQuaXuLy.PheDuyet, null);
            (double, string, string?) Tc(double g, string y) => (g, KetQuaXuLy.TuChoi, y);
            (double, string, string?) Cs(double g, string y) => (g, KetQuaXuLy.YeuCauChinhSua, y);
            var khong = new (double, string, string?)[0];

            void Tao(string tenNguoi, string maKT, string trangThai, int ngayTruoc, string noiDung,
                     (double gio, string kq, string? yKien)[] daXong, double? moGio = null)
            {
                var qt = dsQt.First(q => q.LoaiDeNghi!.MaKyThuat == maKT);
                var cacBuoc = qt.BuocQuyTrinhs.OrderBy(b => b.ThuTuBuoc).ToList();
                var nd = users[tenNguoi];
                bool nghiPhep = maKT == "NGHI_PHEP";

                // Dựng mốc thời gian
                var nhan = new DateTime[daXong.Length];
                var xong = new DateTime[daXong.Length];
                DateTime tGui;
                if (moGio.HasValue)
                {
                    var cuoi = now.AddHours(-moGio.Value);
                    for (int i = daXong.Length - 1; i >= 0; i--)
                    {
                        xong[i] = cuoi;
                        nhan[i] = cuoi.AddHours(-daXong[i].gio);
                        cuoi = nhan[i];
                    }
                    tGui = cuoi;
                }
                else
                {
                    tGui = now.AddDays(-ngayTruoc).AddHours(1);
                    var t = tGui;
                    for (int i = 0; i < daXong.Length; i++)
                    {
                        nhan[i] = t;
                        xong[i] = t.AddHours(daXong[i].gio);
                        t = xong[i];
                    }
                }
                var ngayTao = trangThai == TrangThaiHoSo.Nhap ? now.AddDays(-ngayTruoc) : tGui.AddHours(-1);

                var h = new HoSoDeNghi
                {
                    SoHoSo = SinhSo(maKT),
                    MaLoai = qt.MaLoai,
                    MaNguoiDung = nd.MaNguoiDung,
                    TrangThai = trangThai,
                    NoiDung = noiDung,
                    NgayTao = ngayTao,
                    NgayBatDau = ngayTao.Date.AddDays(7),
                    NgayKetThuc = ngayTao.Date.AddDays(nghiPhep ? 8 : 9),
                    DiaDiem = nghiPhep ? null : (maKT == "CONG_TAC" ? "Đà Nẵng" : "Hội trường A"),
                    KinhPhi = nghiPhep ? null : (maKT == "CONG_TAC" ? 6000000 : 25000000)
                };
                void Ls(int maNguoi, DateTime tg, string nd2) =>
                    h.LichSuXuLys.Add(new LichSuXuLy { MaNguoiDung = maNguoi, ThoiDiem = tg, NoiDung = nd2 });

                Ls(nd.MaNguoiDung, ngayTao, "Tạo đề nghị (nháp)");

                if (trangThai == TrangThaiHoSo.Nhap)
                {
                    db.HoSoDeNghis.Add(h);
                    return;
                }

                h.MaQuyTrinh = qt.MaQuyTrinh;
                Ls(nd.MaNguoiDung, tGui, $"Gửi đề nghị, chuyển đến bước \"{cacBuoc[0].TenBuoc}\"");

                if (trangThai == TrangThaiHoSo.DaHuy)
                {
                    Ls(nd.MaNguoiDung, tGui.AddHours(2), "Hủy đề nghị");
                    db.HoSoDeNghis.Add(h);
                    return;
                }

                for (int i = 0; i < daXong.Length; i++)
                {
                    var b = cacBuoc[i];
                    var (gio, kq, yKien) = daXong[i];
                    h.BuocXuLyHoSos.Add(new BuocXuLyHoSo
                    {
                        MaBuoc = b.MaBuoc,
                        MaNguoiDung = b.MaNguoiDung,
                        ThoiDiemNhan = nhan[i],
                        HanXuLy = nhan[i].AddHours(b.ThoiHanXuLyGio),
                        ThoiDiemHoanTat = xong[i],
                        KetQua = kq,
                        YKien = yKien,
                        ThoiGianXuLyGio = gio,
                        QuaHan = gio > b.ThoiHanXuLyGio
                    });
                    string y = string.IsNullOrWhiteSpace(yKien) ? "" : $". Ý kiến: {yKien}";
                    string noiLs = kq == KetQuaXuLy.PheDuyet ? $"Phê duyệt bước \"{b.TenBuoc}\"{y}"
                                 : kq == KetQuaXuLy.TuChoi ? $"Từ chối đề nghị tại bước \"{b.TenBuoc}\"{y}"
                                 : $"Yêu cầu chỉnh sửa tại bước \"{b.TenBuoc}\"{y}";
                    Ls(b.MaNguoiDung, xong[i], noiLs);
                    if (kq == KetQuaXuLy.PheDuyet && i + 1 < cacBuoc.Count)
                        Ls(b.MaNguoiDung, xong[i], $"Chuyển đến bước \"{cacBuoc[i + 1].TenBuoc}\"");
                }

                switch (trangThai)
                {
                    case TrangThaiHoSo.DaDuyet:
                        h.MaBuoc = null;
                        Ls(cacBuoc[daXong.Length - 1].MaNguoiDung, xong[daXong.Length - 1], "Đề nghị đã được phê duyệt hoàn tất");
                        break;
                    case TrangThaiHoSo.DaTuChoi:
                        h.MaBuoc = null;
                        break;
                    case TrangThaiHoSo.ChoChinhSua:
                        h.MaBuoc = cacBuoc[daXong.Length - 1].MaBuoc;
                        break;
                    case TrangThaiHoSo.ChoDuyet:
                        var bm = cacBuoc[daXong.Length];
                        var nhanMo = now.AddHours(-moGio!.Value);
                        h.MaBuoc = bm.MaBuoc;
                        h.BuocXuLyHoSos.Add(new BuocXuLyHoSo
                        {
                            MaBuoc = bm.MaBuoc,
                            MaNguoiDung = bm.MaNguoiDung,
                            ThoiDiemNhan = nhanMo,
                            HanXuLy = nhanMo.AddHours(bm.ThoiHanXuLyGio)
                        });
                        break;
                }
                db.HoSoDeNghis.Add(h);
            }

            // ----- Nghỉ phép (1 bước: trưởng phòng, hạn 24 giờ) -----
            Tao("nhanvien", "NGHI_PHEP", TrangThaiHoSo.DaDuyet, 40, "Xin nghỉ phép việc gia đình", new[] { Pd(8) });
            Tao("nhanvien", "NGHI_PHEP", TrangThaiHoSo.DaDuyet, 35, "Xin nghỉ phép khám sức khỏe", new[] { Pd(30) });
            Tao("nhanvien2", "NGHI_PHEP", TrangThaiHoSo.DaDuyet, 30, "Xin nghỉ phép du lịch", new[] { Pd(5) });
            Tao("nhanvien2", "NGHI_PHEP", TrangThaiHoSo.DaTuChoi, 28, "Xin nghỉ phép dịp cao điểm", new[] { Tc(12, "Trùng đợt cao điểm của dự án") });
            Tao("nhanvien", "NGHI_PHEP", TrangThaiHoSo.ChoDuyet, 0, "Xin nghỉ phép đi hội thảo", khong, moGio: 3);
            Tao("nhanvien", "NGHI_PHEP", TrangThaiHoSo.Nhap, 2, "Nghỉ phép (chưa gửi)", khong);

            // ----- Công tác (2 bước: trưởng phòng 24h, giám đốc 48h) -----
            Tao("nhanvien", "CONG_TAC", TrangThaiHoSo.DaDuyet, 25, "Đi công tác gặp khách hàng", new[] { Pd(10), Pd(20) });
            Tao("nhanvien", "CONG_TAC", TrangThaiHoSo.DaDuyet, 22, "Công tác khảo sát địa điểm triển khai", new[] { Pd(28), Pd(60) });
            Tao("nhanvien2", "CONG_TAC", TrangThaiHoSo.DaDuyet, 18, "Công tác tham dự hội nghị", new[] { Pd(6), Pd(40) });
            Tao("nhanvien2", "CONG_TAC", TrangThaiHoSo.ChoDuyet, 0, "Công tác bàn giao thiết bị", khong, moGio: 30);
            Tao("nhanvien", "CONG_TAC", TrangThaiHoSo.ChoDuyet, 0, "Công tác huấn luyện chi nhánh", new[] { Pd(7) }, moGio: 52);
            Tao("nhanvien2", "CONG_TAC", TrangThaiHoSo.DaHuy, 10, "Công tác (đã hủy)", khong);

            // ----- Tổ chức hoạt động (2 bước) -----
            Tao("nhanvien", "HOAT_DONG", TrangThaiHoSo.DaDuyet, 15, "Tổ chức team building", new[] { Pd(14), Pd(55) });
            Tao("nhanvien2", "HOAT_DONG", TrangThaiHoSo.DaTuChoi, 12, "Tổ chức tiệc cuối năm", new[] { Pd(9), Tc(30, "Kinh phí vượt hạn mức cho phép") });
            Tao("nhanvien", "HOAT_DONG", TrangThaiHoSo.ChoChinhSua, 6, "Tổ chức hội thao nội bộ", new[] { Cs(16, "Bổ sung dự toán chi tiết") });
            Tao("nhanvien2", "HOAT_DONG", TrangThaiHoSo.ChoDuyet, 0, "Tổ chức buổi chia sẻ chuyên môn", new[] { Pd(10) }, moGio: 20);

            db.SaveChanges();
        }
    }
}