using System.Globalization;
using SpaThuCung.Models;

namespace SpaThuCung.Services;

/// <summary>Các hàm thuần của chức năng Xếp lịch làm việc (FR13, BR02), dùng chung cho Service và ViewModel.</summary>
public static class QuyTacLichLamViec
{
    private static readonly string[] TenThu = { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };
    private static readonly string[] TenThuDayDu = { "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy", "Chủ nhật" };

    /// <summary>Số ngày tối đa của một lần xem lịch.</summary>
    public const int SoNgayToiDa = 62;

    public static DateOnly HomNay => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Thứ 2 của tuần chứa ngày đã cho.</summary>
    public static DateOnly NgayDauTuan(DateOnly ngay) => ngay.AddDays(-ViTriTrongTuan(ngay));

    /// <summary>0 = Thứ 2 ... 6 = Chủ nhật.</summary>
    public static int ViTriTrongTuan(DateOnly ngay) => ((int)ngay.DayOfWeek + 6) % 7;

    public static string Thu(DateOnly ngay) => TenThu[ViTriTrongTuan(ngay)];

    /// <summary>Ví dụ "Thứ Năm".</summary>
    public static string ThuDayDu(DateOnly ngay) => TenThuDayDu[ViTriTrongTuan(ngay)];

    public static string NgayNgan(DateOnly ngay) => ngay.ToString("dd/MM", CultureInfo.InvariantCulture);

    /// <summary>Ví dụ "T3 03/11".</summary>
    public static string ThuVaNgay(DateOnly ngay) => $"{Thu(ngay)} {NgayNgan(ngay)}";

    /// <summary>Ví dụ "Tuần 02/11 – 08/11/2026".</summary>
    public static string NhanTuan(DateOnly ngayDauTuan)
    {
        var cuoi = ngayDauTuan.AddDays(6);
        return $"Tuần {NgayNgan(ngayDauTuan)} – {cuoi.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}";
    }

    /// <summary>Ngày đã qua thì không xếp và không xoá lịch (giữ lịch sử phân công). Hôm nay vẫn sửa được.</summary>
    public static bool DaQua(DateOnly ngay) => ngay < HomNay;

    public static string? KiemTraViTri(string? viTri) =>
        ViTri.TatCa.Contains(viTri ?? string.Empty) ? null : "Vị trí chỉ được là Lễ tân hoặc Groomer.";

    /// <summary>Số người cấu hình cho một vị trí trong ca.</summary>
    public static int DinhMuc(CaLamViecDto ca, string viTri) => viTri == ViTri.LeTan ? ca.SoLeTan : ca.SoGroomer;

    /// <summary>Danh sách ngày dạng "T2, T4".</summary>
    public static string NoiNgay(IEnumerable<DateOnly> cacNgay) =>
        string.Join(", ", cacNgay.OrderBy(x => x).Select(ThuVaNgay));
}
