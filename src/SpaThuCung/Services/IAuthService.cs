namespace SpaThuCung.Services;

public interface IAuthService
{
    Task<KetQuaDangNhap> DangNhapAsync(string tenDangNhap, string matKhau);

    /// <summary>Người đang đăng nhập tự đổi mật khẩu của mình (UC02).</summary>
    Task<KetQua> DoiMatKhauAsync(string matKhauHienTai, string matKhauMoi);

    /// <summary>Quản lý đặt lại mật khẩu cho nhân viên đang làm việc.</summary>
    Task<KetQua> DatLaiMatKhauAsync(int maNv, string matKhauMoi);
}
