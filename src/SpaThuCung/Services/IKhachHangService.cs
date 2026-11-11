namespace SpaThuCung.Services;

/// <summary>
/// Khách hàng và thú cưng (UC06, UC07, FR03, FR04). Mọi nhân viên đã đăng nhập dùng được.
/// Không có chức năng xoá: hồ sơ giữ lại để tra lịch sử phiếu (NFR03).
/// </summary>
public interface IKhachHangService
{
    /// <summary>Tất cả khách, kèm số thú cưng, số lần không đến và cờ cảnh báo (BR05).</summary>
    Task<KetQua<List<KhachHangDto>>> LayDanhSachKhachAsync();

    /// <summary>Thêm khách. SĐT đã có thì không tạo mới, trả về hồ sơ cũ (DaCoHoSo = true).</summary>
    Task<KetQua<KetQuaThemKhach>> ThemKhachAsync(KhachHangNhap khach);

    /// <summary>Sửa họ tên, SĐT, ghi chú. SĐT mới trùng khách khác thì báo lỗi.</summary>
    Task<KetQua> SuaKhachAsync(int maKh, KhachHangNhap khach);

    /// <summary>Thú cưng của một khách (maKh khác null) hoặc của mọi khách.</summary>
    Task<KetQua<List<ThuCungDto>>> LayDanhSachThuCungAsync(int? maKh = null);

    /// <summary>Thêm thú cưng cho khách; tick tiêm phòng được ngay khi tạo (BR06). Trả về mã thú cưng.</summary>
    Task<KetQua<int>> ThemThuCungAsync(ThuCungNhap thuCung);

    /// <summary>Sửa tên, loài, cân nặng, tiêm phòng, ghi chú. Không đổi chủ.</summary>
    Task<KetQua> SuaThuCungAsync(int maThuCung, ThuCungNhap thuCung);
}
