namespace SpaThuCung.Models;

/// <summary>Một dịch vụ trong phiếu (bảng ChiTietPhieu): đơn giá và thời lượng lưu tại thời điểm lập/nhận.</summary>
public class ChiTietPhieu
{
    public int MaPhieu { get; set; }
    public int MaDv { get; set; }
    public decimal DonGia { get; set; }
    public int ThoiLuong { get; set; }

    /// <summary>Lý do nhập tay giá, thời lượng (bắt buộc khi nhập tay).</summary>
    public string? GhiChu { get; set; }

    public PhieuDichVu? PhieuDichVu { get; set; }
    public DichVu? DichVu { get; set; }
}
