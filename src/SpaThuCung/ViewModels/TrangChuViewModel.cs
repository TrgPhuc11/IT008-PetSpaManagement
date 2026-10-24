using System.Globalization;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Trang chủ giữ chỗ.</summary>
public class TrangChuViewModel
{
    public TrangChuViewModel(IPhienLamViec phien)
    {
        LoiChao = $"Xin chào, {phien.NhanVienHienTai?.HoTen}";

        // Ví dụ "Thứ bảy, 24/10/2026".
        var ngay = DateTime.Now.ToString("dddd, dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
        NgayHomNay = ngay.Length > 0 ? char.ToUpper(ngay[0], CultureInfo.GetCultureInfo("vi-VN")) + ngay[1..] : ngay;
    }

    public string LoiChao { get; }

    public string NgayHomNay { get; }
}
