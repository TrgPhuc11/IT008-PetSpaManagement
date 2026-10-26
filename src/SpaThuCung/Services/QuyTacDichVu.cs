using System.Globalization;
using SpaThuCung.Models;

namespace SpaThuCung.Services;

/// <summary>Quy tắc dịch vụ và bảng giá (BR07). Hàm Kiem... trả null nếu hợp lệ, ngược lại trả thông báo lỗi.</summary>
public static class QuyTacDichVu
{
    /// <summary>Giới hạn của cột DECIMAL(5,2).</summary>
    public const decimal CanNangToiDa = 999.99m;

    /// <summary>Thời lượng một dịch vụ không thể quá một ngày.</summary>
    public const int ThoiLuongToiDa = 1440;

    /// <summary>Giới hạn của cột DECIMAL(12,0).</summary>
    public const decimal GiaToiDa = 999_999_999_999m;

    private static readonly CultureInfo Vn = CultureInfo.GetCultureInfo("vi-VN");

    public static string? KiemTraTen(string ten)
    {
        if (string.IsNullOrWhiteSpace(ten)) return "Nhập tên dịch vụ.";
        if (ten.Trim().Length > 100) return "Tên dịch vụ tối đa 100 ký tự.";
        return null;
    }

    public static string? KiemTraMoTa(string? moTa)
    {
        if (moTa is not null && moTa.Trim().Length > 255) return "Mô tả tối đa 255 ký tự.";
        return null;
    }

    /// <summary>Kiểm tra từng dòng rồi kiểm tra các khoảng cân nặng cùng loài không chồng lấn.</summary>
    public static string? KiemTraBangGia(IReadOnlyList<DongGiaNhap> ds)
    {
        for (var i = 0; i < ds.Count; i++)
        {
            var loi = KiemTraDong(ds[i]);
            if (loi is not null) return $"Dòng {i + 1}: {loi}";
        }

        for (var i = 0; i < ds.Count; i++)
        {
            for (var j = i + 1; j < ds.Count; j++)
            {
                var a = ds[i];
                var b = ds[j];
                if (a.Loai == b.Loai && ChongLan(a.CanNangTu, a.CanNangDen, b.CanNangTu, b.CanNangDen))
                    return $"Khoảng {Khoang(a.CanNangTu, a.CanNangDen)} và {Khoang(b.CanNangTu, b.CanNangDen)} của {a.Loai} bị chồng lấn.";
            }
        }
        return null;
    }

    private static string? KiemTraDong(DongGiaNhap d)
    {
        if (!LoaiThuCung.TatCa.Contains(d.Loai)) return "loài không hợp lệ.";
        if (d.CanNangTu < 0) return "cân nặng \"Từ\" không được âm.";
        if (d.CanNangDen <= d.CanNangTu) return "cân nặng \"Đến\" phải lớn hơn \"Từ\".";
        if (d.CanNangDen > CanNangToiDa) return $"cân nặng tối đa {KgHienThi(CanNangToiDa)} kg.";
        if (decimal.Round(d.CanNangTu, 2) != d.CanNangTu || decimal.Round(d.CanNangDen, 2) != d.CanNangDen)
            return "cân nặng có tối đa 2 chữ số sau dấu phẩy.";
        if (d.Gia < 0 || decimal.Truncate(d.Gia) != d.Gia || d.Gia > GiaToiDa) return "giá phải là số nguyên không âm.";
        if (d.ThoiLuong <= 0 || d.ThoiLuong > ThoiLuongToiDa) return $"thời lượng phải từ 1 đến {ThoiLuongToiDa} phút.";
        return null;
    }

    /// <summary>Khoảng tính từ trên "Từ" đến hết "Đến", nên 0–5 và 5–15 không chồng lấn.</summary>
    public static bool ChongLan(decimal tu1, decimal den1, decimal tu2, decimal den2) => tu1 < den2 && tu2 < den1;

    /// <summary>Dòng giá phù hợp với loài và cân nặng (CanNangTu &lt; cân nặng ≤ CanNangDen), null nếu không có.</summary>
    public static BangGia? TimDongGia(IEnumerable<BangGia> ds, string loai, decimal canNang) =>
        ds.FirstOrDefault(x => x.Loai == loai && x.CanNangTu < canNang && canNang <= x.CanNangDen);

    public static string KgHienThi(decimal kg) => kg.ToString("0.##", Vn);

    public static string Khoang(decimal tu, decimal den) => $"{KgHienThi(tu)}–{KgHienThi(den)} kg";

    public static string TienHienThi(decimal tien) => tien.ToString("N0", Vn);
}
