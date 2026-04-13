using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Migrations
{
    /// <inheritdoc />
    public partial class v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaKH = table.Column<int>(type: "int", nullable: true),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CCCD = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SDT = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgheNghiep = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DigitalPin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPinSetup = table.Column<bool>(type: "bit", nullable: false),
                    MaNV = table.Column<int>(type: "int", nullable: true),
                    HoTenNV = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ViTri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChiNhanhs",
                columns: table => new
                {
                    MaCN = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenChiNhanh = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GioLamViec = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrangThaiHD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiNhanhs", x => x.MaCN);
                });

            migrationBuilder.CreateTable(
                name: "GoiTietKiems",
                columns: table => new
                {
                    MaGoiTietKiem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenGoi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KyHanThang = table.Column<int>(type: "int", nullable: false),
                    LaiSuat = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SoTienToiThieu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HinhThucTraLai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaiTucTuDong = table.Column<bool>(type: "bit", nullable: false),
                    ChoPhepRutTruocHan = table.Column<bool>(type: "bit", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoiTietKiems", x => x.MaGoiTietKiem);
                });

            migrationBuilder.CreateTable(
                name: "LoaiDichVus",
                columns: table => new
                {
                    MaDV = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenDV = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ThoiGianUocTinh = table.Column<int>(type: "int", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ChoPhepDatLich = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiDichVus", x => x.MaDV);
                });

            migrationBuilder.CreateTable(
                name: "OTPVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OTPCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OTPVerifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaoCaoGiaoDichNgay",
                columns: table => new
                {
                    MaBaoCao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNV = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NgayBaoCao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LoaiBaoCao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TongSoTienThu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongSoTienChi = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongTienRong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongSoGiaoDich = table.Column<int>(type: "int", nullable: false),
                    SoGiaoDichGui = table.Column<int>(type: "int", nullable: false),
                    SoGiaoDichRut = table.Column<int>(type: "int", nullable: false),
                    SoGiaoDichMoSo = table.Column<int>(type: "int", nullable: false),
                    SoGiaoDichTatToan = table.Column<int>(type: "int", nullable: false),
                    NgayLap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaoCaoGiaoDichNgay", x => x.MaBaoCao);
                    table.ForeignKey(
                        name: "FK_BaoCaoGiaoDichNgay_AspNetUsers_MaNV",
                        column: x => x.MaNV,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LichSuDangNhaps",
                columns: table => new
                {
                    MaLichSu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDN = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LoaiNguoiDung = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TGDangNhap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TGDangXuat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SoLanDangNhapThatBai = table.Column<int>(type: "int", nullable: false),
                    DiaChiIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuDangNhaps", x => x.MaLichSu);
                    table.ForeignKey(
                        name: "FK_LichSuDangNhaps_AspNetUsers_MaDN",
                        column: x => x.MaDN,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoanNganHang",
                columns: table => new
                {
                    SoTaiKhoan = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    MaKH = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SoDu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayMoTaiKhoan = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoanNganHang", x => x.SoTaiKhoan);
                    table.ForeignKey(
                        name: "FK_TaiKhoanNganHang_AspNetUsers_MaKH",
                        column: x => x.MaKH,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    MaThongBao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKH = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NgayGui = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.MaThongBao);
                    table.ForeignKey(
                        name: "FK_ThongBao_AspNetUsers_MaKH",
                        column: x => x.MaKH,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LichHens",
                columns: table => new
                {
                    MaLichHen = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NgayGiaoDich = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KhungGio = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThoiGianTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GhiChuKH = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MaKH = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaDV = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaCN = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichHens", x => x.MaLichHen);
                    table.ForeignKey(
                        name: "FK_LichHens_AspNetUsers_MaKH",
                        column: x => x.MaKH,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LichHens_ChiNhanhs_MaCN",
                        column: x => x.MaCN,
                        principalTable: "ChiNhanhs",
                        principalColumn: "MaCN",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LichHens_LoaiDichVus_MaDV",
                        column: x => x.MaDV,
                        principalTable: "LoaiDichVus",
                        principalColumn: "MaDV",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiaoDichNganHang",
                columns: table => new
                {
                    MaGD = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoTaiKhoan = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    LoaiGD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayGD = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaNV = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TrangThaiGD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichNganHang", x => x.MaGD);
                    table.ForeignKey(
                        name: "FK_GiaoDichNganHang_AspNetUsers_MaNV",
                        column: x => x.MaNV,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiaoDichNganHang_TaiKhoanNganHang_SoTaiKhoan",
                        column: x => x.SoTaiKhoan,
                        principalTable: "TaiKhoanNganHang",
                        principalColumn: "SoTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoTietKiem",
                columns: table => new
                {
                    MaSTK = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoTaiKhoan = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    SoTienGui = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KyHan = table.Column<int>(type: "int", nullable: false),
                    LaiSuat = table.Column<double>(type: "float", nullable: false),
                    NgayMoSo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayDaoHan = table.Column<DateTime>(type: "datetime2", nullable: false, computedColumnSql: "DATEADD(MONTH, [KyHan], [NgayMoSo])"),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LoaiTaiTuc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoTietKiem", x => x.MaSTK);
                    table.ForeignKey(
                        name: "FK_SoTietKiem_TaiKhoanNganHang_SoTaiKhoan",
                        column: x => x.SoTaiKhoan,
                        principalTable: "TaiKhoanNganHang",
                        principalColumn: "SoTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiaoDichTietKiem",
                columns: table => new
                {
                    MaGD = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSTK = table.Column<int>(type: "int", nullable: true),
                    MaNV = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LoaiGD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayGD = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThaiGD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichTietKiem", x => x.MaGD);
                    table.ForeignKey(
                        name: "FK_GiaoDichTietKiem_AspNetUsers_MaNV",
                        column: x => x.MaNV,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiaoDichTietKiem_SoTietKiem_MaSTK",
                        column: x => x.MaSTK,
                        principalTable: "SoTietKiem",
                        principalColumn: "MaSTK",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CCCD",
                table: "AspNetUsers",
                column: "CCCD",
                unique: true,
                filter: "[CCCD] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoGiaoDichNgay_MaNV",
                table: "BaoCaoGiaoDichNgay",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichNganHang_MaNV",
                table: "GiaoDichNganHang",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichNganHang_SoTaiKhoan",
                table: "GiaoDichNganHang",
                column: "SoTaiKhoan");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichTietKiem_MaNV",
                table: "GiaoDichTietKiem",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichTietKiem_MaSTK",
                table: "GiaoDichTietKiem",
                column: "MaSTK");

            migrationBuilder.CreateIndex(
                name: "IX_LichHens_MaCN",
                table: "LichHens",
                column: "MaCN");

            migrationBuilder.CreateIndex(
                name: "IX_LichHens_MaDV",
                table: "LichHens",
                column: "MaDV");

            migrationBuilder.CreateIndex(
                name: "IX_LichHens_MaKH",
                table: "LichHens",
                column: "MaKH");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuDangNhaps_MaDN",
                table: "LichSuDangNhaps",
                column: "MaDN");

            migrationBuilder.CreateIndex(
                name: "IX_SoTietKiem_SoTaiKhoan",
                table: "SoTietKiem",
                column: "SoTaiKhoan");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanNganHang_MaKH",
                table: "TaiKhoanNganHang",
                column: "MaKH");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_MaKH",
                table: "ThongBao",
                column: "MaKH");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BaoCaoGiaoDichNgay");

            migrationBuilder.DropTable(
                name: "GiaoDichNganHang");

            migrationBuilder.DropTable(
                name: "GiaoDichTietKiem");

            migrationBuilder.DropTable(
                name: "GoiTietKiems");

            migrationBuilder.DropTable(
                name: "LichHens");

            migrationBuilder.DropTable(
                name: "LichSuDangNhaps");

            migrationBuilder.DropTable(
                name: "OTPVerifications");

            migrationBuilder.DropTable(
                name: "ThongBao");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "SoTietKiem");

            migrationBuilder.DropTable(
                name: "ChiNhanhs");

            migrationBuilder.DropTable(
                name: "LoaiDichVus");

            migrationBuilder.DropTable(
                name: "TaiKhoanNganHang");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
