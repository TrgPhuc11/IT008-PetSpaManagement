using System.Data;
using Microsoft.EntityFrameworkCore;
using SpaThuCung.Data;
using SpaThuCung.Models;

namespace SpaThuCung.Services;

public class TheoDoiService : ITheoDoiService
{
    private const string LoiKetNoi = "Không kết nối được cơ sở dữ liệu.";
    private const string LoiChuaDangNhap = "Phiên làm việc đã hết, hãy đăng nhập lại.";
    private const string LoiLuu = "Không lưu được, có thể dữ liệu vừa thay đổi ở máy khác. Hãy tải lại và thử lại.";
    private const string LoiKhongThayPhieu = "Không tìm thấy phiếu, hãy tải lại.";

    private readonly IDbContextFactory<SpaDbContext> _dbFactory;
    private readonly IPhienLamViec _phien;

    public TheoDoiService(IDbContextFactory<SpaDbContext> dbFactory, IPhienLamViec phien)
    {
        _dbFactory = dbFactory;
        _phien = phien;
    }

    // ---------- Danh sách ----------

    public async Task<KetQua<List<PhieuTheoDoiDto>>> LayDangOTiemAsync()
    {
        if (!_phien.DaDangNhap) return KetQua<List<PhieuTheoDoiDto>>.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var coMat = new[] { TrangThaiPhieu.DaNhanThuCung, TrangThaiPhieu.DangThucHien };
            var ds = await TruyVanPhieu(db)
                .Where(p => coMat.Contains(p.TrangThai) || (p.TrangThai == TrangThaiPhieu.HoanTat && p.GioTra == null))
                .OrderBy(p => p.NgayGioHen)
                .ToListAsync();
            return KetQua<List<PhieuTheoDoiDto>>.Ok(await TaoDtoAsync(db, ds));
        }
        catch (Exception)
        {
            return KetQua<List<PhieuTheoDoiDto>>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<List<PhieuTheoDoiDto>>> LayLichHenAsync(DateTime ngay, string? trangThai)
    {
        if (!_phien.DaDangNhap) return KetQua<List<PhieuTheoDoiDto>>.Loi(LoiChuaDangNhap);
        if (trangThai is not null && !TrangThaiPhieu.TatCa.Contains(trangThai))
            return KetQua<List<PhieuTheoDoiDto>>.Loi("Trạng thái không hợp lệ.");

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var tu = ngay.Date;
            var den = tu.AddDays(1);
            var truyVan = TruyVanPhieu(db).Where(p => p.NgayGioHen >= tu && p.NgayGioHen < den);
            if (trangThai is not null) truyVan = truyVan.Where(p => p.TrangThai == trangThai);
            var ds = await truyVan.OrderBy(p => p.NgayGioHen).ThenBy(p => p.MaPhieu).ToListAsync();
            return KetQua<List<PhieuTheoDoiDto>>.Ok(await TaoDtoAsync(db, ds));
        }
        catch (Exception)
        {
            return KetQua<List<PhieuTheoDoiDto>>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<List<LongTinhTrangDto>>> LayTinhTrangLongAsync()
    {
        if (!_phien.DaDangNhap) return KetQua<List<LongTinhTrangDto>>.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var giuLong = QuyTacPhieu.TrangThaiGiuLong;
            var dangGiu = await db.PhieuDichVu.AsNoTracking()
                .Where(p => p.MaLong != null
                            && (giuLong.Contains(p.TrangThai) || (p.TrangThai == TrangThaiPhieu.HoanTat && p.GioTra == null)))
                .Select(p => new { MaLong = p.MaLong!.Value, p.ThuCung!.Ten })
                .ToListAsync();
            var tenTheoLong = dangGiu.GroupBy(x => x.MaLong).ToDictionary(g => g.Key, g => g.First().Ten);

            var cacLong = await db.Long.AsNoTracking().OrderBy(l => l.TenLong).ToListAsync();
            var ds = cacLong
                .Select(l => tenTheoLong.TryGetValue(l.MaLong, out var ten)
                    ? new LongTinhTrangDto(l.MaLong, l.TenLong, l.LoaiLong, false, ten)
                    : new LongTinhTrangDto(l.MaLong, l.TenLong, l.LoaiLong, true, null))
                .OrderByDescending(x => x.Trong) // lồng trống đứng trước
                .ThenBy(x => x.TenLong)
                .ToList();
            return KetQua<List<LongTinhTrangDto>>.Ok(ds);
        }
        catch (Exception)
        {
            return KetQua<List<LongTinhTrangDto>>.Loi(LoiKetNoi);
        }
    }

    // ---------- Nhận thú cưng ----------

    public async Task<KetQua<NhanThongTinDto>> LayThongTinNhanAsync(int maPhieu)
    {
        if (!_phien.DaDangNhap) return KetQua<NhanThongTinDto>.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var p = await TruyVanPhieu(db).Include(x => x.ChiTiet).ThenInclude(c => c.DichVu)
                .FirstOrDefaultAsync(x => x.MaPhieu == maPhieu);
            if (p is null) return KetQua<NhanThongTinDto>.Loi(LoiKhongThayPhieu);

            var loi = KiemTraDuocNhan(p);
            if (loi is not null) return KetQua<NhanThongTinDto>.Loi(loi);

            var (ca, loiCa) = await DocCaNhanAsync(db, p);
            if (ca is null) return KetQua<NhanThongTinDto>.Loi(loiCa!);

            var datLich = await PhieuService.DocLongTrongAsync(db, LoaiLong.DatLich);
            var walkIn = await PhieuService.DocLongTrongAsync(db, LoaiLong.WalkIn);
            var dong = p.ChiTiet.OrderBy(c => c.DichVu!.TenDv)
                .Select(c => new DongNhanDto(c.MaDv, c.DichVu!.TenDv, c.DonGia, c.ThoiLuong, c.GhiChu))
                .ToList();

            var dto = (await TaoDtoAsync(db, new List<PhieuDichVu> { p }))[0];
            return KetQua<NhanThongTinDto>.Ok(new NhanThongTinDto(dto, p.CanNangLucLap, dong, ca, datLich, walkIn));
        }
        catch (Exception)
        {
            return KetQua<NhanThongTinDto>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<List<DongGiaMoiDto>>> TraGiaLaiAsync(int maPhieu, decimal canNang)
    {
        if (!_phien.DaDangNhap) return KetQua<List<DongGiaMoiDto>>.Loi(LoiChuaDangNhap);

        var loiCan = QuyTacKhachHang.KiemTraCanNang(canNang);
        if (loiCan is not null) return KetQua<List<DongGiaMoiDto>>.Loi(loiCan);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var p = await db.PhieuDichVu.AsNoTracking().Include(x => x.ThuCung).Include(x => x.ChiTiet)
                .FirstOrDefaultAsync(x => x.MaPhieu == maPhieu);
            if (p is null) return KetQua<List<DongGiaMoiDto>>.Loi(LoiKhongThayPhieu);

            var loai = p.ThuCung!.Loai;
            var cacMaDv = p.ChiTiet.Select(c => c.MaDv).ToList();
            var cacDv = await db.DichVu.AsNoTracking()
                .Where(x => cacMaDv.Contains(x.MaDv))
                .Include(x => x.BangGia.Where(g => g.Loai == loai))
                .ToDictionaryAsync(x => x.MaDv);

            var ds = new List<DongGiaMoiDto>();
            foreach (var c in p.ChiTiet)
            {
                var g = cacDv.TryGetValue(c.MaDv, out var dv) ? QuyTacDichVu.TimDongGia(dv.BangGia, loai, canNang) : null;
                ds.Add(g is null
                    ? new DongGiaMoiDto(c.MaDv, false, 0, 0)
                    : new DongGiaMoiDto(c.MaDv, true, g.Gia, g.ThoiLuong));
            }
            return KetQua<List<DongGiaMoiDto>>.Ok(ds);
        }
        catch (Exception)
        {
            return KetQua<List<DongGiaMoiDto>>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua> NhanThuCungAsync(NhanThuCungMoi yeuCau)
    {
        var nv = _phien.NhanVienHienTai;
        if (!_phien.DaDangNhap || nv is null) return KetQua.Loi(LoiChuaDangNhap);

        var loiCan = QuyTacKhachHang.KiemTraCanNang(yeuCau.CanNang);
        if (loiCan is not null) return KetQua.Loi(loiCan);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var gd = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var p = await db.PhieuDichVu
                .Include(x => x.ThuCung).Include(x => x.ChiTiet).ThenInclude(c => c.DichVu)
                .Include(x => x.Groomer)
                .FirstOrDefaultAsync(x => x.MaPhieu == yeuCau.MaPhieu);
            if (p is null) return KetQua.Loi(LoiKhongThayPhieu);
            var loi = KiemTraDuocNhan(p);
            if (loi is not null) return KetQua.Loi(loi);

            var bayGio = QuyTacPhieu.CatGiay(DateTime.Now);

            // BR10: Groomer có lịch đúng ngày, đúng ca.
            var (ca, loiCa) = await DocCaNhanAsync(db, p);
            if (ca is null) return KetQua.Loi(loiCa!);
            if (ca.Groomer.Count == 0) return KetQua.Loi($"{ca.Ca.TenCa} hôm nay chưa có Groomer trong lịch làm việc, chưa nhận được.");
            var groomer = ca.Groomer.FirstOrDefault(x => x.MaNv == yeuCau.MaGroomer);
            if (groomer is null) return KetQua.Loi($"Groomer đã chọn không có lịch {ca.Ca.TenCa} hôm nay, hãy chọn lại.");

            // BR14: tra lại giá theo cân nặng thực tế; dòng không có khoảng phù hợp phải nhập tay kèm ghi chú (BR08).
            var cacMaDv = p.ChiTiet.Select(c => c.MaDv).ToList();
            if (yeuCau.Dong.Count != cacMaDv.Count || yeuCau.Dong.Select(d => d.MaDv).Except(cacMaDv).Any()
                || yeuCau.Dong.Select(d => d.MaDv).Distinct().Count() != cacMaDv.Count)
                return KetQua.Loi("Danh sách dịch vụ không khớp với phiếu, hãy mở lại form nhận.");

            var loai = p.ThuCung!.Loai;
            var cacDv = await db.DichVu.AsNoTracking()
                .Where(x => cacMaDv.Contains(x.MaDv))
                .Include(x => x.BangGia.Where(g => g.Loai == loai))
                .ToDictionaryAsync(x => x.MaDv);

            var nhatKy = new List<NhatKyPhieu>();
            foreach (var d in yeuCau.Dong)
            {
                var c = p.ChiTiet.First(x => x.MaDv == d.MaDv);
                var ten = c.DichVu!.TenDv;
                decimal donGia;
                int thoiLuong;
                string? ghiChu;
                if (d.NhapTay)
                {
                    var l = QuyTacPhieu.KiemTraNhapTay(d.DonGia, d.ThoiLuong, d.GhiChu);
                    if (l is not null) return KetQua.Loi($"{ten}: {l}");
                    donGia = d.DonGia!.Value;
                    thoiLuong = d.ThoiLuong!.Value;
                    ghiChu = d.GhiChu!.Trim();
                }
                else
                {
                    var g = QuyTacDichVu.TimDongGia(cacDv[d.MaDv].BangGia, loai, yeuCau.CanNang);
                    if (g is null)
                        return KetQua.Loi($"{ten} chưa có giá cho {loai} {QuyTacDichVu.KgHienThi(yeuCau.CanNang)} kg, hãy nhập tay kèm ghi chú.");
                    donGia = g.Gia;
                    thoiLuong = g.ThoiLuong;
                    ghiChu = null;
                }

                if (d.NhapTay && (c.DonGia != donGia || c.ThoiLuong != thoiLuong || c.GhiChu != ghiChu))
                    nhatKy.Add(TaoNhatKy(nv.MaNv, bayGio, HanhDongNhatKy.NhapTayGia,
                        $"{ten}: {QuyTacDichVu.TienHienThi(donGia)} đ, {thoiLuong} phút. Lý do: {ghiChu}"));
                c.DonGia = donGia;
                c.ThoiLuong = thoiLuong;
                c.GhiChu = ghiChu;
            }

            var tongCu = p.TongTien;
            var tongTien = p.ChiTiet.Sum(c => c.DonGia);
            var tongPhut = p.ChiTiet.Sum(c => c.ThoiLuong);
            if (tongTien > QuyTacDichVu.GiaToiDa) return KetQua.Loi("Tổng tiền vượt giới hạn cho phép.");
            if (tongTien != tongCu && !yeuCau.KhachDongY)
                return KetQua.Loi($"Tổng tiền đổi từ {QuyTacDichVu.TienHienThi(tongCu)} đ thành {QuyTacDichVu.TienHienThi(tongTien)} đ. "
                                  + "Hãy báo khách và tick \"khách đồng ý chi phí mới\".");

            // BR13: phiếu đặt lịch dùng lồng Đặt lịch trống; hết lồng Đặt lịch mới được mượn lồng Walk-in trống.
            var longDatLich = await PhieuService.DocLongTrongAsync(db, LoaiLong.DatLich);
            var longWalkIn = await PhieuService.DocLongTrongAsync(db, LoaiLong.WalkIn);
            var duocChon = longDatLich.Count > 0 ? longDatLich : longWalkIn;
            if (duocChon.Count == 0) return KetQua.Loi("Đã hết cả lồng Đặt lịch lẫn lồng Walk-in trống, chưa nhận được thú cưng.");
            var longMoi = duocChon.FirstOrDefault(x => x.MaLong == yeuCau.MaLong);
            if (longMoi is null)
                return KetQua.Loi(longDatLich.Count > 0
                    ? "Lồng đã chọn không còn trống (còn lồng Đặt lịch trống thì chưa dùng lồng Walk-in), hãy chọn lại."
                    : "Lồng đã chọn không còn trống, hãy chọn lại.");

            var canCu = p.CanNangLucLap;
            var groomerCu = p.Groomer?.HoTen;
            var daDoiGroomer = p.MaGroomer != groomer.MaNv;
            if (canCu != yeuCau.CanNang)
                nhatKy.Add(TaoNhatKy(nv.MaNv, bayGio, HanhDongNhatKy.SuaCanNang,
                    $"{QuyTacDichVu.KgHienThi(canCu)} → {QuyTacDichVu.KgHienThi(yeuCau.CanNang)} kg"));
            if (daDoiGroomer)
                nhatKy.Add(TaoNhatKy(nv.MaNv, bayGio, HanhDongNhatKy.DoiGroomer,
                    $"{groomerCu ?? "Chưa phân công"} → {groomer.HoTen}"));
            nhatKy.Insert(0, TaoNhatKy(nv.MaNv, bayGio, HanhDongNhatKy.NhanThuCung,
                $"Cân {QuyTacDichVu.KgHienThi(yeuCau.CanNang)} kg, lồng {longMoi.TenLong}, Groomer {groomer.HoTen}, "
                + $"tổng {QuyTacDichVu.TienHienThi(tongTien)} đ"
                + (tongTien != tongCu ? $" (trước {QuyTacDichVu.TienHienThi(tongCu)} đ, khách đồng ý)" : string.Empty) + "."));

            p.CanNangLucLap = yeuCau.CanNang;
            p.ThuCung!.CanNang = yeuCau.CanNang;
            p.MaGroomer = groomer.MaNv;
            p.MaLong = longMoi.MaLong;
            p.TongTien = tongTien;
            p.TongThoiGian = tongPhut;
            p.GioNhan = bayGio; // giờ hẹn gốc giữ nguyên
            p.TrangThai = TrangThaiPhieu.DaNhanThuCung;
            foreach (var k in nhatKy) p.NhatKy.Add(k);

            await db.SaveChangesAsync();
            await gd.CommitAsync();

            var tb = $"Đã nhận {p.ThuCung.Ten} ({QuyTacPhieu.MaHienThi(p.MaPhieu)}), lồng {longMoi.TenLong}, Groomer {groomer.HoTen}.";
            var canhBao = QuyTacPhieu.CanhBaoQuaTai(ca.Ca, bayGio, tongPhut, null);
            if (canhBao.Count > 0) tb += " Lưu ý: " + string.Join(" ", canhBao);
            return KetQua.Ok(tb);
        }
        catch (DbUpdateException)
        {
            return KetQua.Loi(LoiLuu);
        }
        catch (InvalidOperationException)
        {
            return KetQua.Loi(LoiLuu);
        }
        catch (Exception)
        {
            return KetQua.Loi(LoiKetNoi);
        }
    }

    // ---------- Chuyển trạng thái ----------

    public async Task<KetQua> BatDauChamSocAsync(int maPhieu)
    {
        if (!_phien.DaDangNhap) return KetQua.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var gd = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var p = await db.PhieuDichVu.Include(x => x.ThuCung).Include(x => x.Groomer)
                .FirstOrDefaultAsync(x => x.MaPhieu == maPhieu);
            if (p is null) return KetQua.Loi(LoiKhongThayPhieu);
            if (p.TrangThai != TrangThaiPhieu.DaNhanThuCung)
                return KetQua.Loi($"Phiếu đang ở trạng thái \"{p.TrangThai}\", chỉ bắt đầu chăm sóc phiếu Đã nhận thú cưng.");

            // BR16: có Groomer và Groomer không có phiếu khác đang thực hiện.
            if (p.MaGroomer is null || p.Groomer is null)
                return KetQua.Loi("Phiếu chưa có Groomer. Hãy chọn Groomer trước khi bắt đầu chăm sóc.");
            var dangLam = await db.PhieuDichVu.AsNoTracking()
                .Where(x => x.MaGroomer == p.MaGroomer && x.MaPhieu != p.MaPhieu && x.TrangThai == TrangThaiPhieu.DangThucHien)
                .Select(x => new { x.MaPhieu, x.ThuCung!.Ten })
                .FirstOrDefaultAsync();
            if (dangLam is not null)
                return KetQua.Loi($"{p.Groomer.HoTen} đang chăm sóc {dangLam.Ten} ({QuyTacPhieu.MaHienThi(dangLam.MaPhieu)}). "
                                  + "Hãy hoàn tất phiếu đó trước, hoặc đổi sang Groomer khác.");

            // BR14: Groomer phải đã vào ca (có lịch hôm nay đúng ca hiện tại).
            var bayGio = QuyTacPhieu.CatGiay(DateTime.Now);
            var (ca, loiCa) = await PhieuService.DocCaVaGroomerAsync(db, bayGio);
            if (ca is null) return KetQua.Loi($"Bây giờ ngoài giờ làm việc nên chưa bắt đầu chăm sóc được. {loiCa}");
            if (ca.Groomer.All(x => x.MaNv != p.MaGroomer))
                return KetQua.Loi($"{p.Groomer.HoTen} chưa vào ca ({ca.Ca.TenCa} hôm nay không có lịch của Groomer này). "
                                  + "Hãy chờ Groomer vào ca hoặc đổi sang Groomer khác.");

            // BR14: khách đến sớm chỉ được chăm sóc sớm khi không còn phiếu hẹn sớm hơn của Groomer đang chờ.
            if (bayGio < p.NgayGioHen)
            {
                var truoc = await db.PhieuDichVu.AsNoTracking()
                    .Where(x => x.MaGroomer == p.MaGroomer && x.MaPhieu != p.MaPhieu && x.NgayGioHen < p.NgayGioHen
                                && x.NgayGioHen >= bayGio.Date
                                && (x.TrangThai == TrangThaiPhieu.DaDatLich || x.TrangThai == TrangThaiPhieu.DaNhanThuCung))
                    .OrderBy(x => x.NgayGioHen)
                    .Select(x => new { x.MaPhieu, x.NgayGioHen })
                    .FirstOrDefaultAsync();
                if (truoc is not null)
                    return KetQua.Loi($"Khách đến sớm hơn giờ hẹn ({QuyTacPhieu.Gio(p.NgayGioHen)}) và {p.Groomer.HoTen} còn phiếu hẹn sớm hơn đang chờ "
                                      + $"({QuyTacPhieu.MaHienThi(truoc.MaPhieu)}, {QuyTacPhieu.Gio(truoc.NgayGioHen)}). "
                                      + "Thú cưng chờ trong lồng tới lượt, hoặc đổi sang Groomer khác.");
            }

            p.TrangThai = TrangThaiPhieu.DangThucHien;
            p.GioBatDau = bayGio;
            await db.SaveChangesAsync();
            await gd.CommitAsync();

            var xong = bayGio.AddMinutes(p.TongThoiGian);
            return KetQua.Ok($"{p.ThuCung!.Ten} bắt đầu chăm sóc lúc {QuyTacPhieu.Gio(bayGio)}, dự kiến xong {QuyTacPhieu.Gio(xong)}.");
        }
        catch (DbUpdateException)
        {
            return KetQua.Loi(LoiLuu);
        }
        catch (InvalidOperationException)
        {
            return KetQua.Loi(LoiLuu);
        }
        catch (Exception)
        {
            return KetQua.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua> HoanTatChamSocAsync(int maPhieu)
    {
        if (!_phien.DaDangNhap) return KetQua.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var p = await db.PhieuDichVu.Include(x => x.ThuCung).FirstOrDefaultAsync(x => x.MaPhieu == maPhieu);
            if (p is null) return KetQua.Loi(LoiKhongThayPhieu);
            if (p.TrangThai != TrangThaiPhieu.DangThucHien)
                return KetQua.Loi($"Phiếu đang ở trạng thái \"{p.TrangThai}\", chỉ hoàn tất phiếu đang thực hiện chăm sóc.");

            p.TrangThai = TrangThaiPhieu.HoanTat;
            await db.SaveChangesAsync();
            return KetQua.Ok($"Đã hoàn tất chăm sóc {p.ThuCung!.Ten}. Thú cưng ở lại lồng chờ khách đón.");
        }
        catch (DbUpdateException)
        {
            return KetQua.Loi(LoiLuu);
        }
        catch (Exception)
        {
            return KetQua.Loi(LoiKetNoi);
        }
    }

    // ---------- Đổi lồng ----------

    public async Task<KetQua<List<LongTrongDto>>> LayLongDeDoiAsync(int maPhieu)
    {
        if (!_phien.DaDangNhap) return KetQua<List<LongTrongDto>>.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var p = await db.PhieuDichVu.AsNoTracking().FirstOrDefaultAsync(x => x.MaPhieu == maPhieu);
            if (p is null) return KetQua<List<LongTrongDto>>.Loi(LoiKhongThayPhieu);
            return KetQua<List<LongTrongDto>>.Ok(await LongDoiDuocAsync(db, p.NguonTiepNhan));
        }
        catch (Exception)
        {
            return KetQua<List<LongTrongDto>>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua> DoiLongAsync(int maPhieu, int maLongMoi)
    {
        var nv = _phien.NhanVienHienTai;
        if (!_phien.DaDangNhap || nv is null) return KetQua.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var gd = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var p = await db.PhieuDichVu.Include(x => x.ThuCung).Include(x => x.Long)
                .FirstOrDefaultAsync(x => x.MaPhieu == maPhieu);
            if (p is null) return KetQua.Loi(LoiKhongThayPhieu);
            var dangGiu = p.TrangThai is TrangThaiPhieu.DaNhanThuCung or TrangThaiPhieu.DangThucHien
                          || (p.TrangThai == TrangThaiPhieu.HoanTat && p.GioTra is null);
            if (!dangGiu || p.MaLong is null) return KetQua.Loi("Phiếu này không đang giữ lồng nên không đổi lồng được.");

            var trong = await LongDoiDuocAsync(db, p.NguonTiepNhan);
            var moi = trong.FirstOrDefault(x => x.MaLong == maLongMoi);
            if (moi is null) return KetQua.Loi("Lồng đã chọn không còn trống hoặc không dùng được cho phiếu này, hãy chọn lại.");

            var cu = p.Long?.TenLong ?? "—";
            var bayGio = QuyTacPhieu.CatGiay(DateTime.Now);
            p.MaLong = moi.MaLong;
            p.NhatKy.Add(TaoNhatKy(nv.MaNv, bayGio, HanhDongNhatKy.DoiLong, $"{cu} → {moi.TenLong}"));
            await db.SaveChangesAsync();
            await gd.CommitAsync();
            return KetQua.Ok($"Đã chuyển {p.ThuCung!.Ten} từ lồng {cu} sang lồng {moi.TenLong}.");
        }
        catch (DbUpdateException)
        {
            return KetQua.Loi(LoiLuu);
        }
        catch (InvalidOperationException)
        {
            return KetQua.Loi(LoiLuu);
        }
        catch (Exception)
        {
            return KetQua.Loi(LoiKetNoi);
        }
    }

    // ---------- Hàm phụ ----------

    private static IQueryable<PhieuDichVu> TruyVanPhieu(SpaDbContext db) =>
        db.PhieuDichVu.AsNoTracking()
            .Include(p => p.ThuCung).ThenInclude(t => t!.KhachHang)
            .Include(p => p.Groomer)
            .Include(p => p.Long);

    private static NhatKyPhieu TaoNhatKy(int maNv, DateTime luc, string hanhDong, string noiDung) => new()
    {
        MaNv = maNv,
        ThoiDiem = luc,
        HanhDong = hanhDong,
        NoiDung = noiDung.Length > 500 ? noiDung[..500] : noiDung,
    };

    /// <summary>Phiếu Đã đặt lịch, đến đúng ngày hẹn mới nhận được; trả lỗi bằng chữ nếu chưa được.</summary>
    private static string? KiemTraDuocNhan(PhieuDichVu p)
    {
        if (p.TrangThai != TrangThaiPhieu.DaDatLich)
            return $"Phiếu đang ở trạng thái \"{p.TrangThai}\", chỉ nhận thú cưng của phiếu Đã đặt lịch.";
        if (p.NgayGioHen.Date != DateTime.Today)
            return $"Phiếu hẹn ngày {p.NgayGioHen:dd/MM/yyyy}, chỉ nhận thú cưng đúng ngày hẹn. "
                   + "Cần đổi ngày thì dời lịch (sẽ có ở đợt sau).";
        return null;
    }

    /// <summary>
    /// Ca và Groomer dùng khi nhận (BR10): ca đang diễn ra bây giờ; nếu bây giờ ngoài mọi ca (đến sớm trước giờ mở cửa)
    /// thì dùng ca của giờ hẹn trong hôm nay.
    /// </summary>
    private static async Task<(CaCuaGioHenDto? KetQua, string? Loi)> DocCaNhanAsync(SpaDbContext db, PhieuDichVu p)
    {
        var bayGio = QuyTacPhieu.CatGiay(DateTime.Now);
        var (ca, loi) = await PhieuService.DocCaVaGroomerAsync(db, bayGio);
        if (ca is not null) return (ca, null);

        var theoGioHen = bayGio.Date + p.NgayGioHen.TimeOfDay;
        var (ca2, loi2) = await PhieuService.DocCaVaGroomerAsync(db, theoGioHen);
        return ca2 is not null ? (ca2, null) : (null, loi ?? loi2);
    }

    /// <summary>Lồng trống đổi sang được: Walk-in chỉ lồng Walk-in; đặt lịch lồng Đặt lịch trước rồi Walk-in (BR13).</summary>
    private static async Task<List<LongTrongDto>> LongDoiDuocAsync(SpaDbContext db, string nguonTiepNhan)
    {
        var walkIn = await PhieuService.DocLongTrongAsync(db, LoaiLong.WalkIn);
        if (nguonTiepNhan == NguonTiepNhan.WalkIn) return walkIn;
        var datLich = await PhieuService.DocLongTrongAsync(db, LoaiLong.DatLich);
        return datLich.Concat(walkIn).ToList();
    }

    private static async Task<List<PhieuTheoDoiDto>> TaoDtoAsync(SpaDbContext db, List<PhieuDichVu> ds)
    {
        // Các phiếu cùng Groomer để tính lúc Groomer rảnh (BR14).
        var cacGroomer = ds.Where(p => p.MaGroomer != null).Select(p => p.MaGroomer!.Value).Distinct().ToList();
        var chuaXong = new[] { TrangThaiPhieu.DaDatLich, TrangThaiPhieu.DaNhanThuCung, TrangThaiPhieu.DangThucHien };
        var lienQuan = cacGroomer.Count == 0
            ? new List<PhieuDichVu>()
            : await db.PhieuDichVu.AsNoTracking()
                .Where(p => p.MaGroomer != null && cacGroomer.Contains(p.MaGroomer.Value) && chuaXong.Contains(p.TrangThai))
                .ToListAsync();

        return ds.Select(p => new PhieuTheoDoiDto(
            p.MaPhieu, p.MaThuCung, p.ThuCung!.Ten, p.ThuCung.Loai,
            p.ThuCung.KhachHang?.HoTen ?? string.Empty, p.ThuCung.KhachHang?.Sdt ?? string.Empty,
            p.TrangThai, p.NguonTiepNhan, p.NgayGioHen, p.GioNhan, p.GioBatDau, p.GioTra,
            p.MaLong, p.Long?.TenLong, p.MaGroomer, p.Groomer?.HoTen,
            p.TongTien, p.TongThoiGian, p.TtThanhToan, GioXongDuKien(p, lienQuan), p.LyDoHuy)).ToList();
    }

    /// <summary>
    /// Giờ kết thúc dự kiến (BR14): chưa nhận = giờ hẹn + tổng thời lượng; đã nhận chưa bắt đầu = mốc muộn hơn giữa giờ nhận và
    /// lúc Groomer rảnh + tổng thời lượng; đang thực hiện = giờ bắt đầu + tổng thời lượng. Trạng thái khác thì không có.
    /// Lúc Groomer rảnh là mốc xong của phiếu đang thực hiện và các phiếu hẹn sớm hơn đã nhận đang chờ (ước tính).
    /// </summary>
    private static DateTime? GioXongDuKien(PhieuDichVu p, IReadOnlyList<PhieuDichVu> lienQuan)
    {
        var tong = TimeSpan.FromMinutes(p.TongThoiGian);
        switch (p.TrangThai)
        {
            case TrangThaiPhieu.DaDatLich:
                return p.NgayGioHen + tong;
            case TrangThaiPhieu.DangThucHien:
                return (p.GioBatDau ?? p.GioNhan ?? p.NgayGioHen) + tong;
            case TrangThaiPhieu.DaNhanThuCung:
                var rai = p.GioNhan ?? p.NgayGioHen;
                foreach (var q in lienQuan.Where(x => x.MaGroomer == p.MaGroomer && x.MaPhieu != p.MaPhieu))
                {
                    DateTime? xong = null;
                    if (q.TrangThai == TrangThaiPhieu.DangThucHien)
                        xong = (q.GioBatDau ?? q.GioNhan ?? q.NgayGioHen) + TimeSpan.FromMinutes(q.TongThoiGian);
                    else if (q.TrangThai == TrangThaiPhieu.DaNhanThuCung && q.NgayGioHen < p.NgayGioHen)
                        xong = (q.GioNhan ?? q.NgayGioHen) + TimeSpan.FromMinutes(q.TongThoiGian);
                    if (xong > rai) rai = xong.Value;
                }
                return rai + tong;
            default:
                return null;
        }
    }
}
