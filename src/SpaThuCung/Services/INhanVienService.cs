namespace SpaThuCung.Services;

/// <summary>Quản lý nhân viên (chỉ vai trò Quản lý). Đặt lại mật khẩu nằm ở IAuthService.</summary>
public interface INhanVienService
{
    Task<KetQua<List<NhanVienDto>>> LayDanhSachAsync();

    Task<KetQua<NhanVienDto>> ThemAsync(NhanVienMoi du);

    Task<KetQua<NhanVienDto>> CapNhatAsync(int maNv, string hoTen, string? sdt, string vaiTro);

    /// <summary>Đảo Đang làm việc và Đã nghỉ việc. Không có chức năng xoá.</summary>
    Task<KetQua<NhanVienDto>> DoiTrangThaiAsync(int maNv);
}
