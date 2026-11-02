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

    // =====================================================================
    // Lịch làm việc (UC05, FR13, BR02)
    // =====================================================================

    public async Task<KetQua<LichTuanDto>> LayLichTuanAsync(DateOnly ngayTrongTuan)
    {
        if (!_phien.LaQuanLy) return KetQua<LichTuanDto>.Loi(LoiQuyen);

        var dau = QuyTacLichLamViec.NgayDauTuan(ngayTrongTuan);
        var cuoi = dau.AddDays(6);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var cacDong = await db.LichLamViec.AsNoTracking()
                .Where(x => x.Ngay >= dau && x.Ngay <= cuoi)
                .OrderBy(x => x.Ngay).ThenBy(x => x.ViTri).ThenBy(x => x.NhanVien!.HoTen)
                .Select(x => new DongLichDto(
                    x.MaLich, x.MaNv, x.NhanVien!.HoTen,
                    x.NhanVien.TrangThaiLamViec == TrangThaiLamViec.DaNghiViec,
                    x.Ngay, x.MaCa, x.ViTri))
                .ToListAsync();

            // Ca đang sử dụng, cộng các ca đã ngừng nhưng tuần này còn lịch cũ.
            var maCaCoLich = cacDong.Select(x => x.MaCa).Distinct().ToList();
            var cacCa = (await db.CaLamViec.AsNoTracking()
                    .Where(x => x.DangSuDung || maCaCoLich.Contains(x.MaCa))
                    .OrderBy(x => x.GioBatDau).ThenBy(x => x.MaCa)
                    .ToListAsync())
                .Select(CaToDto)
                .ToList();

            var nhanVien = await db.NhanVien.AsNoTracking()
                .Where(x => x.TrangThaiLamViec == TrangThaiLamViec.DangLamViec)
                .OrderBy(x => x.HoTen).ThenBy(x => x.MaNv)
                .Select(x => new NhanVienChonDto(x.MaNv, x.HoTen, x.VaiTro))
                .ToListAsync();

            return KetQua<LichTuanDto>.Ok(new LichTuanDto(dau, cacCa, cacDong, nhanVien));
        }
        catch (Exception)
        {
            return KetQua<LichTuanDto>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<KetQuaXepLich>> XepLichAsync(int maNv, int maCa, string viTri, IReadOnlyCollection<DateOnly> cacNgay)
    {
        if (!_phien.LaQuanLy) return KetQua<KetQuaXepLich>.Loi(LoiQuyen);

        var loi = QuyTacLichLamViec.KiemTraViTri(viTri);
        if (loi is not null) return KetQua<KetQuaXepLich>.Loi(loi);

        var ngay = (cacNgay ?? Array.Empty<DateOnly>()).Distinct().OrderBy(x => x).ToList();
        if (ngay.Count == 0) return KetQua<KetQuaXepLich>.Loi("Chọn ít nhất một ngày.");

        var daQua = ngay.Where(QuyTacLichLamViec.DaQua).ToList();
        if (daQua.Count > 0)
            return KetQua<KetQuaXepLich>.Loi($"Không xếp lịch cho ngày đã qua: {QuyTacLichLamViec.NoiNgay(daQua)}.");

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var nv = await db.NhanVien.AsNoTracking().FirstOrDefaultAsync(x => x.MaNv == maNv);
            if (nv is null) return KetQua<KetQuaXepLich>.Loi(LoiKhongTim);
            if (nv.TrangThaiLamViec != TrangThaiLamViec.DangLamViec)
                return KetQua<KetQuaXepLich>.Loi($"{nv.HoTen} đã nghỉ việc, không xếp lịch được.");

            var ca = await db.CaLamViec.AsNoTracking().FirstOrDefaultAsync(x => x.MaCa == maCa);
            if (ca is null) return KetQua<KetQuaXepLich>.Loi("Không tìm thấy ca làm việc, hãy tải lại.");
            if (!ca.DangSuDung) return KetQua<KetQuaXepLich>.Loi($"{ca.TenCa} đã ngừng sử dụng, không xếp lịch được.");

            // BR02: mỗi nhân viên tối đa một ca, một vị trí mỗi ngày. Ngày đã có lịch thì bỏ qua.
            var daCo = await db.LichLamViec.AsNoTracking()
                .Where(x => x.MaNv == maNv && ngay.Contains(x.Ngay))
                .Select(x => x.Ngay)
                .ToListAsync();
            var trung = ngay.Where(daCo.Contains).ToList();
            var luu = ngay.Except(trung).ToList();

            if (luu.Count == 0)
                return KetQua<KetQuaXepLich>.Ok(
                    new KetQuaXepLich(luu, trung, new List<DateOnly>()),
                    $"Không thêm được ngày nào: {nv.HoTen} đã có lịch vào {QuyTacLichLamViec.NoiNgay(trung)}.");

            foreach (var d in luu)
                db.LichLamViec.Add(new LichLamViec { MaNv = maNv, Ngay = d, MaCa = maCa, ViTri = viTri });

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Có người vừa xếp cùng nhân viên, cùng ngày: ràng buộc UNIQUE (ma_nv, ngay) chặn cả lượt lưu.
                return KetQua<KetQuaXepLich>.Loi("Không lưu được: lịch vừa bị thay đổi ở nơi khác, hãy tải lại rồi xếp lại.");
            }

            // Vượt số người cấu hình của ca: chỉ cảnh báo để Quản lý cân nhắc (UC05).
            var dinhMuc = QuyTacLichLamViec.DinhMuc(CaToDto(ca), viTri);
            var soNguoi = await db.LichLamViec.AsNoTracking()
                .Where(x => x.MaCa == maCa && x.ViTri == viTri && luu.Contains(x.Ngay))
                .GroupBy(x => x.Ngay)
                .Select(g => new { Ngay = g.Key, So = g.Count() })
                .ToListAsync();
            var vuot = soNguoi.Where(x => x.So > dinhMuc).Select(x => x.Ngay).OrderBy(x => x).ToList();

            var thongBao = $"Đã xếp {nv.HoTen} ({viTri}, {ca.TenCa}) vào {luu.Count} ngày: {QuyTacLichLamViec.NoiNgay(luu)}.";
            if (trung.Count > 0)
                thongBao += $" Bỏ qua {QuyTacLichLamViec.NoiNgay(trung)} vì đã có lịch trong ngày.";
            if (vuot.Count > 0)
                thongBao += $" {QuyTacLichLamViec.NoiNgay(vuot)} vượt số {viTri} của ca ({dinhMuc} người).";

            return KetQua<KetQuaXepLich>.Ok(new KetQuaXepLich(luu, trung, vuot), thongBao);
        }
        catch (Exception)
        {
            return KetQua<KetQuaXepLich>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua> XoaLichAsync(int maLich)
    {
        if (!_phien.LaQuanLy) return KetQua.Loi(LoiQuyen);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var lich = await db.LichLamViec
                .Include(x => x.NhanVien)
                .Include(x => x.CaLamViec)
                .FirstOrDefaultAsync(x => x.MaLich == maLich);
            if (lich is null) return KetQua.Loi("Không tìm thấy dòng lịch, hãy tải lại.");
            if (QuyTacLichLamViec.DaQua(lich.Ngay)) return KetQua.Loi("Không xoá lịch của ngày đã qua.");

            // TODO (khi có bảng PhieuDichVu): không xoá lịch Groomer đã được phân công phiếu trong ca đó.

            db.LichLamViec.Remove(lich);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return KetQua.Loi(LoiLuu);
            }

            return KetQua.Ok($"Đã xoá {lich.NhanVien?.HoTen} khỏi {lich.CaLamViec?.TenCa} {QuyTacLichLamViec.ThuVaNgay(lich.Ngay)}.");
        }
        catch (Exception)
        {
            return KetQua.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<List<NhanVienChonDto>>> LayGroomerTrongCaAsync(DateOnly ngay, int maCa)
    {
        if (!_phien.DaDangNhap) return KetQua<List<NhanVienChonDto>>.Loi("Phiên làm việc đã hết, hãy đăng nhập lại.");

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var ds = await db.LichLamViec.AsNoTracking()
                .Where(x => x.Ngay == ngay && x.MaCa == maCa && x.ViTri == ViTri.Groomer
                            && x.NhanVien!.TrangThaiLamViec == TrangThaiLamViec.DangLamViec)
                .OrderBy(x => x.NhanVien!.HoTen)
                .Select(x => new NhanVienChonDto(x.MaNv, x.NhanVien!.HoTen, x.NhanVien.VaiTro))
                .ToListAsync();
            return KetQua<List<NhanVienChonDto>>.Ok(ds);
        }
        catch (Exception)
        {
            return KetQua<List<NhanVienChonDto>>.Loi(LoiKetNoi);
        }
    }

    private static CaLamViecDto CaToDto(CaLamViec x) =>
        new(x.MaCa, x.TenCa, x.GioBatDau, x.GioKetThuc, x.PhutVuotChoPhep, x.SoLeTan, x.SoGroomer, x.DangSuDung);

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
