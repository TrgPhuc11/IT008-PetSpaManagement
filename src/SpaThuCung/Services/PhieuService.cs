using System.Data;
using Microsoft.EntityFrameworkCore;
using SpaThuCung.Data;
using SpaThuCung.Models;

namespace SpaThuCung.Services;

public class PhieuService : IPhieuService
{
    private const string LoiKetNoi = "Không kết nối được cơ sở dữ liệu.";
    private const string LoiChuaDangNhap = "Phiên làm việc đã hết, hãy đăng nhập lại.";
    private const string LoiLuu = "Không lưu được phiếu, có thể dữ liệu vừa thay đổi ở máy khác. Hãy thử lại.";

    private readonly IDbContextFactory<SpaDbContext> _dbFactory;
    private readonly IPhienLamViec _phien;

    public PhieuService(IDbContextFactory<SpaDbContext> dbFactory, IPhienLamViec phien)
    {
        _dbFactory = dbFactory;
        _phien = phien;
    }

    // ---------- Tra cứu cho form ----------

    public async Task<KetQua<CaCuaGioHenDto>> LayCaVaGroomerAsync(DateTime gioHen)
    {
        if (!_phien.DaDangNhap) return KetQua<CaCuaGioHenDto>.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var (kq, loi) = await DocCaVaGroomerAsync(db, QuyTacPhieu.CatGiay(gioHen));
            return kq is null ? KetQua<CaCuaGioHenDto>.Loi(loi!) : KetQua<CaCuaGioHenDto>.Ok(kq);
        }
        catch (Exception)
        {
            return KetQua<CaCuaGioHenDto>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<List<LongTrongDto>>> LayLongTrongAsync(string loaiLong)
    {
        if (!_phien.DaDangNhap) return KetQua<List<LongTrongDto>>.Loi(LoiChuaDangNhap);
        if (!LoaiLong.TatCa.Contains(loaiLong)) return KetQua<List<LongTrongDto>>.Loi("Loại lồng không hợp lệ.");

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return KetQua<List<LongTrongDto>>.Ok(await DocLongTrongAsync(db, loaiLong));
        }
        catch (Exception)
        {
            return KetQua<List<LongTrongDto>>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<string?>> TimPhieuDangHoatDongAsync(int maThuCung)
    {
        if (!_phien.DaDangNhap) return KetQua<string?>.Loi(LoiChuaDangNhap);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return KetQua<string?>.Ok(await MoTaPhieuDangHoatDongAsync(db, maThuCung));
        }
        catch (Exception)
        {
            return KetQua<string?>.Loi(LoiKetNoi);
        }
    }

    // ---------- Lập phiếu ----------

    public async Task<KetQua<KetQuaLapPhieu>> LapPhieuAsync(PhieuMoi phieu)
    {
        var nv = _phien.NhanVienHienTai;
        if (!_phien.DaDangNhap || nv is null) return KetQua<KetQuaLapPhieu>.Loi(LoiChuaDangNhap);

        if (!NguonTiepNhan.TatCa.Contains(phieu.NguonTiepNhan))
            return KetQua<KetQuaLapPhieu>.Loi("Chọn nguồn tiếp nhận Đặt lịch hoặc Walk-in.");
        var laWalkIn = phieu.NguonTiepNhan == NguonTiepNhan.WalkIn;

        var loiCan = QuyTacKhachHang.KiemTraCanNang(phieu.CanNang);
        if (loiCan is not null) return KetQua<KetQuaLapPhieu>.Loi(loiCan);

        if (phieu.DichVu.Count == 0) return KetQua<KetQuaLapPhieu>.Loi("Chọn ít nhất một dịch vụ.");
        if (phieu.DichVu.Select(x => x.MaDv).Distinct().Count() != phieu.DichVu.Count)
            return KetQua<KetQuaLapPhieu>.Loi("Một dịch vụ chỉ chọn một lần trong phiếu.");

        var bayGio = QuyTacPhieu.CatGiay(DateTime.Now);
        DateTime gioHen;
        if (laWalkIn)
        {
            gioHen = bayGio;
            if (phieu.MaLong is null) return KetQua<KetQuaLapPhieu>.Loi("Chọn lồng Walk-in trống cho thú cưng.");
            if (phieu.MaGroomer is null) return KetQua<KetQuaLapPhieu>.Loi("Chọn Groomer cho phiếu Walk-in.");
        }
        else
        {
            if (phieu.NgayGioHen is null) return KetQua<KetQuaLapPhieu>.Loi("Chọn ngày giờ hẹn.");
            gioHen = QuyTacPhieu.CatGiay(phieu.NgayGioHen.Value);
            if (gioHen <= bayGio) return KetQua<KetQuaLapPhieu>.Loi("Giờ hẹn phải sau thời điểm hiện tại.");
            if (gioHen.Date > bayGio.Date.AddDays(QuyTacPhieu.SoNgayDatTruocToiDa))
                return KetQua<KetQuaLapPhieu>.Loi($"Chỉ đặt lịch trước tối đa {QuyTacPhieu.SoNgayDatTruocToiDa} ngày.");
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var gd = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            // BR04, BR06: thú cưng có chủ, đã xác nhận tiêm phòng, không có phiếu đang hoạt động.
            // Theo dõi thay đổi: cân nặng lúc lập là cân nặng thực tế nên cập nhật luôn vào hồ sơ thú cưng.
            var tc = await db.ThuCung.FirstOrDefaultAsync(x => x.MaThuCung == phieu.MaThuCung);
            if (tc is null) return KetQua<KetQuaLapPhieu>.Loi("Không tìm thấy thú cưng, hãy tải lại.");
            if (!tc.DaTiemPhong)
                return KetQua<KetQuaLapPhieu>.Loi($"{tc.Ten} chưa được xác nhận tiêm phòng nên chưa lập phiếu được.");

            var dangCo = await MoTaPhieuDangHoatDongAsync(db, tc.MaThuCung);
            if (dangCo is not null) return KetQua<KetQuaLapPhieu>.Loi($"{tc.Ten} {dangCo}, không lập thêm phiếu.");

            // BR09, BR10: giờ hẹn thuộc một ca, ca có Groomer, Groomer được chọn có lịch đúng ngày, đúng ca.
            var (caGroomer, loiCa) = await DocCaVaGroomerAsync(db, gioHen);
            if (caGroomer is null) return KetQua<KetQuaLapPhieu>.Loi(loiCa!);
            if (caGroomer.Groomer.Count == 0)
                return KetQua<KetQuaLapPhieu>.Loi(LoiCaKhongCoGroomer(caGroomer));

            GroomerCaDto? groomer = null;
            if (phieu.MaGroomer is int maGroomer)
            {
                groomer = caGroomer.Groomer.FirstOrDefault(x => x.MaNv == maGroomer);
                if (groomer is null)
                    return KetQua<KetQuaLapPhieu>.Loi(
                        $"Groomer đã chọn không có lịch {caGroomer.Ca.TenCa} ngày {gioHen:dd/MM/yyyy}, hãy chọn lại.");
            }

            // BR08: tra giá theo loài và cân nặng lúc lập; dòng không có khoảng phù hợp phải nhập tay kèm ghi chú.
            var cacMaDv = phieu.DichVu.Select(x => x.MaDv).ToList();
            var cacDv = await db.DichVu.AsNoTracking()
                .Where(x => cacMaDv.Contains(x.MaDv))
                .Include(x => x.BangGia.Where(g => g.Loai == tc.Loai))
                .ToDictionaryAsync(x => x.MaDv);

            var chiTiet = new List<ChiTietPhieu>();
            var nhatKy = new List<NhatKyPhieu>();
            foreach (var d in phieu.DichVu)
            {
                if (!cacDv.TryGetValue(d.MaDv, out var dv))
                    return KetQua<KetQuaLapPhieu>.Loi("Có dịch vụ vừa bị xoá, hãy tải lại.");
                if (dv.TrangThai != TrangThaiDichVu.DangCungCap)
                    return KetQua<KetQuaLapPhieu>.Loi($"Dịch vụ {dv.TenDv} đã ngừng cung cấp.");

                if (d.NhapTay)
                {
                    var loi = QuyTacPhieu.KiemTraNhapTay(d.DonGia, d.ThoiLuong, d.GhiChu);
                    if (loi is not null) return KetQua<KetQuaLapPhieu>.Loi($"{dv.TenDv}: {loi}");

                    var ghiChu = d.GhiChu!.Trim();
                    chiTiet.Add(new ChiTietPhieu
                    {
                        MaDv = dv.MaDv, DonGia = d.DonGia!.Value, ThoiLuong = d.ThoiLuong!.Value, GhiChu = ghiChu,
                    });
                    nhatKy.Add(TaoNhatKy(nv.MaNv, bayGio, HanhDongNhatKy.NhapTayGia,
                        $"{dv.TenDv}: {QuyTacDichVu.TienHienThi(d.DonGia.Value)} đ, {d.ThoiLuong} phút. Lý do: {ghiChu}"));
                }
                else
                {
                    var g = QuyTacDichVu.TimDongGia(dv.BangGia, tc.Loai, phieu.CanNang);
                    if (g is null)
                        return KetQua<KetQuaLapPhieu>.Loi(
                            $"{dv.TenDv} chưa có giá cho {tc.Loai} {QuyTacDichVu.KgHienThi(phieu.CanNang)} kg, hãy nhập tay kèm ghi chú.");
                    chiTiet.Add(new ChiTietPhieu { MaDv = dv.MaDv, DonGia = g.Gia, ThoiLuong = g.ThoiLuong });
                }
            }

            var tongTien = chiTiet.Sum(x => x.DonGia);
            var tongPhut = chiTiet.Sum(x => x.ThoiLuong);
            if (tongTien > QuyTacDichVu.GiaToiDa) return KetQua<KetQuaLapPhieu>.Loi("Tổng tiền vượt giới hạn cho phép.");

            // BR13: Walk-in chỉ dùng lồng Walk-in đang trống; hết lồng thì không nhận.
            Long? longGan = null;
            if (laWalkIn)
            {
                var trong = await DocLongTrongAsync(db, LoaiLong.WalkIn);
                if (trong.Count == 0) return KetQua<KetQuaLapPhieu>.Loi("Đã hết lồng Walk-in trống, không nhận được khách Walk-in.");
                if (trong.All(x => x.MaLong != phieu.MaLong))
                    return KetQua<KetQuaLapPhieu>.Loi("Lồng đã chọn không còn trống, hãy chọn lồng khác.");
                longGan = await db.Long.AsNoTracking().FirstAsync(x => x.MaLong == phieu.MaLong);
            }

            var p = new PhieuDichVu
            {
                MaThuCung = tc.MaThuCung,
                MaGroomer = groomer?.MaNv,
                MaNvLap = nv.MaNv,
                MaLong = longGan?.MaLong,
                NguonTiepNhan = phieu.NguonTiepNhan,
                NgayGioHen = gioHen,
                TrangThai = laWalkIn ? TrangThaiPhieu.DaNhanThuCung : TrangThaiPhieu.DaDatLich,
                CanNangLucLap = phieu.CanNang,
                TongTien = tongTien,
                TongThoiGian = tongPhut,
                TtThanhToan = TrangThaiThanhToan.ChuaThanhToan,
                GioNhan = laWalkIn ? bayGio : null,
                NgayTao = bayGio,
                ChiTiet = chiTiet,
            };

            if (laWalkIn)
            {
                // Walk-in: lập phiếu cũng là nhận thú cưng (cân, gán lồng, phân công) nên ghi nhật ký Nhận thú cưng.
                nhatKy.Insert(0, TaoNhatKy(nv.MaNv, bayGio, HanhDongNhatKy.NhanThuCung,
                    $"Walk-in: cân {QuyTacDichVu.KgHienThi(phieu.CanNang)} kg, lồng {longGan!.TenLong}, Groomer {groomer!.HoTen}."));
            }
            p.NhatKy = nhatKy;

            var canCu = tc.CanNang;
            tc.CanNang = phieu.CanNang;

            db.PhieuDichVu.Add(p);
            await db.SaveChangesAsync();
            await gd.CommitAsync();

            var tb = laWalkIn
                ? $"Đã lập phiếu {QuyTacPhieu.MaHienThi(p.MaPhieu)} (Walk-in) cho {tc.Ten}, lồng {longGan!.TenLong}, Groomer {groomer!.HoTen}."
                : $"Đã đặt lịch {QuyTacPhieu.MaHienThi(p.MaPhieu)} cho {tc.Ten} lúc {QuyTacPhieu.NgayGio(gioHen)}"
                  + (groomer is null ? ", chưa phân công Groomer." : $", Groomer {groomer.HoTen}.");
            if (canCu != phieu.CanNang)
                tb += $" Đã cập nhật cân nặng của {tc.Ten}: {QuyTacDichVu.KgHienThi(canCu)} → {QuyTacDichVu.KgHienThi(phieu.CanNang)} kg.";
            return KetQua<KetQuaLapPhieu>.Ok(new KetQuaLapPhieu(p.MaPhieu, p.TrangThai, gioHen, tongTien, tongPhut), tb);
        }
        catch (DbUpdateException)
        {
            return KetQua<KetQuaLapPhieu>.Loi(LoiLuu);
        }
        catch (InvalidOperationException)
        {
            return KetQua<KetQuaLapPhieu>.Loi(LoiLuu);
        }
        catch (Exception)
        {
            return KetQua<KetQuaLapPhieu>.Loi(LoiKetNoi);
        }
    }

    // ---------- Hàm phụ ----------

    private static NhatKyPhieu TaoNhatKy(int maNv, DateTime luc, string hanhDong, string noiDung) => new()
    {
        MaNv = maNv,
        ThoiDiem = luc,
        HanhDong = hanhDong,
        NoiDung = noiDung.Length > 500 ? noiDung[..500] : noiDung,
    };

    private static string LoiCaKhongCoGroomer(CaCuaGioHenDto x) =>
        $"{x.Ca.TenCa} ngày {x.GioHen:dd/MM/yyyy} chưa có Groomer trong lịch làm việc, chưa nhận phiếu được. "
        + "Quản lý cần xếp lịch làm việc trước.";

    /// <summary>Ca chứa giờ hẹn và Groomer của ca; trả (null, lỗi) nếu giờ hẹn ngoài mọi ca đang sử dụng.</summary>
    private static async Task<(CaCuaGioHenDto? KetQua, string? Loi)> DocCaVaGroomerAsync(SpaDbContext db, DateTime gioHen)
    {
        var cacCa = (await db.CaLamViec.AsNoTracking().Where(x => x.DangSuDung).ToListAsync())
            .Select(x => new CaLamViecDto(x.MaCa, x.TenCa, x.GioBatDau, x.GioKetThuc, x.PhutVuotChoPhep,
                x.SoLeTan, x.SoGroomer, x.DangSuDung))
            .ToList();

        var ca = QuyTacCauHinh.XacDinhCa(cacCa, gioHen.TimeOfDay);
        if (ca is null)
        {
            var moCua = QuyTacCauHinh.GioMoCua(cacCa);
            var dongCua = QuyTacCauHinh.GioDongCua(cacCa);
            return (null, moCua is null || dongCua is null
                ? "Chưa có ca làm việc nào đang sử dụng."
                : $"{QuyTacPhieu.Gio(gioHen)} nằm ngoài giờ làm việc ({QuyTacPhieu.Gio(moCua.Value)}–{QuyTacPhieu.Gio(dongCua.Value)}).");
        }

        var ngay = DateOnly.FromDateTime(gioHen);
        var dauCa = gioHen.Date + ca.GioBatDau;
        var cuoiCa = gioHen.Date + ca.GioKetThuc;

        var groomer = await db.LichLamViec.AsNoTracking()
            .Where(x => x.Ngay == ngay && x.MaCa == ca.MaCa && x.ViTri == ViTri.Groomer
                        && x.NhanVien!.TrangThaiLamViec == TrangThaiLamViec.DangLamViec)
            .OrderBy(x => x.NhanVien!.HoTen)
            .Select(x => new
            {
                x.MaNv,
                x.NhanVien!.HoTen,
                Phut = db.PhieuDichVu
                    .Where(p => p.MaGroomer == x.MaNv && p.NgayGioHen >= dauCa && p.NgayGioHen < cuoiCa
                                && p.TrangThai != TrangThaiPhieu.DaHuy && p.TrangThai != TrangThaiPhieu.KhongDen)
                    .Sum(p => (int?)p.TongThoiGian) ?? 0,
            })
            .ToListAsync();

        return (new CaCuaGioHenDto(gioHen, ca, groomer.Select(x => new GroomerCaDto(x.MaNv, x.HoTen, x.Phut)).ToList()), null);
    }

    /// <summary>Lồng của một loại không thuộc phiếu nào đang giữ thú cưng (BR13).</summary>
    private static async Task<List<LongTrongDto>> DocLongTrongAsync(SpaDbContext db, string loaiLong)
    {
        var giuLong = QuyTacPhieu.TrangThaiGiuLong;
        return await db.Long.AsNoTracking()
            .Where(l => l.LoaiLong == loaiLong
                        && !db.PhieuDichVu.Any(p => p.MaLong == l.MaLong
                                                    && (giuLong.Contains(p.TrangThai)
                                                        || (p.TrangThai == TrangThaiPhieu.HoanTat && p.GioTra == null))))
            .OrderBy(l => l.TenLong)
            .Select(l => new LongTrongDto(l.MaLong, l.TenLong, l.LoaiLong))
            .ToListAsync();
    }

    /// <summary>Ví dụ "đang có phiếu PDV0012 (Đã đặt lịch, hẹn 09:00 12/11/2026)"; null nếu không có phiếu đang hoạt động.</summary>
    private static async Task<string?> MoTaPhieuDangHoatDongAsync(SpaDbContext db, int maThuCung)
    {
        var dangHoatDong = QuyTacPhieu.TrangThaiDangHoatDong;
        var p = await db.PhieuDichVu.AsNoTracking()
            .Where(x => x.MaThuCung == maThuCung
                        && (dangHoatDong.Contains(x.TrangThai) || (x.TrangThai == TrangThaiPhieu.HoanTat && x.GioTra == null)))
            .OrderBy(x => x.NgayGioHen)
            .Select(x => new { x.MaPhieu, x.TrangThai, x.NgayGioHen })
            .FirstOrDefaultAsync();
        if (p is null) return null;

        var trangThai = p.TrangThai == TrangThaiPhieu.HoanTat ? "Hoàn tất, chờ khách đón" : p.TrangThai;
        return $"đang có phiếu {QuyTacPhieu.MaHienThi(p.MaPhieu)} ({trangThai}, hẹn {QuyTacPhieu.NgayGio(p.NgayGioHen)})";
    }
}
