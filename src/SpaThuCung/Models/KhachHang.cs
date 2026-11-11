namespace SpaThuCung.Models;

/// <summary>Hồ sơ khách hàng (bảng KhachHang). SĐT là duy nhất (BR04).</summary>
public class KhachHang
{
    public int MaKh { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Sdt { get; set; } = string.Empty;
    public string? GhiChu { get; set; }

    public List<ThuCung> ThuCung { get; set; } = new();
}
