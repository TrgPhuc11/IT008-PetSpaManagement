using System.Globalization;
using System.IO;
using Microsoft.EntityFrameworkCore;
using SpaThuCung.Data;
using SpaThuCung.Models;

namespace SpaThuCung.Services;

public class CauHinhService : ICauHinhService
{
    private const string LoiKetNoi = "Không kết nối được cơ sở dữ liệu.";
    private const string LoiChuaDangNhap = "Phiên làm việc đã hết, hãy đăng nhập lại.";
    private const string LoiQuyen = "Chỉ Quản lý được chỉnh cấu hình hệ thống.";
    private const string LoiLuu = "Không lưu được vào cơ sở dữ liệu.";
    private const string LoiKhongTimCa = "Không tìm thấy ca làm việc, hãy tải lại.";

    private readonly IDbContextFactory<SpaDbContext> _dbFactory;
    private readonly IPhienLamViec _phien;

    public CauHinhService(IDbContextFactory<SpaDbContext> dbFactory, IPhienLamViec phien)
    {
        _dbFactory = dbFactory;
        _phien = phien;
    }

    // ---------- Thông số ----------

    public async Task<KetQua<ThongSoDto>> LayThongSoAsync()
    {
        if (!_phien.DaDangNhap) return KetQua<ThongSoDto>.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var theoMa = await db.CauHinh.AsNoTracking().ToDictionaryAsync(x => x.MaCauHinh, x => x.GiaTri);

            var so = QuyTacCauHinh.ThongSoSo.ToDictionary(ts => ts.Ma, ts => DocSo(ts, theoMa.GetValueOrDefault(ts.Ma)));
            var thuMuc = theoMa.GetValueOrDefault(MaCauHinh.ThuMucAnh);
            if (string.IsNullOrWhiteSpace(thuMuc)) thuMuc = QuyTacCauHinh.ThuMucAnhMacDinh;

            return KetQua<ThongSoDto>.Ok(new ThongSoDto(so, thuMuc));
        }
        catch (Exception)
        {
            return KetQua<ThongSoDto>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua> CapNhatAsync(ThongSoDto thongSo)
    {
        if (!_phien.LaQuanLy) return KetQua.Loi(LoiQuyen);

        foreach (var ts in QuyTacCauHinh.ThongSoSo)
        {
            if (!thongSo.GiaTriSo.TryGetValue(ts.Ma, out var v)) return KetQua.Loi($"Thiếu {ts.Ten.ToLowerInvariant()}.");
            var loi = QuyTacCauHinh.KiemTraSo(ts, v);
            if (loi is not null) return KetQua.Loi($"{ts.Ten}: {loi}");
        }

        var loiThuMuc = QuyTacCauHinh.KiemTraThuMuc(thongSo.ThuMucAnh);
        if (loiThuMuc is not null) return KetQua.Loi($"Thư mục ảnh: {loiThuMuc}");
        var thuMuc = thongSo.ThuMucAnh.Trim();

        // Thư mục phải tạo và ghi được, để lúc chụp ảnh không bị lỗi.
        var loiGhi = KiemTraGhiThuMuc(thuMuc);
        if (loiGhi is not null) return KetQua.Loi(loiGhi);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var dangCo = await db.CauHinh.ToDictionaryAsync(x => x.MaCauHinh);

            foreach (var ts in QuyTacCauHinh.ThongSoSo)
                Gan(db, dangCo, ts.Ma, thongSo.GiaTriSo[ts.Ma].ToString(CultureInfo.InvariantCulture), ts.MoTa);
            Gan(db, dangCo, MaCauHinh.ThuMucAnh, thuMuc, "Thư mục lưu ảnh thú cưng; đường dẫn tương đối tính từ thư mục chương trình");

            try
            {
                // Một lần SaveChanges chạy trong một transaction: lỗi thì không thông số nào bị đổi.
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return KetQua.Loi(LoiLuu);
            }

            return KetQua.Ok("Đã lưu thông số. Các chức năng sẽ dùng giá trị mới ở lần xử lý sau.");
        }
        catch (Exception)
        {
            return KetQua.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<int>> LayGiaTriAsync(string maCauHinh)
    {
        var ts = QuyTacCauHinh.TimThongSo(maCauHinh);
        if (ts is null) return KetQua<int>.Loi($"Không có thông số số {maCauHinh}.");

        var kq = await LayThongSoAsync();
        return kq.ThanhCong ? KetQua<int>.Ok(kq.DuLieu!.GiaTriSo[ts.Ma]) : KetQua<int>.Loi(kq.ThongBao);
    }

    public async Task<KetQua<string>> LayThuMucAnhAsync()
    {
        var kq = await LayThongSoAsync();
        return kq.ThanhCong
            ? KetQua<string>.Ok(QuyTacCauHinh.DuongDanDayDu(kq.DuLieu!.ThuMucAnh))
            : KetQua<string>.Loi(kq.ThongBao);
    }

    // ---------- Ca làm việc ----------

    public async Task<KetQua<List<CaLamViecDto>>> LayDanhSachCaAsync()
    {
        if (!_phien.DaDangNhap) return KetQua<List<CaLamViecDto>>.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return KetQua<List<CaLamViecDto>>.Ok(await DocCacCaAsync(db));
        }
        catch (Exception)
        {
            return KetQua<List<CaLamViecDto>>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<int>> ThemCaAsync(CaLamViecDto ca)
    {
        if (!_phien.LaQuanLy) return KetQua<int>.Loi(LoiQuyen);
        ca = ca with { MaCa = 0, TenCa = (ca.TenCa ?? string.Empty).Trim(), DangSuDung = true };

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var loi = QuyTacCauHinh.KiemTraCa(ca, await DocCacCaAsync(db));
            if (loi is not null) return KetQua<int>.Loi(loi);

            var moi = new CaLamViec { DangSuDung = true };
            GanCa(moi, ca);
            db.CaLamViec.Add(moi);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Hai người cùng thêm một tên: ràng buộc UNIQUE chặn lại.
                return KetQua<int>.Loi(LoiLuu);
            }

            return KetQua<int>.Ok(moi.MaCa, $"Đã thêm {moi.TenCa}.");
        }
        catch (Exception)
        {
            return KetQua<int>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua> SuaCaAsync(CaLamViecDto ca)
    {
        if (!_phien.LaQuanLy) return KetQua.Loi(LoiQuyen);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var tim = await db.CaLamViec.FirstOrDefaultAsync(x => x.MaCa == ca.MaCa);
            if (tim is null) return KetQua.Loi(LoiKhongTimCa);

            // Trạng thái lấy theo CSDL, không theo màn hình.
            ca = ca with { TenCa = (ca.TenCa ?? string.Empty).Trim(), DangSuDung = tim.DangSuDung };
            var loi = QuyTacCauHinh.KiemTraCa(ca, await DocCacCaAsync(db));
            if (loi is not null) return KetQua.Loi(loi);

            GanCa(tim, ca);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return KetQua.Loi(LoiLuu);
            }

            // Khi có bảng PhieuDichVu: liệt kê các phiếu "Đã đặt lịch" có giờ hẹn không còn thuộc ca nào (FR14).
            return KetQua.Ok($"Đã lưu {tim.TenCa}. Xếp lịch và cảnh báo sẽ dùng giờ mới ở lần xử lý sau.");
        }
        catch (Exception)
        {
            return KetQua.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua> DoiTrangThaiCaAsync(int maCa, bool dangSuDung)
    {
        if (!_phien.LaQuanLy) return KetQua.Loi(LoiQuyen);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var tim = await db.CaLamViec.FirstOrDefaultAsync(x => x.MaCa == maCa);
            if (tim is null) return KetQua.Loi(LoiKhongTimCa);
            if (tim.DangSuDung == dangSuDung) return KetQua.Ok();

            var cacCa = await DocCacCaAsync(db);
            if (dangSuDung)
            {
                var loi = QuyTacCauHinh.KiemTraChongGio(ToDto(tim) with { DangSuDung = true }, cacCa);
                if (loi is not null) return KetQua.Loi($"Không dùng lại được {tim.TenCa}: {loi}");
            }
            else if (!cacCa.Any(x => x.MaCa != maCa && x.DangSuDung))
            {
                return KetQua.Loi("Phải còn ít nhất một ca đang sử dụng.");
            }

            tim.DangSuDung = dangSuDung;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return KetQua.Loi(LoiLuu);
            }

            return KetQua.Ok(dangSuDung ? $"Đã dùng lại {tim.TenCa}." : $"Đã ngừng sử dụng {tim.TenCa}.");
        }
        catch (Exception)
        {
            return KetQua.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<CaLamViecDto?>> XacDinhCaAsync(TimeSpan gioHen)
    {
        var kq = await LayDanhSachCaAsync();
        return kq.ThanhCong
            ? KetQua<CaLamViecDto?>.Ok(QuyTacCauHinh.XacDinhCa(kq.DuLieu!, gioHen))
            : KetQua<CaLamViecDto?>.Loi(kq.ThongBao);
    }

    // ---------- Hàm phụ ----------

    private static async Task<List<CaLamViecDto>> DocCacCaAsync(SpaDbContext db) =>
        (await db.CaLamViec.AsNoTracking().OrderBy(x => x.GioBatDau).ThenBy(x => x.MaCa).ToListAsync())
            .Select(ToDto).ToList();

    private static CaLamViecDto ToDto(CaLamViec x) =>
        new(x.MaCa, x.TenCa, x.GioBatDau, x.GioKetThuc, x.PhutVuotChoPhep, x.SoLeTan, x.SoGroomer, x.DangSuDung);

    private static void GanCa(CaLamViec dich, CaLamViecDto nguon)
    {
        dich.TenCa = nguon.TenCa;
        dich.GioBatDau = nguon.GioBatDau;
        dich.GioKetThuc = nguon.GioKetThuc;
        dich.PhutVuotChoPhep = nguon.PhutVuotChoPhep;
        dich.SoLeTan = nguon.SoLeTan;
        dich.SoGroomer = nguon.SoGroomer;
    }

    private static int DocSo(DinhNghiaThongSo ts, string? giaTri) =>
        int.TryParse(giaTri, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && QuyTacCauHinh.KiemTraSo(ts, v) is null
            ? v
            : ts.MacDinh;

    private static void Gan(SpaDbContext db, Dictionary<string, CauHinh> dangCo, string ma, string giaTri, string moTa)
    {
        if (dangCo.TryGetValue(ma, out var dong))
        {
            dong.GiaTri = giaTri;
            return;
        }
        db.CauHinh.Add(new CauHinh { MaCauHinh = ma, GiaTri = giaTri, MoTa = moTa });
    }

    /// <summary>Tạo thư mục nếu chưa có và thử ghi một tệp tạm; trả lỗi nếu không được.</summary>
    private static string? KiemTraGhiThuMuc(string thuMuc)
    {
        var duongDan = thuMuc;
        try
        {
            duongDan = QuyTacCauHinh.DuongDanDayDu(thuMuc);
            Directory.CreateDirectory(duongDan);
            var thu = Path.Combine(duongDan, $".kiem-tra-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(thu, string.Empty);
            File.Delete(thu);
            return null;
        }
        catch (Exception)
        {
            return $"Không tạo hoặc ghi được vào thư mục {duongDan}. Chọn thư mục khác.";
        }
    }
}
