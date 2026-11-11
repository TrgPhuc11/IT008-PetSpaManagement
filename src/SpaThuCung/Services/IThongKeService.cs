namespace SpaThuCung.Services;

/// <summary>
/// Thống kê doanh thu (UC18, FR12, chỉ Quản lý). Chỉ đọc dữ liệu; phiếu được lọc theo ngày hẹn (ngay_gio_hen).
/// </summary>
public interface IThongKeService
{
    /// <summary>
    /// Doanh thu trong khoảng [tuNgay, denNgay] (tính cả hai đầu), chia kỳ theo ngày hoặc tháng; kèm doanh thu theo dịch vụ,
    /// theo Groomer, số phiếu mỗi kỳ và số phiếu theo trạng thái. Không có phiếu đã thanh toán thì doanh thu bằng 0.
    /// </summary>
    Task<KetQua<ThongKeDoanhThuDto>> DoanhThuAsync(DateOnly tuNgay, DateOnly denNgay, CachXemThongKe cachXem);
}
