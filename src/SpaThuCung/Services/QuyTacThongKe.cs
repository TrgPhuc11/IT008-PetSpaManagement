using System.Globalization;

namespace SpaThuCung.Services;

/// <summary>Quy tắc chung của Thống kê doanh thu (UC18): giới hạn khoảng thời gian, cách chia kỳ, nhãn kỳ.</summary>
public static class QuyTacThongKe
{
    /// <summary>Xem theo ngày tối đa 93 ngày (khoảng một quý) để biểu đồ còn đọc được.</summary>
    public const int SoNgayToiDa = 93;

    /// <summary>Xem theo tháng tối đa 24 tháng.</summary>
    public const int SoThangToiDa = 24;

    /// <summary>Tên hiển thị cho phiếu đã thanh toán nhưng chưa có Groomer phụ trách.</summary>
    public const string ChuaPhanCong = "Chưa phân công";

    /// <summary>null nếu hợp lệ, ngược lại là câu báo lỗi.</summary>
    public static string? KiemTraKhoang(DateOnly tuNgay, DateOnly denNgay, CachXemThongKe cachXem)
    {
        if (denNgay < tuNgay) return "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.";

        if (cachXem == CachXemThongKe.TheoNgay)
        {
            var soNgay = denNgay.DayNumber - tuNgay.DayNumber + 1;
            if (soNgay > SoNgayToiDa)
                return $"Xem theo ngày tối đa {SoNgayToiDa} ngày; khoảng dài hơn hãy chọn xem theo tháng.";
        }
        else if (SoThang(tuNgay, denNgay) > SoThangToiDa)
        {
            return $"Xem theo tháng tối đa {SoThangToiDa} tháng.";
        }

        return null;
    }

    /// <summary>Số tháng (tính cả tháng đầu và tháng cuối) mà khoảng ngày chạm tới.</summary>
    public static int SoThang(DateOnly tuNgay, DateOnly denNgay) =>
        (denNgay.Year - tuNgay.Year) * 12 + denNgay.Month - tuNgay.Month + 1;

    /// <summary>Ngày đầu kỳ chứa một ngày.</summary>
    public static DateOnly DauKy(DateOnly ngay, CachXemThongKe cachXem) =>
        cachXem == CachXemThongKe.TheoNgay ? ngay : new DateOnly(ngay.Year, ngay.Month, 1);

    /// <summary>Các kỳ liên tiếp phủ khoảng ngày (kể cả kỳ không có phiếu), theo thứ tự thời gian.</summary>
    public static List<DateOnly> CacKy(DateOnly tuNgay, DateOnly denNgay, CachXemThongKe cachXem)
    {
        var ds = new List<DateOnly>();
        for (var ky = DauKy(tuNgay, cachXem); ky <= denNgay;
             ky = cachXem == CachXemThongKe.TheoNgay ? ky.AddDays(1) : ky.AddMonths(1))
        {
            ds.Add(ky);
        }
        return ds;
    }

    public static string NhanKy(DateOnly dauKy, CachXemThongKe cachXem) =>
        dauKy.ToString(cachXem == CachXemThongKe.TheoNgay ? "dd/MM" : "MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>Phiếu được tính vào doanh thu: đã thanh toán và không ở trạng thái Đã huỷ, Không đến (BR17).</summary>
    public static bool TinhDoanhThu(string trangThai, string ttThanhToan) =>
        ttThanhToan == Models.TrangThaiThanhToan.DaThanhToan
        && trangThai != Models.TrangThaiPhieu.DaHuy
        && trangThai != Models.TrangThaiPhieu.KhongDen;
}
