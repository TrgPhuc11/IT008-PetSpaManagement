namespace SpaThuCung.Services;

/// <summary>
/// Dịch vụ và bảng giá (UC08, UC09). Mọi nhân viên được xem và tra giá;
/// chỉ Quản lý được thêm, sửa dịch vụ và chỉnh bảng giá (BR07).
/// </summary>
public interface IDichVuService
{
    Task<KetQua<List<DichVuDto>>> LayDanhSachAsync();

    /// <summary>Bảng giá của một dịch vụ, sắp theo loài rồi cân nặng.</summary>
    Task<KetQua<List<DongGiaDto>>> LayBangGiaAsync(int maDv);

    Task<KetQua<DichVuDto>> ThemAsync(string tenDv, string? moTa);

    Task<KetQua<DichVuDto>> CapNhatAsync(int maDv, string tenDv, string? moTa);

    /// <summary>Đảo Đang cung cấp và Ngừng cung cấp. Không có chức năng xoá dịch vụ.</summary>
    Task<KetQua<DichVuDto>> DoiTrangThaiAsync(int maDv);

    /// <summary>Thay toàn bộ bảng giá của dịch vụ bằng danh sách mới (dòng không còn trong danh sách bị xoá).</summary>
    Task<KetQua> LuuBangGiaAsync(int maDv, IReadOnlyList<DongGiaNhap> ds);

    /// <summary>Giá và thời lượng của các dịch vụ đang cung cấp cho một loài và cân nặng.</summary>
    Task<KetQua<List<GiaTraCuu>>> TraGiaAsync(string loai, decimal canNang);
}
