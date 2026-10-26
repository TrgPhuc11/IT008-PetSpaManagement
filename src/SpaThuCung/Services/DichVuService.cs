using Microsoft.EntityFrameworkCore;
using SpaThuCung.Data;
using SpaThuCung.Models;

namespace SpaThuCung.Services;

public class DichVuService : IDichVuService
{
    private const string LoiKetNoi = "Không kết nối được cơ sở dữ liệu.";
    private const string LoiChuaDangNhap = "Phiên làm việc đã hết, hãy đăng nhập lại.";
    private const string LoiQuyen = "Chỉ Quản lý được thêm, sửa dịch vụ và chỉnh bảng giá.";
    private const string LoiLuu = "Không lưu được vào cơ sở dữ liệu.";
    private const string LoiKhongTim = "Không tìm thấy dịch vụ.";
    private const string LoiBangGiaDaDoi = "Bảng giá vừa được thay đổi ở nơi khác. Hãy tải lại rồi sửa.";

    private readonly IDbContextFactory<SpaDbContext> _dbFactory;
    private readonly IPhienLamViec _phien;

    public DichVuService(IDbContextFactory<SpaDbContext> dbFactory, IPhienLamViec phien)
    {
        _dbFactory = dbFactory;
        _phien = phien;
    }

    public async Task<KetQua<List<DichVuDto>>> LayDanhSachAsync()
    {
        if (!_phien.DaDangNhap) return KetQua<List<DichVuDto>>.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var ds = await db.DichVu.AsNoTracking()
                .OrderBy(x => x.MaDv)
                .Select(x => new DichVuDto(x.MaDv, x.TenDv, x.MoTa, x.TrangThai))
                .ToListAsync();
            return KetQua<List<DichVuDto>>.Ok(ds);
        }
        catch (Exception)
        {
            return KetQua<List<DichVuDto>>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<List<DongGiaDto>>> LayBangGiaAsync(int maDv)
    {
        if (!_phien.DaDangNhap) return KetQua<List<DongGiaDto>>.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var ds = await db.BangGia.AsNoTracking()
                .Where(x => x.MaDv == maDv)
                .Select(x => new DongGiaDto(x.MaGia, x.Loai, x.CanNangTu, x.CanNangDen, x.Gia, x.ThoiLuong))
                .ToListAsync();

            // Chó trước Mèo, rồi theo cân nặng (sắp ở client vì thứ tự loài theo danh sách cố định).
            return KetQua<List<DongGiaDto>>.Ok(SapXep(ds).ToList());
        }
        catch (Exception)
        {
            return KetQua<List<DongGiaDto>>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<DichVuDto>> ThemAsync(string tenDv, string? moTa)
    {
        if (!_phien.LaQuanLy) return KetQua<DichVuDto>.Loi(LoiQuyen);

        tenDv = (tenDv ?? string.Empty).Trim();
        moTa = string.IsNullOrWhiteSpace(moTa) ? null : moTa.Trim();

        var loi = QuyTacDichVu.KiemTraTen(tenDv) ?? QuyTacDichVu.KiemTraMoTa(moTa);
        if (loi is not null) return KetQua<DichVuDto>.Loi(loi);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // So sánh không phân biệt hoa thường nhờ collation mặc định của SQL Server.
            if (await db.DichVu.AnyAsync(x => x.TenDv == tenDv))
                return KetQua<DichVuDto>.Loi(LoiTrungTen(tenDv));

            var dv = new DichVu { TenDv = tenDv, MoTa = moTa, TrangThai = TrangThaiDichVu.DangCungCap };
            db.DichVu.Add(dv);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Hai người cùng thêm một tên: ràng buộc UNIQUE chặn lại.
                return KetQua<DichVuDto>.Loi(await TrungTenAsync(tenDv, 0) ? LoiTrungTen(tenDv) : LoiLuu);
            }

            return KetQua<DichVuDto>.Ok(ToDto(dv), $"Đã thêm {tenDv}. Thêm mức giá ở bảng bên phải.");
        }
        catch (Exception)
        {
            return KetQua<DichVuDto>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<DichVuDto>> CapNhatAsync(int maDv, string tenDv, string? moTa)
    {
        if (!_phien.LaQuanLy) return KetQua<DichVuDto>.Loi(LoiQuyen);

        tenDv = (tenDv ?? string.Empty).Trim();
        moTa = string.IsNullOrWhiteSpace(moTa) ? null : moTa.Trim();

        var loi = QuyTacDichVu.KiemTraTen(tenDv) ?? QuyTacDichVu.KiemTraMoTa(moTa);
        if (loi is not null) return KetQua<DichVuDto>.Loi(loi);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var dv = await db.DichVu.FirstOrDefaultAsync(x => x.MaDv == maDv);
            if (dv is null) return KetQua<DichVuDto>.Loi(LoiKhongTim);

            if (await db.DichVu.AnyAsync(x => x.TenDv == tenDv && x.MaDv != maDv))
                return KetQua<DichVuDto>.Loi(LoiTrungTen(tenDv));

            dv.TenDv = tenDv;
            dv.MoTa = moTa;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return KetQua<DichVuDto>.Loi(await TrungTenAsync(tenDv, maDv) ? LoiTrungTen(tenDv) : LoiLuu);
            }

            return KetQua<DichVuDto>.Ok(ToDto(dv), $"Đã cập nhật {tenDv}.");
        }
        catch (Exception)
        {
            return KetQua<DichVuDto>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<DichVuDto>> DoiTrangThaiAsync(int maDv)
    {
        if (!_phien.LaQuanLy) return KetQua<DichVuDto>.Loi(LoiQuyen);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var dv = await db.DichVu.FirstOrDefaultAsync(x => x.MaDv == maDv);
            if (dv is null) return KetQua<DichVuDto>.Loi(LoiKhongTim);

            string thongBao;
            if (dv.TrangThai == TrangThaiDichVu.DangCungCap)
            {
                dv.TrangThai = TrangThaiDichVu.NgungCungCap;
                thongBao = $"Đã ngừng cung cấp {dv.TenDv}. Phiếu cũ không bị ảnh hưởng.";
            }
            else
            {
                dv.TrangThai = TrangThaiDichVu.DangCungCap;
                thongBao = $"Đã mở lại {dv.TenDv}.";
            }

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return KetQua<DichVuDto>.Loi(LoiLuu);
            }

            return KetQua<DichVuDto>.Ok(ToDto(dv), thongBao);
        }
        catch (Exception)
        {
            return KetQua<DichVuDto>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua> LuuBangGiaAsync(int maDv, IReadOnlyList<DongGiaNhap> ds)
    {
        if (!_phien.LaQuanLy) return KetQua.Loi(LoiQuyen);

        var loi = QuyTacDichVu.KiemTraBangGia(ds);
        if (loi is not null) return KetQua.Loi(loi);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            if (!await db.DichVu.AnyAsync(x => x.MaDv == maDv)) return KetQua.Loi(LoiKhongTim);

            var cu = await db.BangGia.Where(x => x.MaDv == maDv).ToListAsync();
            var theoMa = cu.ToDictionary(x => x.MaGia);

            // Dòng cũ phải còn trong CSDL và thuộc đúng dịch vụ này.
            if (ds.Any(d => d.MaGia != 0 && !theoMa.ContainsKey(d.MaGia)))
                return KetQua.Loi(LoiBangGiaDaDoi);

            var giuLai = ds.Where(d => d.MaGia != 0).Select(d => d.MaGia).ToHashSet();
            db.BangGia.RemoveRange(cu.Where(x => !giuLai.Contains(x.MaGia)));

            foreach (var d in ds)
            {
                var dong = d.MaGia == 0 ? new BangGia { MaDv = maDv } : theoMa[d.MaGia];
                dong.Loai = d.Loai;
                dong.CanNangTu = d.CanNangTu;
                dong.CanNangDen = d.CanNangDen;
                dong.Gia = d.Gia;
                dong.ThoiLuong = d.ThoiLuong;
                if (d.MaGia == 0) db.BangGia.Add(dong);
            }

            // Một lần SaveChanges chạy trong một transaction: lỗi thì không dòng nào bị đổi.
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return KetQua.Loi(LoiLuu);
            }

            return KetQua.Ok("Đã lưu bảng giá. Phiếu lập từ giờ dùng giá và thời lượng mới; phiếu đã lập giữ giá cũ.");
        }
        catch (Exception)
        {
            return KetQua.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<List<GiaTraCuu>>> TraGiaAsync(string loai, decimal canNang)
    {
        if (!_phien.DaDangNhap) return KetQua<List<GiaTraCuu>>.Loi(LoiChuaDangNhap);
        if (!LoaiThuCung.TatCa.Contains(loai)) return KetQua<List<GiaTraCuu>>.Loi("Chọn loài.");
        if (canNang <= 0 || canNang > QuyTacDichVu.CanNangToiDa)
            return KetQua<List<GiaTraCuu>>.Loi("Nhập cân nặng lớn hơn 0.");

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var ds = await db.DichVu.AsNoTracking()
                .Where(x => x.TrangThai == TrangThaiDichVu.DangCungCap)
                .OrderBy(x => x.MaDv)
                .Include(x => x.BangGia.Where(g => g.Loai == loai))
                .ToListAsync();

            var kq = ds.Select(dv =>
            {
                var g = QuyTacDichVu.TimDongGia(dv.BangGia, loai, canNang);
                return new GiaTraCuu(dv.MaDv, dv.TenDv, g?.CanNangTu, g?.CanNangDen, g?.Gia, g?.ThoiLuong);
            }).ToList();

            return KetQua<List<GiaTraCuu>>.Ok(kq);
        }
        catch (Exception)
        {
            return KetQua<List<GiaTraCuu>>.Loi(LoiKetNoi);
        }
    }

    private async Task<bool> TrungTenAsync(string tenDv, int boQuaMa)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.DichVu.AnyAsync(x => x.TenDv == tenDv && x.MaDv != boQuaMa);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static IEnumerable<DongGiaDto> SapXep(IEnumerable<DongGiaDto> ds) =>
        ds.OrderBy(x => x.Loai == LoaiThuCung.Cho ? 0 : 1).ThenBy(x => x.CanNangTu);

    private static string LoiTrungTen(string ten) => $"Dịch vụ \"{ten}\" đã có.";

    private static DichVuDto ToDto(DichVu dv) => new(dv.MaDv, dv.TenDv, dv.MoTa, dv.TrangThai);
}
