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
    public DbSet<CauHinh> CauHinh => Set<CauHinh>();
    public DbSet<CaLamViec> CaLamViec => Set<CaLamViec>();
    public DbSet<LichLamViec> LichLamViec => Set<LichLamViec>();

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

        modelBuilder.Entity<CauHinh>(e =>
        {
            e.ToTable("CauHinh");
            e.HasKey(x => x.MaCauHinh);
            e.Property(x => x.MaCauHinh).HasColumnName("ma_cau_hinh").HasMaxLength(50).IsUnicode(false);
            e.Property(x => x.GiaTri).HasColumnName("gia_tri").HasMaxLength(100).IsRequired();
            e.Property(x => x.MoTa).HasColumnName("mo_ta").HasMaxLength(255);
        });

        modelBuilder.Entity<CaLamViec>(e =>
        {
            e.ToTable("CaLamViec");
            e.HasKey(x => x.MaCa);
            e.Property(x => x.MaCa).HasColumnName("ma_ca").ValueGeneratedOnAdd();
            e.Property(x => x.TenCa).HasColumnName("ten_ca").HasMaxLength(30).IsRequired();
            e.Property(x => x.GioBatDau).HasColumnName("gio_bat_dau").HasColumnType("time(0)");
            e.Property(x => x.GioKetThuc).HasColumnName("gio_ket_thuc").HasColumnType("time(0)");
            e.Property(x => x.PhutVuotChoPhep).HasColumnName("phut_vuot_cho_phep");
            e.Property(x => x.SoLeTan).HasColumnName("so_le_tan");
            e.Property(x => x.SoGroomer).HasColumnName("so_groomer");
            e.Property(x => x.DangSuDung).HasColumnName("dang_su_dung");
            e.HasIndex(x => x.TenCa).IsUnique();
        });

        modelBuilder.Entity<LichLamViec>(e =>
        {
            e.ToTable("LichLamViec");
            e.HasKey(x => x.MaLich);
            e.Property(x => x.MaLich).HasColumnName("ma_lich").ValueGeneratedOnAdd();
            e.Property(x => x.MaNv).HasColumnName("ma_nv");
            e.Property(x => x.Ngay).HasColumnName("ngay").HasColumnType("date");
            e.Property(x => x.MaCa).HasColumnName("ma_ca");
            e.Property(x => x.ViTri).HasColumnName("vi_tri").HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.MaNv, x.Ngay }).IsUnique();
            e.HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.MaNv);
            e.HasOne(x => x.CaLamViec).WithMany().HasForeignKey(x => x.MaCa);
        });
    }
}
