using Microsoft.EntityFrameworkCore;
using InternalRequest_Management.Data;
using InternalRequest_Management.Models;

namespace InternalRequest_Management.Services
{
    // Xử lý luồng chuyển bước. Hàm này chỉ thêm dữ liệu vào context,
    // việc SaveChanges do controller gọi để mọi thay đổi nằm trong một lần lưu.
    public class QuyTrinhService
    {
        private readonly ApplicationDbContext _db;

        public QuyTrinhService(ApplicationDbContext db)
        {
            _db = db;
        }

        public void GhiLichSu(HoSoDeNghi h, int maNguoi, string noiDung)
        {
            h.LichSuXuLys.Add(new LichSuXuLy
            {
                MaNguoiDung = maNguoi,
                NoiDung = noiDung,
                ThoiDiem = DateTime.Now
            });
        }

        // Mở một dòng xử lý mới cho bước, hạn = giờ nhận + số giờ quy định
        public void MoBuoc(HoSoDeNghi h, BuocQuyTrinh buoc, int maNguoiXuLy)
        {
            var now = DateTime.Now;
            h.MaBuoc = buoc.MaBuoc;
            h.BuocXuLyHoSos.Add(new BuocXuLyHoSo
            {
                MaBuoc = buoc.MaBuoc,
                MaNguoiDung = maNguoiXuLy,
                ThoiDiemNhan = now,
                HanXuLy = now.AddHours(buoc.ThoiHanXuLyGio)
            });
        }

        // Gửi đề nghị lần đầu (từ Nháp). Trả về null nếu thành công, ngược lại trả về thông báo lỗi.
        public async Task<string?> GuiAsync(HoSoDeNghi h, int maNguoiThaoTac)
        {
            var loai = await _db.LoaiDeNghis.FindAsync(h.MaLoai);
            if (loai == null || !loai.TrangThaiSuDung)
                return "Loại đề nghị này đã ngừng sử dụng, không thể gửi.";

            var qt = await _db.QuyTrinhPheDuyets
                .Include(q => q.BuocQuyTrinhs).ThenInclude(b => b.NguoiXuLy)
                .Where(q => q.MaLoai == h.MaLoai && q.TrangThaiApDung)
                .OrderByDescending(q => q.PhienBan)
                .FirstOrDefaultAsync();

            if (qt == null)
                return "Loại đề nghị này chưa có quy trình phê duyệt đang áp dụng. Vui lòng liên hệ quản trị viên.";

            // Tìm bước đầu tiên có người xử lý khác người đề nghị
            BuocQuyTrinh? dau = null;
            foreach (var b in qt.BuocQuyTrinhs.OrderBy(b => b.ThuTuBuoc))
            {
                if (b.MaNguoiDung == h.MaNguoiDung)
                {
                    GhiLichSu(h, maNguoiThaoTac, $"Bỏ qua bước \"{b.TenBuoc}\" vì người xử lý trùng người đề nghị");
                    continue;
                }
                dau = b;
                break;
            }

            if (dau == null)
                return "Không có người phê duyệt phù hợp trong quy trình (các bước đều do chính bạn xử lý). Vui lòng liên hệ quản trị viên.";

            h.MaQuyTrinh = qt.MaQuyTrinh;
            h.TrangThai = TrangThaiHoSo.ChoDuyet;
            MoBuoc(h, dau, dau.MaNguoiDung);
            GhiLichSu(h, maNguoiThaoTac, $"Gửi đề nghị, chuyển đến bước \"{dau.TenBuoc}\" ({dau.NguoiXuLy?.HoTen})");
            return null;
        }

        // Gửi lại sau khi chỉnh sửa: quay về đúng bước và đúng người đã yêu cầu chỉnh sửa
        public async Task<string?> GuiLaiAsync(HoSoDeNghi h, int maNguoiThaoTac)
        {
            var cu = await _db.BuocXuLyHoSos
                .Where(x => x.MaHoSo == h.MaHoSo && x.KetQua == KetQuaXuLy.YeuCauChinhSua)
                .OrderByDescending(x => x.ThoiDiemHoanTat)
                .FirstOrDefaultAsync();
            if (cu == null)
                return "Không xác định được bước cần quay lại.";

            var buoc = await _db.BuocQuyTrinhs.FindAsync(cu.MaBuoc);
            if (buoc == null)
                return "Bước xử lý không còn tồn tại.";

            h.TrangThai = TrangThaiHoSo.ChoDuyet;
            MoBuoc(h, buoc, cu.MaNguoiDung);
            GhiLichSu(h, maNguoiThaoTac, $"Cập nhật và gửi lại, chuyển về bước \"{buoc.TenBuoc}\"");
            return null;
        }

