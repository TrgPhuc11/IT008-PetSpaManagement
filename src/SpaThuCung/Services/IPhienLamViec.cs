using SpaThuCung.Models;

namespace SpaThuCung.Services;

/// <summary>Phiên làm việc: giữ nhân viên đang đăng nhập.</summary>
public interface IPhienLamViec
{
    NhanVien? NhanVienHienTai { get; }
    bool DaDangNhap { get; }
    bool LaQuanLy { get; }

    /// <summary>Phát ra khi thông tin của người đang đăng nhập đổi (ví dụ tự sửa họ tên).</summary>
    event Action? ThongTinThayDoi;

    void DangNhap(NhanVien nhanVien);
    void DangXuat();

    /// <summary>Thay bản thông tin của người đang đăng nhập bằng bản mới và phát ThongTinThayDoi.</summary>
    void CapNhatThongTin(NhanVien nhanVien);
}
