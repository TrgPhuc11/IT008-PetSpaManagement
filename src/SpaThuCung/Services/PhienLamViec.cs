using SpaThuCung.Models;

namespace SpaThuCung.Services;

public class PhienLamViec : IPhienLamViec
{
    public NhanVien? NhanVienHienTai { get; private set; }

    public bool DaDangNhap => NhanVienHienTai is not null;

    public bool LaQuanLy => NhanVienHienTai?.VaiTro == VaiTro.QuanLy;

    public event Action? ThongTinThayDoi;

    public void DangNhap(NhanVien nhanVien) => NhanVienHienTai = nhanVien;

    public void DangXuat() => NhanVienHienTai = null;

    public void CapNhatThongTin(NhanVien nhanVien)
    {
        // Chỉ cập nhật khi vẫn đang đăng nhập đúng người đó.
        if (NhanVienHienTai is null || NhanVienHienTai.MaNv != nhanVien.MaNv) return;

        NhanVienHienTai = nhanVien;
        ThongTinThayDoi?.Invoke();
    }
}
