namespace SpaThuCung.Services;

public interface IAuthService
{
    Task<KetQuaDangNhap> DangNhapAsync(string tenDangNhap, string matKhau);

    /// <summary>Quản lý đặt lại mật khẩu cho nhân viên đang làm việc.</summary>
    Task<KetQua> DatLaiMatKhauAsync(int maNv, string matKhauMoi);
}
