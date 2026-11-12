namespace SpaThuCung.Models;

/// <summary>
/// Ảnh thú cưng của một phiếu (bảng HinhAnhPhieu, FR15, BR18). Tệp nằm trong thư mục ảnh của ứng dụng,
/// bảng chỉ lưu đường dẫn tương đối, ví dụ 125\NHAN_01.jpg.
/// </summary>
public class HinhAnhPhieu
{
    public int MaAnh { get; set; }
    public int MaPhieu { get; set; }

    /// <summary>Lúc nhận hoặc Sau chăm sóc (xem LoaiAnh).</summary>
    public string LoaiAnh { get; set; } = Models.LoaiAnh.LucNhan;

    public string DuongDan { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
    public DateTime ThoiDiem { get; set; }

    public PhieuDichVu? PhieuDichVu { get; set; }
}

/// <summary>Các giá trị hợp lệ của cột loai_anh trong HinhAnhPhieu.</summary>
public static class LoaiAnh
{
    public const string LucNhan = "Lúc nhận";
    public const string SauChamSoc = "Sau chăm sóc";

    public static readonly IReadOnlyList<string> TatCa = new[] { LucNhan, SauChamSoc };
}
