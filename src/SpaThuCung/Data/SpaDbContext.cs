using Microsoft.EntityFrameworkCore;
using SpaThuCung.Models;

namespace SpaThuCung.Data;

public class SpaDbContext : DbContext
{
    public SpaDbContext(DbContextOptions<SpaDbContext> options) : base(options)
    {
    }

    public DbSet<NhanVien> NhanVien => Set<NhanVien>();

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
    }
}
