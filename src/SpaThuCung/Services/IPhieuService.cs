namespace SpaThuCung.Services;

/// <summary>
/// Phiếu dịch vụ (Module 4). Hiện có Lập phiếu đặt lịch, Walk-in (UC10, FR07) kèm chọn giờ hẹn theo ca
/// và phân công Groomer (UC12, UC13). Mọi nhân viên đã đăng nhập dùng được.
/// </summary>
public interface IPhieuService
{
    /// <summary>
    /// Ca đang sử dụng chứa giờ hẹn và các Groomer có lịch đúng ngày, đúng ca (BR09, BR10), kèm số phút đã nhận trong ca (BR11).
    /// Giờ hẹn ngoài mọi ca thì báo lỗi.
    /// </summary>
    Task<KetQua<CaCuaGioHenDto>> LayCaVaGroomerAsync(DateTime gioHen);

    /// <summary>Các lồng đang trống của một loại (Đặt lịch hoặc Walk-in), sắp theo tên (BR13).</summary>
    Task<KetQua<List<LongTrongDto>>> LayLongTrongAsync(string loaiLong);

    /// <summary>Mô tả phiếu đang hoạt động của thú cưng (BR06); DuLieu = null nếu không có.</summary>
    Task<KetQua<string?>> TimPhieuDangHoatDongAsync(int maThuCung);

    /// <summary>
    /// Lập phiếu trong một transaction (UC10). Đặt lịch: "Đã đặt lịch". Walk-in: "Đã nhận thú cưng", gán lồng Walk-in trống,
    /// ghi giờ nhận và nhật ký. Giá, thời lượng các dòng không nhập tay được tra lại từ bảng giá lúc lưu (BR08).
    /// Cân nặng lúc lập là cân nặng thực tế nên được cập nhật vào hồ sơ thú cưng.
    /// Các cảnh báo quá tải (BR11) không chặn nên ViewModel hỏi trước khi gọi.
    /// </summary>
    Task<KetQua<KetQuaLapPhieu>> LapPhieuAsync(PhieuMoi phieu);
}
