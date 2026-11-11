namespace SpaThuCung.Models;

/// <summary>Thú cưng của một khách hàng (bảng ThuCung).</summary>
public class ThuCung
{
    public int MaThuCung { get; set; }
    public int MaKh { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string Loai { get; set; } = LoaiThuCung.Cho;
    public decimal CanNang { get; set; }

    /// <summary>Nhân viên tick xác nhận đã tiêm phòng; chưa tiêm phòng thì không lập được phiếu (BR06).</summary>
    public bool DaTiemPhong { get; set; }

    public string? GhiChu { get; set; }

    public KhachHang? KhachHang { get; set; }
}
