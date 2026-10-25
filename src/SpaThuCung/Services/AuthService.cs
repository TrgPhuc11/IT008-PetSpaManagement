using Microsoft.EntityFrameworkCore;
using SpaThuCung.Data;
using SpaThuCung.Models;
using BCryptNet = BCrypt.Net.BCrypt;

namespace SpaThuCung.Services;

public class AuthService : IAuthService
{
    private const string SaiThongTin = "Tên đăng nhập hoặc mật khẩu không đúng.";

    private readonly IDbContextFactory<SpaDbContext> _dbFactory;
    private readonly IPhienLamViec _phien;

    public AuthService(IDbContextFactory<SpaDbContext> dbFactory, IPhienLamViec phien)
    {
        _dbFactory = dbFactory;
        _phien = phien;
    }

    public async Task<KetQuaDangNhap> DangNhapAsync(string tenDangNhap, string matKhau)
    {
        tenDangNhap = (tenDangNhap ?? string.Empty).Trim();
        matKhau ??= string.Empty;

        if (tenDangNhap.Length == 0 || matKhau.Length == 0)
            return KetQuaDangNhap.Loi("Nhập tên đăng nhập và mật khẩu.");

        NhanVien? nv;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            nv = await db.NhanVien.AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenDangNhap == tenDangNhap);
        }
        catch (Exception)
        {
            // Lỗi SQL / kết nối: không để app crash.
            return KetQuaDangNhap.Loi("Không kết nối được cơ sở dữ liệu.");
        }

        // Không lộ việc tên đăng nhập hay mật khẩu sai.
        if (nv is null || !KiemTraMatKhau(matKhau, nv.MatKhau))
            return KetQuaDangNhap.Loi(SaiThongTin);

        if (nv.TrangThaiLamViec == TrangThaiLamViec.DaNghiViec)
            return KetQuaDangNhap.Loi("Tài khoản đã ngừng hoạt động (nhân viên đã nghỉ việc). Liên hệ Quản lý.");

        _phien.DangNhap(nv);
        return KetQuaDangNhap.Ok(nv);
    }

    public async Task<KetQua> DoiMatKhauAsync(string matKhauHienTai, string matKhauMoi)
    {
        var nguoiDung = _phien.NhanVienHienTai;
        if (nguoiDung is null) return KetQua.Loi("Chưa đăng nhập.");

        if (string.IsNullOrEmpty(matKhauHienTai)) return KetQua.Loi("Nhập mật khẩu hiện tại.");
        if (string.IsNullOrEmpty(matKhauMoi) || matKhauMoi.Length < QuyTacNhanVien.MatKhauToiThieu)
            return KetQua.Loi($"Mật khẩu mới cần ít nhất {QuyTacNhanVien.MatKhauToiThieu} ký tự.");
        if (matKhauMoi == matKhauHienTai) return KetQua.Loi("Mật khẩu mới phải khác mật khẩu hiện tại.");

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var nv = await db.NhanVien.FirstOrDefaultAsync(x => x.MaNv == nguoiDung.MaNv);
            if (nv is null) return KetQua.Loi("Không tìm thấy tài khoản.");

            // Đối chiếu mật khẩu hiện tại với chuỗi băm trong CSDL.
            if (!KiemTraMatKhau(matKhauHienTai, nv.MatKhau))
                return KetQua.Loi("Mật khẩu hiện tại không đúng.");

            nv.MatKhau = await Task.Run(() => BCryptNet.HashPassword(matKhauMoi));
            await db.SaveChangesAsync();

            return KetQua.Ok("Đã đổi mật khẩu. Lần đăng nhập sau dùng mật khẩu mới.");
        }
        catch (Exception)
        {
            return KetQua.Loi("Không kết nối được cơ sở dữ liệu.");
        }
    }

    public async Task<KetQua> DatLaiMatKhauAsync(int maNv, string matKhauMoi)
    {
        if (!_phien.LaQuanLy) return KetQua.Loi("Chỉ Quản lý được dùng chức năng này.");

        var loi = QuyTacNhanVien.KiemTraMatKhau(matKhauMoi);
        if (loi is not null) return KetQua.Loi(loi);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var nv = await db.NhanVien.FirstOrDefaultAsync(x => x.MaNv == maNv);
            if (nv is null) return KetQua.Loi("Không tìm thấy nhân viên.");

            if (nv.TrangThaiLamViec == TrangThaiLamViec.DaNghiViec)
                return KetQua.Loi($"{nv.HoTen} đã nghỉ việc, không đặt lại mật khẩu.");

            nv.MatKhau = await Task.Run(() => BCryptNet.HashPassword(matKhauMoi));
            await db.SaveChangesAsync();

            return KetQua.Ok($"Đã đặt lại mật khẩu cho {nv.HoTen}. Hãy báo mật khẩu mới cho nhân viên.");
        }
        catch (Exception)
        {
            return KetQua.Loi("Không kết nối được cơ sở dữ liệu.");
        }
    }

    private static bool KiemTraMatKhau(string matKhau, string hash)
    {
        try
        {
            return BCryptNet.Verify(matKhau, hash);
        }
        catch (Exception)
        {
            // Hash hỏng -> coi như sai mật khẩu.
            return false;
        }
    }
}
