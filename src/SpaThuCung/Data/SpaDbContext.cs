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
    public DbSet<KhachHang> KhachHang => Set<KhachHang>();
    public DbSet<ThuCung> ThuCung => Set<ThuCung>();
    public DbSet<PhieuDichVu> PhieuDichVu => Set<PhieuDichVu>();
    public DbSet<ChiTietPhieu> ChiTietPhieu => Set<ChiTietPhieu>();

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

        modelBuilder.Entity<KhachHang>(e =>
        {
            e.ToTable("KhachHang");
            e.HasKey(x => x.MaKh);
            e.Property(x => x.MaKh).HasColumnName("ma_kh").ValueGeneratedOnAdd();
            e.Property(x => x.HoTen).HasColumnName("ho_ten").HasMaxLength(100).IsRequired();
            e.Property(x => x.Sdt).HasColumnName("sdt").HasMaxLength(15).IsUnicode(false).IsRequired();
            e.Property(x => x.GhiChu).HasColumnName("ghi_chu").HasMaxLength(255);
            e.HasIndex(x => x.Sdt).IsUnique();
        });

        modelBuilder.Entity<ThuCung>(e =>
        {
            e.ToTable("ThuCung");
            e.HasKey(x => x.MaThuCung);
            e.Property(x => x.MaThuCung).HasColumnName("ma_thu_cung").ValueGeneratedOnAdd();
            e.Property(x => x.MaKh).HasColumnName("ma_kh");
            e.Property(x => x.Ten).HasColumnName("ten").HasMaxLength(50).IsRequired();
            e.Property(x => x.Loai).HasColumnName("loai").HasMaxLength(10).IsRequired();
            e.Property(x => x.CanNang).HasColumnName("can_nang").HasPrecision(5, 2);
            e.Property(x => x.DaTiemPhong).HasColumnName("da_tiem_phong");
            e.Property(x => x.GhiChu).HasColumnName("ghi_chu").HasMaxLength(255);
            e.HasOne(x => x.KhachHang).WithMany(x => x.ThuCung).HasForeignKey(x => x.MaKh);
        });

        // Long đã có bảng (script 05) nhưng chưa có entity: phiếu chỉ giữ mã lồng.
        modelBuilder.Entity<PhieuDichVu>(e =>
        {
            e.ToTable("PhieuDichVu");
            e.HasKey(x => x.MaPhieu);
            e.Property(x => x.MaPhieu).HasColumnName("ma_phieu").ValueGeneratedOnAdd();
            e.Property(x => x.MaThuCung).HasColumnName("ma_thu_cung");
            e.Property(x => x.MaGroomer).HasColumnName("ma_groomer");
            e.Property(x => x.MaNvLap).HasColumnName("ma_nv_lap");
            e.Property(x => x.MaLong).HasColumnName("ma_long");
            e.Property(x => x.NguonTiepNhan).HasColumnName("nguon_tiep_nhan").HasMaxLength(20).IsRequired();
            e.Property(x => x.NgayGioHen).HasColumnName("ngay_gio_hen").HasColumnType("datetime");
            e.Property(x => x.TrangThai).HasColumnName("trang_thai").HasMaxLength(30).IsRequired();
            e.Property(x => x.CanNangLucLap).HasColumnName("can_nang_luc_lap").HasPrecision(5, 2);
            e.Property(x => x.TongTien).HasColumnName("tong_tien").HasPrecision(12, 0);
            e.Property(x => x.TongThoiGian).HasColumnName("tong_thoi_gian");
            e.Property(x => x.TtThanhToan).HasColumnName("tt_thanh_toan").HasMaxLength(20).IsRequired();
            e.Property(x => x.PhuongThuc).HasColumnName("phuong_thuc").HasMaxLength(20);
            e.Property(x => x.LyDoHuy).HasColumnName("ly_do_huy").HasMaxLength(255);
            e.Property(x => x.GioNhan).HasColumnName("gio_nhan").HasColumnType("datetime");
            e.Property(x => x.GioBatDau).HasColumnName("gio_bat_dau").HasColumnType("datetime");
            e.Property(x => x.GioTra).HasColumnName("gio_tra").HasColumnType("datetime");
            e.Property(x => x.NgayTao).HasColumnName("ngay_tao").HasColumnType("datetime").ValueGeneratedOnAdd();
            e.HasOne(x => x.Groomer).WithMany().HasForeignKey(x => x.MaGroomer);
            e.HasOne(x => x.ThuCung).WithMany().HasForeignKey(x => x.MaThuCung);
        });

        modelBuilder.Entity<ChiTietPhieu>(e =>
        {
            e.ToTable("ChiTietPhieu");
            e.HasKey(x => new { x.MaPhieu, x.MaDv });
            e.Property(x => x.MaPhieu).HasColumnName("ma_phieu");
            e.Property(x => x.MaDv).HasColumnName("ma_dv");
            e.Property(x => x.DonGia).HasColumnName("don_gia").HasPrecision(12, 0);
            e.Property(x => x.ThoiLuong).HasColumnName("thoi_luong");
            e.Property(x => x.GhiChu).HasColumnName("ghi_chu").HasMaxLength(255);
            e.HasOne(x => x.PhieuDichVu).WithMany(x => x.ChiTiet).HasForeignKey(x => x.MaPhieu);
            e.HasOne(x => x.DichVu).WithMany().HasForeignKey(x => x.MaDv);
        });
    }
}
