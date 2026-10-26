using Microsoft.EntityFrameworkCore;
using SpaThuCung.Models;

namespace SpaThuCung.Data;

public class SpaDbContext : DbContext
{
    public SpaDbContext(DbContextOptions<SpaDbContext> options) : base(options)
    {
    }

    public DbSet<NhanVien> NhanVien => Set<NhanVien>();
    public DbSet<DichVu> DichVu => Set<DichVu>();
    public DbSet<BangGia> BangGia => Set<BangGia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NhanVien>(e =>
        {
            e.ToTable("NhanVien");
            e.HasKey(x => x.MaNv);
            e.Property(x => x.MaNv).HasColumnName("ma_nv").ValueGeneratedOnAdd();
            e.Property(x => x.HoTen).HasColumnName("ho_ten").HasMaxLength(100).IsRequired();
            e.Property(x => x.Sdt).HasColumnName("sdt").HasMaxLength(15).IsUnicode(false);
            e.Property(x => x.VaiTro).HasColumnName("vai_tro").HasMaxLength(20).IsRequired();
            e.Property(x => x.TrangThaiLamViec).HasColumnName("trang_thai_lam_viec").HasMaxLength(20).IsRequired();
            e.Property(x => x.TenDangNhap).HasColumnName("ten_dang_nhap").HasMaxLength(50).IsUnicode(false).IsRequired();
            e.Property(x => x.MatKhau).HasColumnName("mat_khau").HasMaxLength(255).IsUnicode(false).IsRequired();
            e.HasIndex(x => x.TenDangNhap).IsUnique();
        });

        modelBuilder.Entity<DichVu>(e =>
        {
            e.ToTable("DichVu");
            e.HasKey(x => x.MaDv);
            e.Property(x => x.MaDv).HasColumnName("ma_dv").ValueGeneratedOnAdd();
            e.Property(x => x.TenDv).HasColumnName("ten_dv").HasMaxLength(100).IsRequired();
            e.Property(x => x.MoTa).HasColumnName("mo_ta").HasMaxLength(255);
            e.Property(x => x.TrangThai).HasColumnName("trang_thai").HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.TenDv).IsUnique();
        });

        modelBuilder.Entity<BangGia>(e =>
        {
            e.ToTable("BangGia");
            e.HasKey(x => x.MaGia);
            e.Property(x => x.MaGia).HasColumnName("ma_gia").ValueGeneratedOnAdd();
            e.Property(x => x.MaDv).HasColumnName("ma_dv");
            e.Property(x => x.Loai).HasColumnName("loai").HasMaxLength(10).IsRequired();
            e.Property(x => x.CanNangTu).HasColumnName("can_nang_tu").HasPrecision(5, 2);
            e.Property(x => x.CanNangDen).HasColumnName("can_nang_den").HasPrecision(5, 2);
            e.Property(x => x.Gia).HasColumnName("gia").HasPrecision(12, 0);
            e.Property(x => x.ThoiLuong).HasColumnName("thoi_luong");
            e.HasOne(x => x.DichVu).WithMany(x => x.BangGia).HasForeignKey(x => x.MaDv);
        });
    }
}
