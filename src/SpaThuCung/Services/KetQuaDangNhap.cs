using SpaThuCung.Models;

namespace SpaThuCung.Services;

/// <summary>Kết quả của một lần đăng nhập.</summary>
public sealed record KetQuaDangNhap(bool ThanhCong, string ThongBao, NhanVien? NhanVien)
{
    public static KetQuaDangNhap Ok(NhanVien nv) => new(true, string.Empty, nv);

    public static KetQuaDangNhap Loi(string thongBao) => new(false, thongBao, null);
}
