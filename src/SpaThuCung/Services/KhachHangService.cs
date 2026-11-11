using Microsoft.EntityFrameworkCore;
using SpaThuCung.Data;
using SpaThuCung.Models;

namespace SpaThuCung.Services;

public class KhachHangService : IKhachHangService
{
    private const string LoiKetNoi = "Không kết nối được cơ sở dữ liệu.";
    private const string LoiChuaDangNhap = "Phiên làm việc đã hết, hãy đăng nhập lại.";
    private const string LoiLuu = "Không lưu được vào cơ sở dữ liệu.";
    private const string LoiKhongTimKhach = "Không tìm thấy khách hàng, hãy tải lại.";
    private const string LoiKhongTimThuCung = "Không tìm thấy thú cưng, hãy tải lại.";

    private readonly IDbContextFactory<SpaDbContext> _dbFactory;
    private readonly IPhienLamViec _phien;
    private readonly ICauHinhService _cauHinh;

    public KhachHangService(IDbContextFactory<SpaDbContext> dbFactory, IPhienLamViec phien, ICauHinhService cauHinh)
    {
        _dbFactory = dbFactory;
        _phien = phien;
        _cauHinh = cauHinh;
    }

    // ---------- Khách hàng ----------

    public async Task<KetQua<List<KhachHangDto>>> LayDanhSachKhachAsync()
    {
        if (!_phien.DaDangNhap) return KetQua<List<KhachHangDto>>.Loi(LoiChuaDangNhap);

        // Ngưỡng cảnh báo lấy từ Cấu hình (mặc định 3 lần).
        var nguong = await _cauHinh.LayGiaTriAsync(MaCauHinh.NguongCanhBaoKhongDen);
        if (!nguong.ThanhCong) return KetQua<List<KhachHangDto>>.Loi(nguong.ThongBao);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var ds = await db.KhachHang.AsNoTracking()
                .OrderBy(k => k.HoTen).ThenBy(k => k.MaKh)
                .Select(k => new
                {
                    k.MaKh,
                    k.HoTen,
                    k.Sdt,
                    k.GhiChu,
                    SoThuCung = k.ThuCung.Count,
                    SoKhongDen = db.PhieuDichVu.Count(p => p.ThuCung!.MaKh == k.MaKh && p.TrangThai == TrangThaiPhieu.KhongDen),
                })
                .ToListAsync();

            return KetQua<List<KhachHangDto>>.Ok(ds
                .Select(k => new KhachHangDto(k.MaKh, k.HoTen, k.Sdt, k.GhiChu, k.SoThuCung, k.SoKhongDen,
                    k.SoKhongDen >= nguong.DuLieu))
                .ToList());
        }
        catch (Exception)
        {
            return KetQua<List<KhachHangDto>>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<KetQuaThemKhach>> ThemKhachAsync(KhachHangNhap khach)
    {
        if (!_phien.DaDangNhap) return KetQua<KetQuaThemKhach>.Loi(LoiChuaDangNhap);

        var loi = KiemTraKhach(khach);
        if (loi is not null) return KetQua<KetQuaThemKhach>.Loi(loi);
        var sdt = khach.Sdt.Trim();

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // FR03: nhập SĐT đã có thì mở hồ sơ cũ thay vì tạo mới.
            var daCo = await db.KhachHang.AsNoTracking().FirstOrDefaultAsync(k => k.Sdt == sdt);
            if (daCo is not null)
                return KetQua<KetQuaThemKhach>.Ok(new KetQuaThemKhach(daCo.MaKh, true, daCo.HoTen),
                    $"SĐT {sdt} đã có hồ sơ của {daCo.HoTen}, đã mở hồ sơ cũ.");

            var kh = new KhachHang
            {
                HoTen = khach.HoTen.Trim(),
                Sdt = sdt,
                GhiChu = QuyTacKhachHang.ChuanHoa(khach.GhiChu),
            };
            db.KhachHang.Add(kh);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Trùng SĐT do máy khác vừa thêm (ràng buộc UNIQUE), hoặc lỗi ghi khác.
                return KetQua<KetQuaThemKhach>.Loi(LoiLuu + " Có thể SĐT vừa được dùng cho khách khác, hãy tải lại.");
            }

            return KetQua<KetQuaThemKhach>.Ok(new KetQuaThemKhach(kh.MaKh, false, kh.HoTen), $"Đã thêm khách {kh.HoTen}.");
        }
        catch (Exception)
        {
            return KetQua<KetQuaThemKhach>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua> SuaKhachAsync(int maKh, KhachHangNhap khach)
    {
        if (!_phien.DaDangNhap) return KetQua.Loi(LoiChuaDangNhap);

        var loi = KiemTraKhach(khach);
        if (loi is not null) return KetQua.Loi(loi);
        var sdt = khach.Sdt.Trim();

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var kh = await db.KhachHang.FirstOrDefaultAsync(k => k.MaKh == maKh);
            if (kh is null) return KetQua.Loi(LoiKhongTimKhach);

            var trung = await db.KhachHang.AsNoTracking()
                .Where(k => k.Sdt == sdt && k.MaKh != maKh)
                .Select(k => k.HoTen)
                .FirstOrDefaultAsync();
            if (trung is not null) return KetQua.Loi($"SĐT {sdt} đã thuộc khách {trung}.");

            kh.HoTen = khach.HoTen.Trim();
            kh.Sdt = sdt;
            kh.GhiChu = QuyTacKhachHang.ChuanHoa(khach.GhiChu);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return KetQua.Loi(LoiLuu + " Có thể SĐT vừa được dùng cho khách khác, hãy tải lại.");
            }

            return KetQua.Ok($"Đã lưu khách {kh.HoTen}.");
        }
        catch (Exception)
        {
            return KetQua.Loi(LoiKetNoi);
        }
    }

    private static string? KiemTraKhach(KhachHangNhap khach) =>
        QuyTacKhachHang.KiemTraHoTen(khach.HoTen)
        ?? QuyTacKhachHang.KiemTraSdt(khach.Sdt)
        ?? QuyTacKhachHang.KiemTraGhiChu(khach.GhiChu);

    // ---------- Thú cưng ----------

    public async Task<KetQua<List<ThuCungDto>>> LayDanhSachThuCungAsync(int? maKh = null)
    {
        if (!_phien.DaDangNhap) return KetQua<List<ThuCungDto>>.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var ds = await db.ThuCung.AsNoTracking()
                .Where(t => maKh == null || t.MaKh == maKh)
                .OrderBy(t => t.KhachHang!.HoTen).ThenBy(t => t.Ten).ThenBy(t => t.MaThuCung)
                .Select(t => new ThuCungDto(t.MaThuCung, t.MaKh, t.KhachHang!.HoTen, t.KhachHang.Sdt, t.Ten, t.Loai,
                    t.CanNang, t.DaTiemPhong, t.GhiChu))
                .ToListAsync();
            return KetQua<List<ThuCungDto>>.Ok(ds);
        }
        catch (Exception)
        {
            return KetQua<List<ThuCungDto>>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<int>> ThemThuCungAsync(ThuCungNhap thuCung)
    {
        if (!_phien.DaDangNhap) return KetQua<int>.Loi(LoiChuaDangNhap);

        var loi = KiemTraThuCung(thuCung);
        if (loi is not null) return KetQua<int>.Loi(loi);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var tenChu = await db.KhachHang.AsNoTracking()
                .Where(k => k.MaKh == thuCung.MaKh).Select(k => k.HoTen).FirstOrDefaultAsync();
            if (tenChu is null) return KetQua<int>.Loi("Chọn khách hàng (chủ thú cưng).");

            var tc = new ThuCung
            {
                MaKh = thuCung.MaKh,
                Ten = thuCung.Ten.Trim(),
                Loai = thuCung.Loai,
                CanNang = thuCung.CanNang,
                DaTiemPhong = thuCung.DaTiemPhong,
                GhiChu = QuyTacKhachHang.ChuanHoa(thuCung.GhiChu),
            };
            db.ThuCung.Add(tc);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return KetQua<int>.Loi(LoiLuu);
            }

            return KetQua<int>.Ok(tc.MaThuCung, $"Đã thêm {tc.Ten} cho khách {tenChu}.");
        }
        catch (Exception)
        {
            return KetQua<int>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua> SuaThuCungAsync(int maThuCung, ThuCungNhap thuCung)
    {
        if (!_phien.DaDangNhap) return KetQua.Loi(LoiChuaDangNhap);

        var loi = KiemTraThuCung(thuCung);
        if (loi is not null) return KetQua.Loi(loi);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var tc = await db.ThuCung.FirstOrDefaultAsync(t => t.MaThuCung == maThuCung);
            if (tc is null) return KetQua.Loi(LoiKhongTimThuCung);

            tc.Ten = thuCung.Ten.Trim();
            tc.Loai = thuCung.Loai;
            tc.CanNang = thuCung.CanNang;
            tc.DaTiemPhong = thuCung.DaTiemPhong;
            tc.GhiChu = QuyTacKhachHang.ChuanHoa(thuCung.GhiChu);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return KetQua.Loi(LoiLuu);
            }

            return KetQua.Ok($"Đã lưu thú cưng {tc.Ten}.");
        }
        catch (Exception)
        {
            return KetQua.Loi(LoiKetNoi);
        }
    }

    private static string? KiemTraThuCung(ThuCungNhap thuCung) =>
        QuyTacKhachHang.KiemTraTenThuCung(thuCung.Ten)
        ?? QuyTacKhachHang.KiemTraLoai(thuCung.Loai)
        ?? QuyTacKhachHang.KiemTraCanNang(thuCung.CanNang)
        ?? QuyTacKhachHang.KiemTraGhiChu(thuCung.GhiChu);
}
