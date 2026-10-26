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
    private const string LoiBangGiaDaDoi = "Bảng giá vừa được thay đổi ở nơi khác. Bấm Huỷ để tải lại rồi sửa.";

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
                .Include(x => x.BangGia)
                .OrderBy(x => x.MaDv)
                .ToListAsync();
            return KetQua<List<DichVuDto>>.Ok(ds.Select(ToDto).ToList());
        }
        catch (Exception)
        {
            return KetQua<List<DichVuDto>>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<int>> LuuAsync(DichVuLuu du)
    {
        if (!_phien.LaQuanLy) return KetQua<int>.Loi(LoiQuyen);

        var tenDv = (du.TenDv ?? string.Empty).Trim();
        var moTa = string.IsNullOrWhiteSpace(du.MoTa) ? null : du.MoTa.Trim();

        var loi = QuyTacDichVu.KiemTraTen(tenDv)
               ?? QuyTacDichVu.KiemTraMoTa(moTa)
               ?? QuyTacDichVu.KiemTraTrangThai(du.TrangThai)
               ?? QuyTacDichVu.KiemTraBangGia(du.BangGia);
        if (loi is not null) return KetQua<int>.Loi(loi);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // So sánh không phân biệt hoa thường nhờ collation mặc định của SQL Server.
            if (await db.DichVu.AnyAsync(x => x.TenDv == tenDv && x.MaDv != du.MaDv))
                return KetQua<int>.Loi(LoiTrungTen(tenDv));

            DichVu dv;
            if (du.MaDv == 0)
            {
                if (du.BangGia.Any(d => d.MaGia != 0)) return KetQua<int>.Loi(LoiBangGiaDaDoi);

                dv = new DichVu();
                db.DichVu.Add(dv);
            }
            else
            {
                var tim = await db.DichVu.Include(x => x.BangGia).FirstOrDefaultAsync(x => x.MaDv == du.MaDv);
                if (tim is null) return KetQua<int>.Loi(LoiKhongTim);
                dv = tim;

                var theoMa = dv.BangGia.ToDictionary(x => x.MaGia);

                // Dòng cũ phải còn trong CSDL và thuộc đúng dịch vụ này.
                if (du.BangGia.Any(d => d.MaGia != 0 && !theoMa.ContainsKey(d.MaGia)))
                    return KetQua<int>.Loi(LoiBangGiaDaDoi);

                // Chỉ xoá các dòng người dùng đã bấm Xoá; dòng đã bị xoá ở nơi khác thì bỏ qua.
                var xoa = du.MaXoa.ToHashSet();
                db.BangGia.RemoveRange(dv.BangGia.Where(x => xoa.Contains(x.MaGia)));

                // Dòng có trong CSDL mà màn hình không gửi (người khác vừa thêm) vẫn giữ, nên kiểm tra chồng lấn trên cả bảng.
                var coTrongDs = du.BangGia.Where(d => d.MaGia != 0).Select(d => d.MaGia).ToHashSet();
                var conLai = dv.BangGia.Where(x => !xoa.Contains(x.MaGia) && !coTrongDs.Contains(x.MaGia))
                    .Select(x => new DongGiaNhap(x.MaGia, x.Loai, x.CanNangTu, x.CanNangDen, x.Gia, x.ThoiLuong))
                    .ToList();
                if (conLai.Count > 0 && QuyTacDichVu.KiemTraBangGia(du.BangGia.Concat(conLai).ToList()) is not null)
                    return KetQua<int>.Loi(LoiBangGiaDaDoi);

                foreach (var d in du.BangGia.Where(d => d.MaGia != 0))
                    GanDong(theoMa[d.MaGia], d);
            }

            dv.TenDv = tenDv;
            dv.MoTa = moTa;
            dv.TrangThai = du.TrangThai;
            foreach (var d in du.BangGia.Where(d => d.MaGia == 0))
            {
                var dong = new BangGia();
                GanDong(dong, d);
                dv.BangGia.Add(dong);
            }

            // Một lần SaveChanges chạy trong một transaction: lỗi thì không có gì bị đổi.
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Hai người cùng lưu một tên: ràng buộc UNIQUE chặn lại.
                return KetQua<int>.Loi(await TrungTenAsync(tenDv, du.MaDv) ? LoiTrungTen(tenDv) : LoiLuu);
            }

            var thongBao = du.MaDv == 0
                ? $"Đã thêm {tenDv}."
                : $"Đã lưu {tenDv}. Phiếu lập từ giờ dùng giá và thời lượng mới; phiếu đã lập giữ giá cũ.";
            return KetQua<int>.Ok(dv.MaDv, thongBao);
        }
        catch (Exception)
        {
            return KetQua<int>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<List<GiaTraCuu>>> TraGiaAsync(string loai, decimal canNang)
    {
        if (!_phien.DaDangNhap) return KetQua<List<GiaTraCuu>>.Loi(LoiChuaDangNhap);
        if (!LoaiThuCung.TatCa.Contains(loai)) return KetQua<List<GiaTraCuu>>.Loi("Chọn loài.");
        if (canNang <= 0 || canNang > QuyTacDichVu.CanNangToiDa)
            return KetQua<List<GiaTraCuu>>.Loi($"Cân nặng phải lớn hơn 0 và không quá {QuyTacDichVu.KgHienThi(QuyTacDichVu.CanNangToiDa)} kg.");

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

    private static void GanDong(BangGia dong, DongGiaNhap d)
    {
        dong.Loai = d.Loai;
        dong.CanNangTu = d.CanNangTu;
        dong.CanNangDen = d.CanNangDen;
        dong.Gia = d.Gia;
        dong.ThoiLuong = d.ThoiLuong;
    }

    private static string LoiTrungTen(string ten) => $"Dịch vụ \"{ten}\" đã có.";

    /// <summary>Bảng giá sắp Chó trước Mèo, rồi theo cân nặng.</summary>
    private static DichVuDto ToDto(DichVu dv) => new(dv.MaDv, dv.TenDv, dv.MoTa, dv.TrangThai,
        dv.BangGia
            .OrderBy(x => x.Loai == LoaiThuCung.Cho ? 0 : 1).ThenBy(x => x.CanNangTu)
            .Select(x => new DongGiaDto(x.MaGia, x.Loai, x.CanNangTu, x.CanNangDen, x.Gia, x.ThoiLuong))
            .ToList());
}
