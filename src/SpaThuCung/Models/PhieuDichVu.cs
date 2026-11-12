namespace SpaThuCung.Models;

/// <summary>Phiếu dịch vụ cho một thú cưng (bảng PhieuDichVu, Chương IV mục 2.8).</summary>
public class PhieuDichVu
{
    public int MaPhieu { get; set; }
    public int MaThuCung { get; set; }

    /// <summary>Groomer phụ trách; null khi chưa phân công.</summary>
    public int? MaGroomer { get; set; }

    public int MaNvLap { get; set; }
    public int? MaLong { get; set; }
    public string NguonTiepNhan { get; set; } = string.Empty;
    public DateTime NgayGioHen { get; set; }
    public string TrangThai { get; set; } = TrangThaiPhieu.DaDatLich;
    public decimal CanNangLucLap { get; set; }
    public decimal TongTien { get; set; }
    public int TongThoiGian { get; set; }
    public string TtThanhToan { get; set; } = TrangThaiThanhToan.ChuaThanhToan;
    public string? PhuongThuc { get; set; }
    public string? LyDoHuy { get; set; }
    public DateTime? GioNhan { get; set; }
    public DateTime? GioBatDau { get; set; }
    public DateTime? GioTra { get; set; }
    public DateTime NgayTao { get; set; }

    public NhanVien? Groomer { get; set; }
    public NhanVien? NhanVienLap { get; set; }
    public ThuCung? ThuCung { get; set; }
    public Long? Long { get; set; }
    public List<ChiTietPhieu> ChiTiet { get; set; } = new();
    public List<NhatKyPhieu> NhatKy { get; set; } = new();
    public List<HinhAnhPhieu> HinhAnh { get; set; } = new();
}
