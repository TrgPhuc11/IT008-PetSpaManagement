namespace SpaThuCung.Models;

/// <summary>Các giá trị hợp lệ của cột trang_thai trong bảng PhieuDichVu.</summary>
public static class TrangThaiPhieu
{
    public const string DaDatLich = "Đã đặt lịch";
    public const string DaNhanThuCung = "Đã nhận thú cưng";
    public const string DangThucHien = "Đang thực hiện chăm sóc";
    public const string HoanTat = "Hoàn tất chăm sóc";
    public const string DaHuy = "Đã huỷ";
    public const string KhongDen = "Không đến";

    /// <summary>Theo thứ tự quy trình.</summary>
    public static readonly IReadOnlyList<string> TatCa = new[]
    {
        DaDatLich, DaNhanThuCung, DangThucHien, HoanTat, DaHuy, KhongDen,
    };
}
