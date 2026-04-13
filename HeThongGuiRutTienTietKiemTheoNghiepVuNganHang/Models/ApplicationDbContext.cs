using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Models
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // New DbSets for banking system
        public DbSet<TaiKhoanNganHang> TaiKhoanNganHangs { get; set; }
        public DbSet<SoTietKiem> SoTietKiems { get; set; }
        public DbSet<GiaoDichTietKiem> GiaoDichTietKiems { get; set; }
        public DbSet<GiaoDichNganHang> GiaoDichNganHangs { get; set; }
        public DbSet<ThongBao> ThongBaos { get; set; }
        public DbSet<BaoCaoGiaoDichNgay> BaoCaoGiaoDichNgays { get; set; }
        public DbSet<GoiTietKiem> GoiTietKiems { get; set; }
        public DbSet<OTPVerification> OTPVerifications { get; set; }
        public DbSet<LichSuDangNhap> LichSuDangNhaps { get; set; }
        public DbSet<LichHen> LichHens { get; set; }
        public DbSet<ChiNhanh> ChiNhanhs { get; set; }
        public DbSet<LoaiDichVu> LoaiDichVus { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Role column with proper length
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasMaxLength(50)
                .IsRequired();

            // Configure computed column for NgayDaoHan in SoTietKiem
            modelBuilder.Entity<SoTietKiem>()
                .Property(e => e.NgayDaoHan)
                .HasComputedColumnSql("DATEADD(MONTH, [KyHan], [NgayMoSo])");

            // Configure unique indexes for banking entities
            modelBuilder.Entity<User>()
                .HasIndex(u => u.CCCD)
                .IsUnique();

            // Banking system relationships
            modelBuilder.Entity<TaiKhoanNganHang>()
                .HasOne(tkn => tkn.KhachHang)
                .WithMany(kh => kh.TaiKhoanNganHangs)
                .HasForeignKey(tkn => tkn.MaKH)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SoTietKiem>()
                .HasOne(stk => stk.TaiKhoanNganHang)
                .WithMany(tkn => tkn.SoTietKiems)
                .HasForeignKey(stk => stk.SoTaiKhoan)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GiaoDichTietKiem>()
                .HasOne(gd => gd.SoTietKiem)
                .WithMany(stk => stk.GiaoDichTietKiems)
                .HasForeignKey(gd => gd.MaSTK)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GiaoDichTietKiem>()
                .HasOne(gd => gd.NhanVien)
                .WithMany(nv => nv.GiaoDichTietKiems)
                .HasForeignKey(gd => gd.MaNV)
                .OnDelete(DeleteBehavior.Restrict); // Changed from SetNull to Restrict

            modelBuilder.Entity<GiaoDichNganHang>()
                .HasOne(gd => gd.TaiKhoanNganHang)
                .WithMany(tkn => tkn.GiaoDichNganHangs)
                .HasForeignKey(gd => gd.SoTaiKhoan)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GiaoDichNganHang>()
                .HasOne(gd => gd.NhanVien)
                .WithMany(nv => nv.GiaoDichNganHangs)
                .HasForeignKey(gd => gd.MaNV)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ThongBao>()
                .HasOne(tb => tb.KhachHang)
                .WithMany(kh => kh.ThongBaos)
                .HasForeignKey(tb => tb.MaKH)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure GoiTietKiem
            modelBuilder.Entity<GoiTietKiem>()
                .Property(g => g.TrangThai)
                .HasMaxLength(20);
        }
    }
}