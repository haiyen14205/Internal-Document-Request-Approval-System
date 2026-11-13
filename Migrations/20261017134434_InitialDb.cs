using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalRequest_Management.Migrations
{
    /// <inheritdoc />
    public partial class InitialDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChucDanh",
                columns: table => new
                {
                    MaChucDanh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenChucDanh = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChucDanh", x => x.MaChucDanh);
                });

            migrationBuilder.CreateTable(
                name: "LoaiDeNghi",
                columns: table => new
                {
                    MaLoai = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoai = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MaKyThuat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrangThaiSuDung = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiDeNghi", x => x.MaLoai);
                });

            migrationBuilder.CreateTable(
                name: "PhongBan",
                columns: table => new
                {
                    MaPhongBan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenPhongBan = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    MaPhongBanCha = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongBan", x => x.MaPhongBan);
                    table.ForeignKey(
                        name: "FK_PhongBan_PhongBan_MaPhongBanCha",
                        column: x => x.MaPhongBanCha,
                        principalTable: "PhongBan",
                        principalColumn: "MaPhongBan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TepDinhKem",
                columns: table => new
                {
                    MaTep = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenTep = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LoaiTep = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    KichThuoc = table.Column<int>(type: "int", nullable: true),
                    DuLieuTep = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    NgayTaiLen = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TepDinhKem", x => x.MaTep);
                });

            migrationBuilder.CreateTable(
                name: "VaiTro",
                columns: table => new
                {
                    MaVaiTro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenVaiTro = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaiTro", x => x.MaVaiTro);
                });

            migrationBuilder.CreateTable(
                name: "QuyTrinhPheDuyet",
                columns: table => new
                {
                    MaQuyTrinh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenQuyTrinh = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhienBan = table.Column<int>(type: "int", nullable: false),
                    TrangThaiApDung = table.Column<bool>(type: "bit", nullable: false),
                    MaLoai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuyTrinhPheDuyet", x => x.MaQuyTrinh);
                    table.ForeignKey(
                        name: "FK_QuyTrinhPheDuyet_LoaiDeNghi_MaLoai",
                        column: x => x.MaLoai,
                        principalTable: "LoaiDeNghi",
                        principalColumn: "MaLoai",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDung",
                columns: table => new
                {
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    MaPhongBan = table.Column<int>(type: "int", nullable: true),
                    MaChucDanh = table.Column<int>(type: "int", nullable: true),
                    MaNguoiQuanLy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung", x => x.MaNguoiDung);
                    table.ForeignKey(
                        name: "FK_NguoiDung_ChucDanh_MaChucDanh",
                        column: x => x.MaChucDanh,
                        principalTable: "ChucDanh",
                        principalColumn: "MaChucDanh",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NguoiDung_NguoiDung_MaNguoiQuanLy",
                        column: x => x.MaNguoiQuanLy,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NguoiDung_PhongBan_MaPhongBan",
                        column: x => x.MaPhongBan,
                        principalTable: "PhongBan",
                        principalColumn: "MaPhongBan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BuocQuyTrinh",
                columns: table => new
                {
                    MaBuoc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenBuoc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ThuTuBuoc = table.Column<int>(type: "int", nullable: false),
                    ThoiHanXuLyGio = table.Column<int>(type: "int", nullable: false),
                    DieuKienApDung = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MaQuyTrinh = table.Column<int>(type: "int", nullable: false),
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuocQuyTrinh", x => x.MaBuoc);
                    table.ForeignKey(
                        name: "FK_BuocQuyTrinh_NguoiDung_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuocQuyTrinh_QuyTrinhPheDuyet_MaQuyTrinh",
                        column: x => x.MaQuyTrinh,
                        principalTable: "QuyTrinhPheDuyet",
                        principalColumn: "MaQuyTrinh",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDungVaiTro",
                columns: table => new
                {
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false),
                    MaVaiTro = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDungVaiTro", x => new { x.MaNguoiDung, x.MaVaiTro });
                    table.ForeignKey(
                        name: "FK_NguoiDungVaiTro_NguoiDung_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NguoiDungVaiTro_VaiTro_MaVaiTro",
                        column: x => x.MaVaiTro,
                        principalTable: "VaiTro",
                        principalColumn: "MaVaiTro",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HoSoDeNghi",
                columns: table => new
                {
                    MaHoSo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoHoSo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayBatDau = table.Column<DateTime>(type: "date", nullable: true),
                    NgayKetThuc = table.Column<DateTime>(type: "date", nullable: true),
                    DiaDiem = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    KinhPhi = table.Column<double>(type: "float", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaLoai = table.Column<int>(type: "int", nullable: false),
                    MaQuyTrinh = table.Column<int>(type: "int", nullable: true),
                    MaBuoc = table.Column<int>(type: "int", nullable: true),
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoSoDeNghi", x => x.MaHoSo);
                    table.ForeignKey(
                        name: "FK_HoSoDeNghi_BuocQuyTrinh_MaBuoc",
                        column: x => x.MaBuoc,
                        principalTable: "BuocQuyTrinh",
                        principalColumn: "MaBuoc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoSoDeNghi_LoaiDeNghi_MaLoai",
                        column: x => x.MaLoai,
                        principalTable: "LoaiDeNghi",
                        principalColumn: "MaLoai",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoSoDeNghi_NguoiDung_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoSoDeNghi_QuyTrinhPheDuyet_MaQuyTrinh",
                        column: x => x.MaQuyTrinh,
                        principalTable: "QuyTrinhPheDuyet",
                        principalColumn: "MaQuyTrinh",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BuocXuLyHoSo",
                columns: table => new
                {
                    MaBuocXuLy = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThoiDiemNhan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HanXuLy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiDiemHoanTat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KetQua = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    YKien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThoiGianXuLyGio = table.Column<double>(type: "float", nullable: true),
                    QuaHan = table.Column<bool>(type: "bit", nullable: false),
                    MaHoSo = table.Column<int>(type: "int", nullable: false),
                    MaBuoc = table.Column<int>(type: "int", nullable: false),
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuocXuLyHoSo", x => x.MaBuocXuLy);
                    table.ForeignKey(
                        name: "FK_BuocXuLyHoSo_BuocQuyTrinh_MaBuoc",
                        column: x => x.MaBuoc,
                        principalTable: "BuocQuyTrinh",
                        principalColumn: "MaBuoc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuocXuLyHoSo_HoSoDeNghi_MaHoSo",
                        column: x => x.MaHoSo,
                        principalTable: "HoSoDeNghi",
                        principalColumn: "MaHoSo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuocXuLyHoSo_NguoiDung_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeNghiDinhKem",
                columns: table => new
                {
                    MaTep = table.Column<int>(type: "int", nullable: false),
                    MaHoSo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeNghiDinhKem", x => new { x.MaTep, x.MaHoSo });
                    table.ForeignKey(
                        name: "FK_DeNghiDinhKem_HoSoDeNghi_MaHoSo",
                        column: x => x.MaHoSo,
                        principalTable: "HoSoDeNghi",
                        principalColumn: "MaHoSo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeNghiDinhKem_TepDinhKem_MaTep",
                        column: x => x.MaTep,
                        principalTable: "TepDinhKem",
                        principalColumn: "MaTep",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuXuLy",
                columns: table => new
                {
                    MaDong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThoiDiem = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaHoSo = table.Column<int>(type: "int", nullable: false),
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuXuLy", x => x.MaDong);
                    table.ForeignKey(
                        name: "FK_LichSuXuLy_HoSoDeNghi_MaHoSo",
                        column: x => x.MaHoSo,
                        principalTable: "HoSoDeNghi",
                        principalColumn: "MaHoSo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichSuXuLy_NguoiDung_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuocQuyTrinh_MaNguoiDung",
                table: "BuocQuyTrinh",
                column: "MaNguoiDung");

            migrationBuilder.CreateIndex(
                name: "IX_BuocQuyTrinh_MaQuyTrinh",
                table: "BuocQuyTrinh",
                column: "MaQuyTrinh");

            migrationBuilder.CreateIndex(
                name: "IX_BuocXuLyHoSo_MaBuoc",
                table: "BuocXuLyHoSo",
                column: "MaBuoc");

            migrationBuilder.CreateIndex(
                name: "IX_BuocXuLyHoSo_MaHoSo",
                table: "BuocXuLyHoSo",
                column: "MaHoSo");

            migrationBuilder.CreateIndex(
                name: "IX_BuocXuLyHoSo_MaNguoiDung",
                table: "BuocXuLyHoSo",
                column: "MaNguoiDung");

            migrationBuilder.CreateIndex(
                name: "IX_DeNghiDinhKem_MaHoSo",
                table: "DeNghiDinhKem",
                column: "MaHoSo");

            migrationBuilder.CreateIndex(
                name: "IX_HoSoDeNghi_MaBuoc",
                table: "HoSoDeNghi",
                column: "MaBuoc");

            migrationBuilder.CreateIndex(
                name: "IX_HoSoDeNghi_MaLoai",
                table: "HoSoDeNghi",
                column: "MaLoai");

            migrationBuilder.CreateIndex(
                name: "IX_HoSoDeNghi_MaNguoiDung",
                table: "HoSoDeNghi",
                column: "MaNguoiDung");

            migrationBuilder.CreateIndex(
                name: "IX_HoSoDeNghi_MaQuyTrinh",
                table: "HoSoDeNghi",
                column: "MaQuyTrinh");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuXuLy_MaHoSo",
                table: "LichSuXuLy",
                column: "MaHoSo");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuXuLy_MaNguoiDung",
                table: "LichSuXuLy",
                column: "MaNguoiDung");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_MaChucDanh",
                table: "NguoiDung",
                column: "MaChucDanh");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_MaNguoiQuanLy",
                table: "NguoiDung",
                column: "MaNguoiQuanLy");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_MaPhongBan",
                table: "NguoiDung",
                column: "MaPhongBan");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDungVaiTro_MaVaiTro",
                table: "NguoiDungVaiTro",
                column: "MaVaiTro");

            migrationBuilder.CreateIndex(
                name: "IX_PhongBan_MaPhongBanCha",
                table: "PhongBan",
                column: "MaPhongBanCha");

            migrationBuilder.CreateIndex(
                name: "IX_QuyTrinhPheDuyet_MaLoai",
                table: "QuyTrinhPheDuyet",
                column: "MaLoai");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuocXuLyHoSo");

            migrationBuilder.DropTable(
                name: "DeNghiDinhKem");

            migrationBuilder.DropTable(
                name: "LichSuXuLy");

            migrationBuilder.DropTable(
                name: "NguoiDungVaiTro");

            migrationBuilder.DropTable(
                name: "TepDinhKem");

            migrationBuilder.DropTable(
                name: "HoSoDeNghi");

            migrationBuilder.DropTable(
                name: "VaiTro");

            migrationBuilder.DropTable(
                name: "BuocQuyTrinh");

            migrationBuilder.DropTable(
                name: "NguoiDung");

            migrationBuilder.DropTable(
                name: "QuyTrinhPheDuyet");

            migrationBuilder.DropTable(
                name: "ChucDanh");

            migrationBuilder.DropTable(
                name: "PhongBan");

            migrationBuilder.DropTable(
                name: "LoaiDeNghi");
        }
    }
}
