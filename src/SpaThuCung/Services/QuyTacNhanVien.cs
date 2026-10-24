using System.Text.RegularExpressions;
using SpaThuCung.Models;

namespace SpaThuCung.Services;

/// <summary>Quy tắc kiểm tra dữ liệu nhân viên. Mỗi hàm trả null nếu hợp lệ, ngược lại trả thông báo lỗi.</summary>
public static class QuyTacNhanVien
{
    public const int MatKhauToiThieu = 6;

    private static readonly Regex TenDangNhapHopLe = new("^[A-Za-z0-9._]{3,50}$", RegexOptions.Compiled);

    public static string? KiemTraHoTen(string hoTen)
    {
        if (string.IsNullOrWhiteSpace(hoTen)) return "Nhập họ tên.";
        if (hoTen.Trim().Length > 100) return "Họ tên tối đa 100 ký tự.";
        return null;
    }

    /// <summary>SĐT không bắt buộc; nếu có thì 9-15 chữ số (chỉ 0-9, không nhận chữ số Unicode khác).</summary>
    public static string? KiemTraSdt(string? sdt)
    {
        if (string.IsNullOrWhiteSpace(sdt)) return null;
        sdt = sdt.Trim();
        if (sdt.Length < 9 || sdt.Length > 15 || !sdt.All(c => c is >= '0' and <= '9'))
            return "SĐT chỉ gồm chữ số, từ 9 đến 15 số.";
        return null;
    }

    public static string? KiemTraTenDangNhap(string tenDangNhap)
    {
        if (string.IsNullOrWhiteSpace(tenDangNhap)) return "Nhập tên đăng nhập.";
        if (!TenDangNhapHopLe.IsMatch(tenDangNhap.Trim()))
            return "Tên đăng nhập gồm 3 đến 50 ký tự: chữ cái không dấu, số, dấu chấm hoặc gạch dưới.";
        return null;
    }

    public static string? KiemTraVaiTro(string vaiTro)
    {
        if (vaiTro != VaiTro.NhanVien && vaiTro != VaiTro.QuanLy) return "Vai trò không hợp lệ.";
        return null;
    }

    public static string? KiemTraMatKhau(string? matKhau)
    {
        if (string.IsNullOrEmpty(matKhau)) return "Nhập mật khẩu.";
        if (matKhau.Length < MatKhauToiThieu) return $"Mật khẩu phải có ít nhất {MatKhauToiThieu} ký tự.";
        return null;
    }
}
