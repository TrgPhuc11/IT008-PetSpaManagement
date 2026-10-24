using SpaThuCung.Models;

namespace SpaThuCung.Services;

/// <summary>
/// Nhân viên để hiển thị trên giao diện. Cố ý không có mật khẩu băm.
/// Là class (không phải record) để so sánh theo tham chiếu, tránh DataGrid/ObservableProperty hiểu nhầm hai bản giống nhau là một.
/// </summary>
public sealed class NhanVienDto
{
    public NhanVienDto(int maNv, string hoTen, string? sdt, string tenDangNhap, string vaiTro, string trangThaiLamViec)
    {
        MaNv = maNv;
        HoTen = hoTen;
        Sdt = sdt;
        TenDangNhap = tenDangNhap;
        VaiTro = vaiTro;
        TrangThaiLamViec = trangThaiLamViec;
    }

    public int MaNv { get; }
    public string HoTen { get; }
    public string? Sdt { get; }
    public string TenDangNhap { get; }
    public string VaiTro { get; }
    public string TrangThaiLamViec { get; }

    /// <summary>Mã dạng NV001.</summary>
    public string MaHienThi => $"NV{MaNv:000}";

    public bool DaNghiViec => TrangThaiLamViec == Models.TrangThaiLamViec.DaNghiViec;
}

/// <summary>Dữ liệu để thêm nhân viên mới.</summary>
public sealed record NhanVienMoi(string HoTen, string? Sdt, string TenDangNhap, string VaiTro, string MatKhau);
