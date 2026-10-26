namespace SpaThuCung.Models;

/// <summary>Các giá trị hợp lệ của cột trang_thai trong bảng DichVu.</summary>
public static class TrangThaiDichVu
{
    public const string DangCungCap = "Đang cung cấp";
    public const string NgungCungCap = "Ngừng cung cấp";

    public static readonly IReadOnlyList<string> TatCa = new[] { DangCungCap, NgungCungCap };
}
