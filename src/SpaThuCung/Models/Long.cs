namespace SpaThuCung.Models;

/// <summary>
/// Một lồng giữ thú cưng (bảng Long). Danh sách lồng khởi tạo sẵn, không có chức năng quản lý.
/// Lồng trống là lồng không thuộc phiếu nào đang giữ thú cưng (BR13).
/// </summary>
public class Long
{
    public int MaLong { get; set; }
    public string TenLong { get; set; } = string.Empty;

    /// <summary>Đặt lịch hoặc Walk-in (xem LoaiLong).</summary>
    public string LoaiLong { get; set; } = Models.LoaiLong.DatLich;

    public string? GhiChu { get; set; }
}
