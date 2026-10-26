namespace SpaThuCung.Services;

/// <summary>
/// Dịch vụ và bảng giá (UC08, UC09). Mọi nhân viên được xem và tra giá;
/// chỉ Quản lý được thêm, sửa dịch vụ và chỉnh bảng giá (BR07).
/// </summary>
public interface IDichVuService
{
    /// <summary>Tất cả dịch vụ kèm bảng giá.</summary>
    Task<KetQua<List<DichVuDto>>> LayDanhSachAsync();

    /// <summary>
    /// Thêm hoặc sửa một dịch vụ (tên, mô tả, trạng thái) cùng bảng giá trong một lần lưu.
    /// Chỉ xoá đúng các dòng giá trong MaXoa; dòng không nhắc tới (ví dụ người khác vừa thêm) được giữ và vẫn kiểm tra chồng lấn.
    /// Trả về mã dịch vụ.
    /// </summary>
    Task<KetQua<int>> LuuAsync(DichVuLuu du);

    /// <summary>Xoá dịch vụ cùng bảng giá. Chỉ xoá được khi chưa có phiếu dịch vụ nào dùng dịch vụ này.</summary>
    Task<KetQua> XoaAsync(int maDv);

    /// <summary>Giá và thời lượng của các dịch vụ đang cung cấp cho một loài và cân nặng.</summary>
    Task<KetQua<List<GiaTraCuu>>> TraGiaAsync(string loai, decimal canNang);
}
