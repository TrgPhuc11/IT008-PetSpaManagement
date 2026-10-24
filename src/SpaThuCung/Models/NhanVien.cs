namespace SpaThuCung.Models;

/// <summary>Nhân viên của spa (cũng là tài khoản đăng nhập).</summary>
public class NhanVien
{
    public int MaNv { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? Sdt { get; set; }
    public string VaiTro { get; set; } = Models.VaiTro.NhanVien;
    public string TrangThaiLamViec { get; set; } = Models.TrangThaiLamViec.DangLamViec;
    public string TenDangNhap { get; set; } = string.Empty;

    /// <summary>Mật khẩu đã băm BCrypt.</summary>
    public string MatKhau { get; set; } = string.Empty;
}
