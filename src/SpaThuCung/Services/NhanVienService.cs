using Microsoft.EntityFrameworkCore;
using SpaThuCung.Data;
using SpaThuCung.Models;
using BCryptNet = BCrypt.Net.BCrypt;

namespace SpaThuCung.Services;

public class NhanVienService : INhanVienService
{
    private const string LoiKetNoi = "Không kết nối được cơ sở dữ liệu.";
    private const string LoiQuyen = "Chỉ Quản lý được dùng chức năng này.";
    private const string LoiLuu = "Không lưu được vào cơ sở dữ liệu.";
    private const string LoiKhongTim = "Không tìm thấy nhân viên.";
    private const string LoiTuMinh = "Không thể đổi trạng thái/vai trò của tài khoản đang đăng nhập.";

    private readonly IDbContextFactory<SpaDbContext> _dbFactory;
    private readonly IPhienLamViec _phien;

    public NhanVienService(IDbContextFactory<SpaDbContext> dbFactory, IPhienLamViec phien)
    {
        _dbFactory = dbFactory;
        _phien = phien;
    }

    public async Task<KetQua<List<NhanVienDto>>> LayDanhSachAsync()
    {
        if (!_phien.LaQuanLy) return KetQua<List<NhanVienDto>>.Loi(LoiQuyen);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // Chỉ lấy các cột cần hiển thị, không đọc mật khẩu băm.
            var ds = await db.NhanVien.AsNoTracking()
                .OrderBy(x => x.MaNv)
                .Select(x => new { x.MaNv, x.HoTen, x.Sdt, x.TenDangNhap, x.VaiTro, x.TrangThaiLamViec })
                .ToListAsync();

            return KetQua<List<NhanVienDto>>.Ok(ds
                .Select(x => new NhanVienDto(x.MaNv, x.HoTen, x.Sdt, x.TenDangNhap, x.VaiTro, x.TrangThaiLamViec))
                .ToList());
        }
        catch (Exception)
        {
            return KetQua<List<NhanVienDto>>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<NhanVienDto>> ThemAsync(NhanVienMoi du)
    {
        if (!_phien.LaQuanLy) return KetQua<NhanVienDto>.Loi(LoiQuyen);

        var hoTen = (du.HoTen ?? string.Empty).Trim();
        var sdt = string.IsNullOrWhiteSpace(du.Sdt) ? null : du.Sdt.Trim();
        var tenDangNhap = (du.TenDangNhap ?? string.Empty).Trim();

        var loi = QuyTacNhanVien.KiemTraHoTen(hoTen)
               ?? QuyTacNhanVien.KiemTraSdt(sdt)
               ?? QuyTacNhanVien.KiemTraTenDangNhap(tenDangNhap)
               ?? QuyTacNhanVien.KiemTraVaiTro(du.VaiTro)
               ?? QuyTacNhanVien.KiemTraMatKhau(du.MatKhau);
        if (loi is not null) return KetQua<NhanVienDto>.Loi(loi);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // So sánh không phân biệt hoa thường nhờ collation mặc định của SQL Server.
            if (await db.NhanVien.AnyAsync(x => x.TenDangNhap == tenDangNhap))
                return KetQua<NhanVienDto>.Loi(LoiTrungTen(tenDangNhap));

            var matKhau = du.MatKhau;
            var hash = await Task.Run(() => BCryptNet.HashPassword(matKhau));

            var nv = new NhanVien
            {
                HoTen = hoTen,
                Sdt = sdt,
                VaiTro = du.VaiTro,
                TrangThaiLamViec = TrangThaiLamViec.DangLamViec,
                TenDangNhap = tenDangNhap,
                MatKhau = hash,
            };
            db.NhanVien.Add(nv);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Hai người cùng thêm một tên đăng nhập: ràng buộc UNIQUE chặn lại.
                await using var db2 = await _dbFactory.CreateDbContextAsync();
                var trung = await db2.NhanVien.AnyAsync(x => x.TenDangNhap == tenDangNhap);
                return KetQua<NhanVienDto>.Loi(trung ? LoiTrungTen(tenDangNhap) : LoiLuu);
            }

            var dto = ToDto(nv);
            return KetQua<NhanVienDto>.Ok(dto, $"Đã thêm {dto.HoTen} ({dto.MaHienThi}).");
        }
        catch (Exception)
        {
            return KetQua<NhanVienDto>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<NhanVienDto>> CapNhatAsync(int maNv, string hoTen, string? sdt, string vaiTro)
    {
        if (!_phien.LaQuanLy) return KetQua<NhanVienDto>.Loi(LoiQuyen);

        hoTen = (hoTen ?? string.Empty).Trim();
        sdt = string.IsNullOrWhiteSpace(sdt) ? null : sdt.Trim();

        var loi = QuyTacNhanVien.KiemTraHoTen(hoTen)
               ?? QuyTacNhanVien.KiemTraSdt(sdt)
               ?? QuyTacNhanVien.KiemTraVaiTro(vaiTro);
        if (loi is not null) return KetQua<NhanVienDto>.Loi(loi);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var nv = await db.NhanVien.FirstOrDefaultAsync(x => x.MaNv == maNv);
            if (nv is null) return KetQua<NhanVienDto>.Loi(LoiKhongTim);

            var laChinhMinh = _phien.NhanVienHienTai?.MaNv == maNv;
            if (laChinhMinh && nv.VaiTro != vaiTro)
                return KetQua<NhanVienDto>.Loi(LoiTuMinh);

            nv.HoTen = hoTen;
            nv.Sdt = sdt;
            nv.VaiTro = vaiTro;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return KetQua<NhanVienDto>.Loi(LoiLuu);
            }

            // Tự sửa thông tin của mình thì cập nhật phiên để menu hiện đúng tên.
            if (laChinhMinh)
                _phien.CapNhatThongTin(SaoChep(nv));

            return KetQua<NhanVienDto>.Ok(ToDto(nv), $"Đã cập nhật {hoTen}.");
        }
        catch (Exception)
        {
            return KetQua<NhanVienDto>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<NhanVienDto>> DoiTrangThaiAsync(int maNv)
    {
        if (!_phien.LaQuanLy) return KetQua<NhanVienDto>.Loi(LoiQuyen);

        if (_phien.NhanVienHienTai?.MaNv == maNv)
            return KetQua<NhanVienDto>.Loi(LoiTuMinh);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var nv = await db.NhanVien.FirstOrDefaultAsync(x => x.MaNv == maNv);
            if (nv is null) return KetQua<NhanVienDto>.Loi(LoiKhongTim);

            string thongBao;
            if (nv.TrangThaiLamViec == TrangThaiLamViec.DangLamViec)
            {
                nv.TrangThaiLamViec = TrangThaiLamViec.DaNghiViec;
                thongBao = $"{nv.HoTen} đã chuyển sang Đã nghỉ việc, không đăng nhập được nữa.";
            }
            else
            {
                nv.TrangThaiLamViec = TrangThaiLamViec.DangLamViec;
                thongBao = $"{nv.HoTen} đã chuyển lại Đang làm việc.";
            }

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return KetQua<NhanVienDto>.Loi(LoiLuu);
            }

            return KetQua<NhanVienDto>.Ok(ToDto(nv), thongBao);
        }
        catch (Exception)
        {
            return KetQua<NhanVienDto>.Loi(LoiKetNoi);
        }
    }

    private static string LoiTrungTen(string ten) => $"Tên đăng nhập \"{ten}\" đã có người dùng.";

    private static NhanVienDto ToDto(NhanVien nv) =>
        new(nv.MaNv, nv.HoTen, nv.Sdt, nv.TenDangNhap, nv.VaiTro, nv.TrangThaiLamViec);

    private static NhanVien SaoChep(NhanVien nv) => new()
    {
        MaNv = nv.MaNv,
        HoTen = nv.HoTen,
        Sdt = nv.Sdt,
        VaiTro = nv.VaiTro,
        TrangThaiLamViec = nv.TrangThaiLamViec,
        TenDangNhap = nv.TenDangNhap,
        MatKhau = nv.MatKhau,
    };
}
