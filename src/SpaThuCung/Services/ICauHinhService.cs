namespace SpaThuCung.Services;

/// <summary>
/// Cấu hình hệ thống (UC19, FR14): danh sách ca làm việc (bảng CaLamViec) và các thông số vận hành (bảng CauHinh).
/// Mọi nhân viên đã đăng nhập được đọc (các chức năng khác cần giá trị cấu hình); chỉ Quản lý được chỉnh (BR03).
/// </summary>
public interface ICauHinhService
{
    /// <summary>Các thông số ở thẻ Thông số. Mã nào chưa có trong CSDL hoặc lỗi định dạng thì lấy giá trị mặc định.</summary>
    Task<KetQua<ThongSoDto>> LayThongSoAsync();

    /// <summary>Lưu cả thẻ Thông số trong một lần; có giá trị sai thì không lưu gì.</summary>
    Task<KetQua> CapNhatAsync(ThongSoDto thongSo);

    /// <summary>Giá trị số của một thông số (mã trong MaCauHinh), mặc định nếu chưa có.</summary>
    Task<KetQua<int>> LayGiaTriAsync(string maCauHinh);

    /// <summary>Đường dẫn đầy đủ của thư mục ảnh thú cưng.</summary>
    Task<KetQua<string>> LayThuMucAnhAsync();

    /// <summary>Tất cả ca (cả ca đã ngừng), sắp theo giờ bắt đầu.</summary>
    Task<KetQua<List<CaLamViecDto>>> LayDanhSachCaAsync();

    /// <summary>Thêm ca mới (đang sử dụng). Trả về mã ca.</summary>
    Task<KetQua<int>> ThemCaAsync(CaLamViecDto ca);

    /// <summary>Sửa tên, giờ, số phút được lố, số Lễ tân, số Groomer của ca. Trạng thái đổi bằng DoiTrangThaiCaAsync.</summary>
    Task<KetQua> SuaCaAsync(CaLamViecDto ca);

    /// <summary>Ngừng sử dụng hoặc dùng lại một ca. Luôn phải còn ít nhất một ca đang sử dụng.</summary>
    Task<KetQua> DoiTrangThaiCaAsync(int maCa, bool dangSuDung);

    /// <summary>Xoá ca chưa có lịch làm việc nào (ca đã có lịch chỉ ngừng sử dụng được). Luôn phải còn ít nhất một ca đang sử dụng.</summary>
    Task<KetQua> XoaCaAsync(int maCa);

    /// <summary>Ca đang sử dụng chứa giờ hẹn (giờ bắt đầu ≤ giờ &lt; giờ kết thúc); DuLieu = null nếu ngoài mọi ca.</summary>
    Task<KetQua<CaLamViecDto?>> XacDinhCaAsync(TimeSpan gioHen);
}
