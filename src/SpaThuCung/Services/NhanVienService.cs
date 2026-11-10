using System.Data;
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

    public async Task<KetQua<LichKhoangDto>> LayLichKhoangAsync(DateOnly tu, DateOnly den)
    {
        if (!_phien.LaQuanLy) return KetQua<LichKhoangDto>.Loi(LoiQuyen);

        if (den < tu) return KetQua<LichKhoangDto>.Loi("Đến ngày phải sau hoặc bằng Từ ngày.");
        if (den.DayNumber - tu.DayNumber + 1 > QuyTacLichLamViec.SoNgayToiDa)
            return KetQua<LichKhoangDto>.Loi($"Khoảng ngày tối đa {QuyTacLichLamViec.SoNgayToiDa} ngày.");

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var cacDong = await db.LichLamViec.AsNoTracking()
                .Where(x => x.Ngay >= tu && x.Ngay <= den)
                .OrderBy(x => x.Ngay).ThenBy(x => x.ViTri).ThenBy(x => x.NhanVien!.HoTen)
                .Select(x => new DongLichDto(
                    x.MaLich, x.MaNv, x.NhanVien!.HoTen,
                    x.NhanVien.TrangThaiLamViec == TrangThaiLamViec.DaNghiViec,
                    x.Ngay, x.MaCa, x.ViTri))
                .ToListAsync();

            // Ca đang sử dụng, cộng các ca đã ngừng nhưng trong khoảng này còn lịch cũ.
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
                .Select(x => new { x.MaNv, x.HoTen, x.VaiTro })
                .ToListAsync();

            // Vị trí thường làm: vị trí xuất hiện nhiều nhất trong toàn bộ lịch của người đó; bằng nhau thì lấy vị trí gần nhất.
            var thongKe = await db.LichLamViec.AsNoTracking()
                .GroupBy(x => new { x.MaNv, x.ViTri })
                .Select(g => new { g.Key.MaNv, g.Key.ViTri, So = g.Count(), GanNhat = g.Max(x => x.Ngay) })
                .ToListAsync();
            var thuong = thongKe
                .GroupBy(x => x.MaNv)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.So).ThenByDescending(x => x.GanNhat).First().ViTri);

            var ds = nhanVien
                .Select(x => new NhanVienLichDto(x.MaNv, x.HoTen, x.VaiTro,
                    thuong.TryGetValue(x.MaNv, out var vt) ? vt : ViTri.Groomer))
                .ToList();

            return KetQua<LichKhoangDto>.Ok(new LichKhoangDto(tu, den, cacCa, cacDong, ds));
        }
        catch (Exception)
        {
            return KetQua<LichKhoangDto>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<KetQuaKiemTraPhanCong>> KiemTraPhanCongAsync(
        int maNv, int maCa, string viTri, IReadOnlyCollection<DateOnly> cacNgay)
    {
        if (!_phien.LaQuanLy) return KetQua<KetQuaKiemTraPhanCong>.Loi(LoiQuyen);

        var loi = QuyTacLichLamViec.KiemTraViTri(viTri);
        if (loi is not null) return KetQua<KetQuaKiemTraPhanCong>.Loi(loi);

        var ngay = (cacNgay ?? Array.Empty<DateOnly>()).Distinct().OrderBy(x => x).ToList();
        if (ngay.Count == 0) return KetQua<KetQuaKiemTraPhanCong>.Loi("Chọn ít nhất một ô.");

        var daQua = ngay.Where(QuyTacLichLamViec.DaQua).ToList();
        if (daQua.Count > 0)
            return KetQua<KetQuaKiemTraPhanCong>.Loi($"Không xếp lịch cho ngày đã qua: {QuyTacLichLamViec.NoiNgay(daQua)}.");

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var nv = await db.NhanVien.AsNoTracking().FirstOrDefaultAsync(x => x.MaNv == maNv);
            if (nv is null) return KetQua<KetQuaKiemTraPhanCong>.Loi(LoiKhongTim);
            if (nv.TrangThaiLamViec != TrangThaiLamViec.DangLamViec)
                return KetQua<KetQuaKiemTraPhanCong>.Loi($"{nv.HoTen} đã nghỉ việc, không xếp lịch được.");

            var ca = await db.CaLamViec.AsNoTracking().FirstOrDefaultAsync(x => x.MaCa == maCa);
            if (ca is null) return KetQua<KetQuaKiemTraPhanCong>.Loi("Không tìm thấy ca làm việc, hãy tải lại.");
            if (!ca.DangSuDung)
                return KetQua<KetQuaKiemTraPhanCong>.Loi($"{ca.TenCa} đã ngừng sử dụng, không xếp lịch được.");

            var dinhMuc = QuyTacLichLamViec.DinhMuc(CaToDto(ca), viTri);

            var cuaNv = await db.LichLamViec.AsNoTracking()
                .Where(x => x.MaNv == maNv && ngay.Contains(x.Ngay))
                .ToDictionaryAsync(x => x.Ngay);

            // Người đang giữ ca ở vị trí này (không tính chính nhân viên đang xếp).
            var nguoiGiu = (await db.LichLamViec.AsNoTracking()
                    .Where(x => x.MaCa == maCa && x.ViTri == viTri && x.MaNv != maNv && ngay.Contains(x.Ngay))
                    .OrderBy(x => x.NhanVien!.HoTen)
                    .Select(x => new { x.Ngay, x.MaNv, x.NhanVien!.HoTen })
                    .ToListAsync())
                .ToLookup(x => x.Ngay);

            var hopLe = new List<ThayDoiLich>();
            var vuong = new List<OVuongDinhMuc>();
            var khongDoi = 0;

            foreach (var d in ngay)
            {
                ThayDoiLich thayDoi;
                if (cuaNv.TryGetValue(d, out var cu))
                {
                    if (cu.MaCa == maCa && cu.ViTri == viTri)
                    {
                        khongDoi++;
                        continue;
                    }

                    thayDoi = ThayDoiLich.Sua(maNv, d, cu.MaCa, cu.ViTri, maCa, viTri);
                }
                else
                {
                    thayDoi = ThayDoiLich.Them(maNv, d, maCa, viTri);
                }

                var giu = nguoiGiu[d].ToList();
                if (giu.Count < dinhMuc)
                    hopLe.Add(thayDoi);
                else
                    vuong.Add(new OVuongDinhMuc(d, maCa, ca.TenCa, viTri, dinhMuc,
                        giu.Select(x => new NguoiGiuCaDto(x.MaNv, x.HoTen)).ToList(), thayDoi));
            }

            return KetQua<KetQuaKiemTraPhanCong>.Ok(new KetQuaKiemTraPhanCong(nv.HoTen, ca.TenCa, hopLe, vuong, khongDoi));
        }
        catch (Exception)
        {
            return KetQua<KetQuaKiemTraPhanCong>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<KetQuaApDungLich>> ApDungThayDoiLichAsync(IReadOnlyList<ThayDoiLich> cacThayDoi)
    {
        if (!_phien.LaQuanLy) return KetQua<KetQuaApDungLich>.Loi(LoiQuyen);

        if (cacThayDoi is null || cacThayDoi.Count == 0)
            return KetQua<KetQuaApDungLich>.Loi("Không có thay đổi nào để lưu.");

        // Kiểm tra hình dạng dữ liệu trước khi mở giao dịch.
        foreach (var c in cacThayDoi)
        {
            var coCu = c.MaCaCu is not null && c.ViTriCu is not null;
            var coMoi = c.MaCaMoi is not null && c.ViTriMoi is not null;
            var hopLe = c.Loai switch
            {
                LoaiThayDoiLich.Them => coMoi,
                LoaiThayDoiLich.Sua => coCu && coMoi,
                _ => coCu,
            };
            if (!hopLe) return KetQua<KetQuaApDungLich>.Loi("Thay đổi lịch không hợp lệ.");
            if (c.ViTriMoi is not null)
            {
                var loiViTri = QuyTacLichLamViec.KiemTraViTri(c.ViTriMoi);
                if (loiViTri is not null) return KetQua<KetQuaApDungLich>.Loi(loiViTri);
            }
        }

        if (cacThayDoi.Select(x => (x.MaNv, x.Ngay)).Distinct().Count() != cacThayDoi.Count)
            return KetQua<KetQuaApDungLich>.Loi("Có hai thay đổi cho cùng một người trong cùng một ngày.");

        var daQua = cacThayDoi.Where(x => QuyTacLichLamViec.DaQua(x.Ngay)).Select(x => x.Ngay).Distinct().ToList();
        if (daQua.Count > 0)
            return KetQua<KetQuaApDungLich>.Loi($"Không thay đổi lịch của ngày đã qua: {QuyTacLichLamViec.NoiNgay(daQua)}.");

        const string LoiDoiLich = "Lịch vừa bị thay đổi ở nơi khác, hãy tải lại rồi làm lại.";

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var gd = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var cacMaNv = cacThayDoi.Select(x => x.MaNv).Distinct().ToList();
            var cacNgay = cacThayDoi.Select(x => x.Ngay).Distinct().ToList();

            var dongHienCo = (await db.LichLamViec
                    .Where(x => cacMaNv.Contains(x.MaNv) && cacNgay.Contains(x.Ngay))
                    .ToListAsync())
                .ToDictionary(x => (x.MaNv, x.Ngay));

            var nhanVien = await db.NhanVien.AsNoTracking()
                .Where(x => cacMaNv.Contains(x.MaNv))
                .ToDictionaryAsync(x => x.MaNv);

            var cacMaCaMoi = cacThayDoi.Where(x => x.MaCaMoi is not null).Select(x => x.MaCaMoi!.Value).Distinct().ToList();
            var cacCa = await db.CaLamViec.AsNoTracking()
                .Where(x => cacMaCaMoi.Contains(x.MaCa))
                .ToDictionaryAsync(x => x.MaCa);

            // Người và ca nhận lịch mới phải còn hợp lệ.
            foreach (var c in cacThayDoi.Where(x => x.Loai != LoaiThayDoiLich.Xoa))
            {
                if (!nhanVien.TryGetValue(c.MaNv, out var nv)) return KetQua<KetQuaApDungLich>.Loi(LoiKhongTim);
                if (nv.TrangThaiLamViec != TrangThaiLamViec.DangLamViec)
                    return KetQua<KetQuaApDungLich>.Loi($"{nv.HoTen} đã nghỉ việc, không xếp lịch được.");
                if (!cacCa.TryGetValue(c.MaCaMoi!.Value, out var ca))
                    return KetQua<KetQuaApDungLich>.Loi("Không tìm thấy ca làm việc, hãy tải lại.");
                if (!ca.DangSuDung)
                    return KetQua<KetQuaApDungLich>.Loi($"{ca.TenCa} đã ngừng sử dụng, không xếp lịch được.");
            }

            // Xoá trước, sửa và thêm sau.
            foreach (var c in cacThayDoi.Where(x => x.Loai == LoaiThayDoiLich.Xoa))
            {
                if (!dongHienCo.TryGetValue((c.MaNv, c.Ngay), out var dong)
                    || dong.MaCa != c.MaCaCu || dong.ViTri != c.ViTriCu)
                    return KetQua<KetQuaApDungLich>.Loi(LoiDoiLich);
                db.LichLamViec.Remove(dong);
                dongHienCo.Remove((c.MaNv, c.Ngay));
            }

            foreach (var c in cacThayDoi.Where(x => x.Loai == LoaiThayDoiLich.Sua))
            {
                if (!dongHienCo.TryGetValue((c.MaNv, c.Ngay), out var dong)
                    || dong.MaCa != c.MaCaCu || dong.ViTri != c.ViTriCu)
                    return KetQua<KetQuaApDungLich>.Loi(LoiDoiLich);
                dong.MaCa = c.MaCaMoi!.Value;
                dong.ViTri = c.ViTriMoi!;
            }

            foreach (var c in cacThayDoi.Where(x => x.Loai == LoaiThayDoiLich.Them))
            {
                // BR02: mỗi nhân viên tối đa một ca mỗi ngày.
                if (dongHienCo.ContainsKey((c.MaNv, c.Ngay)))
                    return KetQua<KetQuaApDungLich>.Loi(LoiDoiLich);
                db.LichLamViec.Add(new LichLamViec
                {
                    MaNv = c.MaNv,
                    Ngay = c.Ngay,
                    MaCa = c.MaCaMoi!.Value,
                    ViTri = c.ViTriMoi!,
                });
            }

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Ràng buộc UNIQUE (ma_nv, ngay) hoặc lỗi lưu: giao dịch tự huỷ khi thoát.
                return KetQua<KetQuaApDungLich>.Loi(LoiDoiLich);
            }

            // Định mức cứng: kiểm tra lại trên dữ liệu sau khi áp dụng, trong cùng giao dịch.
            var oCanKiemTra = cacThayDoi
                .Where(x => x.Loai != LoaiThayDoiLich.Xoa)
                .Select(x => (x.Ngay, MaCa: x.MaCaMoi!.Value, ViTri: x.ViTriMoi!))
                .Distinct()
                .ToList();
            if (oCanKiemTra.Count > 0)
            {
                var cacMaCa = oCanKiemTra.Select(x => x.MaCa).Distinct().ToList();
                var ngayKiemTra = oCanKiemTra.Select(x => x.Ngay).Distinct().ToList();
                var dem = (await db.LichLamViec.AsNoTracking()
                        .Where(x => cacMaCa.Contains(x.MaCa) && ngayKiemTra.Contains(x.Ngay))
                        .Select(x => new { x.Ngay, x.MaCa, x.ViTri })
                        .ToListAsync())
                    .GroupBy(x => (x.Ngay, x.MaCa, x.ViTri))
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (var o in oCanKiemTra.OrderBy(x => x.Ngay))
                {
                    var ca = cacCa[o.MaCa];
                    var dinhMuc = QuyTacLichLamViec.DinhMuc(CaToDto(ca), o.ViTri);
                    if (dem.GetValueOrDefault((o.Ngay, o.MaCa, o.ViTri)) > dinhMuc)
                        return KetQua<KetQuaApDungLich>.Loi(
                            $"{ca.TenCa} {QuyTacLichLamViec.ThuVaNgay(o.Ngay)} đã đủ {o.ViTri} ({dinhMuc}/{dinhMuc}), không nhận thêm. Hãy tải lại.");
                }
            }

            await gd.CommitAsync();

            return KetQua<KetQuaApDungLich>.Ok(new KetQuaApDungLich(
                cacThayDoi.Select(x => x.DaoNguoc()).ToList(),
                cacThayDoi.Count(x => x.Loai == LoaiThayDoiLich.Them),
                cacThayDoi.Count(x => x.Loai == LoaiThayDoiLich.Sua),
                cacThayDoi.Count(x => x.Loai == LoaiThayDoiLich.Xoa)));
        }
        catch (Exception)
        {
            return KetQua<KetQuaApDungLich>.Loi(LoiKetNoi);
        }
    }

    public async Task<KetQua<KetQuaXemTruocLap>> XemTruocLapLichTuanAsync(
        DateOnly ngayTrongTuanNguon, int soTuan, IReadOnlyCollection<int> cacMaNv)
    {
        if (!_phien.LaQuanLy) return KetQua<KetQuaXemTruocLap>.Loi(LoiQuyen);

        if (soTuan < 1 || soTuan > 8) return KetQua<KetQuaXemTruocLap>.Loi("Chỉ chép sang 1 đến 8 tuần tiếp theo.");

        var dauNguon = QuyTacLichLamViec.NgayDauTuan(ngayTrongTuanNguon);
        var cuoiNguon = dauNguon.AddDays(6);
        var denNgay = dauNguon.AddDays(7 * soTuan + 6);
        var maNv = (cacMaNv ?? Array.Empty<int>()).Distinct().ToList();

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var nguon = await db.LichLamViec.AsNoTracking()
                .Where(x => x.Ngay >= dauNguon && x.Ngay <= cuoiNguon
                            && maNv.Contains(x.MaNv)
                            && x.NhanVien!.TrangThaiLamViec == TrangThaiLamViec.DangLamViec)
                .OrderBy(x => x.NhanVien!.HoTen).ThenBy(x => x.MaNv).ThenBy(x => x.Ngay)
                .ToListAsync();

            var cacCa = await db.CaLamViec.AsNoTracking().ToDictionaryAsync(x => x.MaCa);

            // Hiện trạng các tuần đích: ai đã có ca, mỗi ca đã có bao nhiêu người.
            var dauDich = dauNguon.AddDays(7);
            var dich = await db.LichLamViec.AsNoTracking()
                .Where(x => x.Ngay >= dauDich && x.Ngay <= denNgay)
                .Select(x => new { x.MaNv, x.Ngay, x.MaCa, x.ViTri })
                .ToListAsync();
            var daCoCa = dich.Select(x => (x.MaNv, x.Ngay)).ToHashSet();
            var dem = dich
                .GroupBy(x => (x.Ngay, x.MaCa, x.ViTri))
                .ToDictionary(g => g.Key, g => g.Count());

            var cacThayDoi = new List<ThayDoiLich>();
            int daCo = 0, day = 0, daQua = 0, caNgung = 0;

            for (var k = 1; k <= soTuan; k++)
            {
                foreach (var dong in nguon)
                {
                    var ngayDich = dong.Ngay.AddDays(7 * k);

                    if (QuyTacLichLamViec.DaQua(ngayDich)) { daQua++; continue; }
                    if (!cacCa.TryGetValue(dong.MaCa, out var ca) || !ca.DangSuDung) { caNgung++; continue; }
                    if (daCoCa.Contains((dong.MaNv, ngayDich))) { daCo++; continue; }

                    var khoa = (ngayDich, dong.MaCa, dong.ViTri);
                    if (dem.GetValueOrDefault(khoa) >= QuyTacLichLamViec.DinhMuc(CaToDto(ca), dong.ViTri)) { day++; continue; }

                    dem[khoa] = dem.GetValueOrDefault(khoa) + 1;
                    daCoCa.Add((dong.MaNv, ngayDich));
                    cacThayDoi.Add(ThayDoiLich.Them(dong.MaNv, ngayDich, dong.MaCa, dong.ViTri));
                }
            }

            return KetQua<KetQuaXemTruocLap>.Ok(new KetQuaXemTruocLap(cacThayDoi, daCo, day, daQua, caNgung, denNgay));
        }
        catch (Exception)
        {
            return KetQua<KetQuaXemTruocLap>.Loi(LoiKetNoi);
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
