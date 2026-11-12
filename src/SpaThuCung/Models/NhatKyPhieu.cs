namespace SpaThuCung.Models;

/// <summary>
/// Một dòng nhật ký phiếu (bảng NhatKyPhieu): ai làm, lúc nào, thay đổi gì (BR15).
/// MaNv rỗng khi hệ thống tự chuyển Không đến.
/// </summary>
public class NhatKyPhieu
{
    public int MaLog { get; set; }
    public int MaPhieu { get; set; }
    public int? MaNv { get; set; }
    public DateTime ThoiDiem { get; set; }

    /// <summary>Loại thay đổi (xem HanhDongNhatKy).</summary>
    public string HanhDong { get; set; } = string.Empty;

    /// <summary>Giá trị cũ → giá trị mới.</summary>
    public string NoiDung { get; set; } = string.Empty;

    public PhieuDichVu? PhieuDichVu { get; set; }
    public NhanVien? NhanVien { get; set; }
}

/// <summary>Các giá trị hợp lệ của cột hanh_dong trong NhatKyPhieu (Chương IV mục 3).</summary>
public static class HanhDongNhatKy
{
    public const string DoiDichVu = "Đổi dịch vụ";
    public const string DoiGioHen = "Đổi giờ hẹn";
    public const string DoiDoKhachBaoTre = "Dời do khách báo trễ";
    public const string DoiGroomer = "Đổi Groomer";
    public const string SuaCanNang = "Sửa cân nặng";
    public const string NhapTayGia = "Nhập tay giá";
    public const string NhanThuCung = "Nhận thú cưng";
    public const string DoiLong = "Đổi lồng";
    public const string TraThuCung = "Trả thú cưng";
    public const string KhongDen = "Không đến";
    public const string ThemAnh = "Thêm ảnh";
    public const string XoaAnh = "Xoá ảnh";

    public static readonly IReadOnlyList<string> TatCa = new[]
    {
        DoiDichVu, DoiGioHen, DoiDoKhachBaoTre, DoiGroomer, SuaCanNang, NhapTayGia,
        NhanThuCung, DoiLong, TraThuCung, KhongDen, ThemAnh, XoaAnh,
    };
}