        // Xử lý một bước: hanhDong = "duyet" | "tuchoi" | "chinhsua". Trả về null nếu thành công, ngược lại là thông báo lỗi.
        public async Task<string?> XuLyAsync(HoSoDeNghi h, int maNguoiXuLy, string hanhDong, string? yKien)
        {
            if (h.MaNguoiDung == maNguoiXuLy)
                return "Bạn không thể xử lý đề nghị do chính mình tạo.";

            var buocMo = h.BuocXuLyHoSos
                .Where(b => b.ThoiDiemHoanTat == null)
                .OrderByDescending(b => b.ThoiDiemNhan)
                .FirstOrDefault();

            if (h.TrangThai != TrangThaiHoSo.ChoDuyet || buocMo == null || buocMo.MaNguoiDung != maNguoiXuLy)
                return "Đề nghị không còn ở bước này (đã được xử lý, đã bị hủy hoặc chưa đến lượt bạn).";

            yKien = yKien?.Trim();
            if ((hanhDong == "tuchoi" || hanhDong == "chinhsua") && string.IsNullOrWhiteSpace(yKien))
                return hanhDong == "tuchoi"
                    ? "Vui lòng nhập lý do từ chối."
                    : "Vui lòng nhập nội dung cần chỉnh sửa.";

            var buoc = await _db.BuocQuyTrinhs.FindAsync(buocMo.MaBuoc);
            if (buoc == null) return "Bước xử lý không còn tồn tại.";

            // Đóng bước hiện tại
            var now = DateTime.Now;
            buocMo.ThoiDiemHoanTat = now;
            buocMo.ThoiGianXuLyGio = Math.Round((now - buocMo.ThoiDiemNhan).TotalHours, 2);
            buocMo.QuaHan = now > buocMo.HanXuLy;
            buocMo.YKien = yKien;

            string ghiChuY = string.IsNullOrWhiteSpace(yKien) ? "" : $". Ý kiến: {yKien}";

            if (hanhDong == "tuchoi")
            {
                buocMo.KetQua = KetQuaXuLy.TuChoi;
                h.TrangThai = TrangThaiHoSo.DaTuChoi;
                h.MaBuoc = null;
                GhiLichSu(h, maNguoiXuLy, $"Từ chối đề nghị tại bước \"{buoc.TenBuoc}\"{ghiChuY}");
                return null;
            }

            if (hanhDong == "chinhsua")
            {
                buocMo.KetQua = KetQuaXuLy.YeuCauChinhSua;
                h.TrangThai = TrangThaiHoSo.ChoChinhSua; // giữ nguyên bước hiện tại để gửi lại đúng chỗ
                GhiLichSu(h, maNguoiXuLy, $"Yêu cầu chỉnh sửa tại bước \"{buoc.TenBuoc}\"{ghiChuY}");
                return null;
            }

            // Phê duyệt: chuyển sang bước kế hoặc hoàn tất
            buocMo.KetQua = KetQuaXuLy.PheDuyet;
            GhiLichSu(h, maNguoiXuLy, $"Phê duyệt bước \"{buoc.TenBuoc}\"{ghiChuY}");

            var cacBuocSau = await _db.BuocQuyTrinhs
                .Include(b => b.NguoiXuLy)
                .Where(b => b.MaQuyTrinh == buoc.MaQuyTrinh && b.ThuTuBuoc > buoc.ThuTuBuoc)
                .OrderBy(b => b.ThuTuBuoc)
                .ToListAsync();

            BuocQuyTrinh? tiep = null;
            foreach (var b in cacBuocSau)
            {
                if (b.MaNguoiDung == h.MaNguoiDung)
                {
                    GhiLichSu(h, maNguoiXuLy, $"Bỏ qua bước \"{b.TenBuoc}\" vì người xử lý trùng người đề nghị");
                    continue;
                }
                tiep = b;
                break;
            }

            if (tiep != null)
            {
                MoBuoc(h, tiep, tiep.MaNguoiDung);
                GhiLichSu(h, maNguoiXuLy, $"Chuyển đến bước \"{tiep.TenBuoc}\" ({tiep.NguoiXuLy?.HoTen})");
            }
            else
            {
                h.TrangThai = TrangThaiHoSo.DaDuyet;
                h.MaBuoc = null;
                GhiLichSu(h, maNguoiXuLy, "Đề nghị đã được phê duyệt hoàn tất");
            }
            return null;
        }
    }
}