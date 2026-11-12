using System.Globalization;
using SpaThuCung.Models;

namespace SpaThuCung.Services;

/// <summary>Các hàm thuần của phiếu dịch vụ (UC10, BR06, BR08, BR11, BR13), dùng chung cho Service và ViewModel.</summary>
public static class QuyTacPhieu
{
    /// <summary>Bước chọn giờ hẹn (phút).</summary>
    public const int BuocGioHen = 15;

    /// <summary>Đặt lịch được trước tối đa bấy nhiêu ngày.</summary>
    public const int SoNgayDatTruocToiDa = 60;

    public static string MaHienThi(int maPhieu) => $"PDV{maPhieu:0000}";

    /// <summary>
    /// Trạng thái chưa kết thúc. Phiếu đang hoạt động gồm các trạng thái này, cộng phiếu Hoàn tất mà thú cưng chưa được trả
    /// (gio_tra rỗng, thú cưng còn ở tiệm). Một thú cưng không có hai phiếu đang hoạt động (BR06).
    /// </summary>
    public static readonly string[] TrangThaiDangHoatDong =
    {
        TrangThaiPhieu.DaDatLich, TrangThaiPhieu.DaNhanThuCung, TrangThaiPhieu.DangThucHien,
    };

    /// <summary>
    /// Trạng thái đang giữ lồng. Phiếu giữ lồng gồm các trạng thái này, cộng phiếu Hoàn tất chưa trả thú cưng (BR13).
    /// </summary>
    public static readonly string[] TrangThaiGiuLong =
    {
        TrangThaiPhieu.DaNhanThuCung, TrangThaiPhieu.DangThucHien,
    };

    /// <summary>Phiếu không tính vào thời lượng của Groomer (BR11).</summary>
    public static bool KhongTinhThoiLuong(string trangThai) =>
        trangThai is TrangThaiPhieu.DaHuy or TrangThaiPhieu.KhongDen;

    /// <summary>Bỏ giây và phần nhỏ hơn giây (cột DATETIME, giờ hẹn theo phút).</summary>
    public static DateTime CatGiay(DateTime t) => new(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0, t.Kind);

    /// <summary>Lý do nhập tay bắt buộc, tối đa 255 ký tự (BR08).</summary>
    public static string? KiemTraNhapTay(decimal? donGia, int? thoiLuong, string? ghiChu)
    {
        if (donGia is null || donGia < 0 || decimal.Truncate(donGia.Value) != donGia || donGia > QuyTacDichVu.GiaToiDa)
            return "giá nhập tay phải là số nguyên không âm (VNĐ).";
        if (thoiLuong is null || thoiLuong <= 0 || thoiLuong > QuyTacDichVu.ThoiLuongToiDa)
            return $"thời lượng nhập tay phải từ 1 đến {QuyTacDichVu.ThoiLuongToiDa} phút.";
        if (string.IsNullOrWhiteSpace(ghiChu)) return "nhập tay giá phải ghi lý do (vượt cân, trường hợp đặc biệt...).";
        if (ghiChu.Trim().Length > 255) return "ghi chú tối đa 255 ký tự.";
        return null;
    }

    /// <summary>
    /// Các cảnh báo không chặn của BR11 cho một Groomer: tổng thời lượng trong ca (kể cả phiếu đang lập) vượt thời lượng ca,
    /// hoặc giờ kết thúc dự kiến vượt giờ kết thúc ca quá số phút được lố.
    /// </summary>
    public static List<string> CanhBaoQuaTai(CaLamViecDto ca, DateTime gioHen, int tongThoiLuong, GroomerCaDto? groomer)
    {
        var ds = new List<string>();

        if (groomer is not null && groomer.PhutDaNhan + tongThoiLuong > ca.ThoiLuongPhut)
            ds.Add($"{groomer.HoTen} đã nhận {groomer.PhutDaNhan} phút trong {ca.TenCa}, thêm phiếu này là "
                   + $"{groomer.PhutDaNhan + tongThoiLuong} phút, vượt thời lượng ca {ca.ThoiLuongPhut} phút.");

        var gioXong = gioHen.AddMinutes(tongThoiLuong);
        var gioLo = gioHen.Date + ca.GioLoToiDa;
        if (tongThoiLuong > 0 && gioXong > gioLo)
            ds.Add($"Giờ xong dự kiến {Gio(gioXong)} vượt quá {Gio(gioLo)} "
                   + $"(hết {ca.TenCa} {Gio(gioHen.Date + ca.GioKetThuc)}, được lố {ca.PhutVuotChoPhep} phút).");

        return ds;
    }

    /// <summary>Các mốc giờ hẹn chọn được trong ngày: trong các ca đang sử dụng, cách nhau 15 phút.</summary>
    public static List<TimeSpan> CacMocGio(IEnumerable<CaLamViecDto> cacCa)
    {
        var ds = new SortedSet<TimeSpan>();
        foreach (var ca in cacCa.Where(c => c.DangSuDung))
        {
            for (var t = ca.GioBatDau; t < ca.GioKetThuc; t += TimeSpan.FromMinutes(BuocGioHen))
                ds.Add(t);
        }
        return ds.ToList();
    }

    public static string Gio(DateTime t) => t.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string Gio(TimeSpan t) => t.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    /// <summary>Ví dụ "09:15 12/11/2026".</summary>
    public static string NgayGio(DateTime t) => t.ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture);
}
