using Microsoft.EntityFrameworkCore;
using SpaThuCung.Data;
using SpaThuCung.Models;

namespace SpaThuCung.Services;

public class ThongKeService : IThongKeService
{
    private const string LoiKetNoi = "Không kết nối được cơ sở dữ liệu.";
    private const string LoiQuyen = "Chỉ Quản lý được xem thống kê doanh thu.";

    private readonly IDbContextFactory<SpaDbContext> _dbFactory;
    private readonly IPhienLamViec _phien;

    public ThongKeService(IDbContextFactory<SpaDbContext> dbFactory, IPhienLamViec phien)
    {
        _dbFactory = dbFactory;
        _phien = phien;
    }

    public async Task<KetQua<ThongKeDoanhThuDto>> DoanhThuAsync(DateOnly tuNgay, DateOnly denNgay, CachXemThongKe cachXem)
    {
        if (!_phien.LaQuanLy) return KetQua<ThongKeDoanhThuDto>.Loi(LoiQuyen);

        var loi = QuyTacThongKe.KiemTraKhoang(tuNgay, denNgay, cachXem);
        if (loi is not null) return KetQua<ThongKeDoanhThuDto>.Loi(loi);

        // Lọc theo ngày hẹn: từ 0 giờ ngày đầu đến trước 0 giờ ngày sau ngày cuối.
        var tu = tuNgay.ToDateTime(TimeOnly.MinValue);
        var den = denNgay.AddDays(1).ToDateTime(TimeOnly.MinValue);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // Mỗi phiếu chỉ lấy vài cột; số phiếu trong tối đa 93 ngày / 24 tháng của một tiệm là nhỏ, gom nhóm trong bộ nhớ.
            var cacPhieu = await db.PhieuDichVu.AsNoTracking()
                .Where(p => p.NgayGioHen >= tu && p.NgayGioHen < den)
                .Select(p => new { p.NgayGioHen, p.TrangThai, p.TtThanhToan, p.TongTien, p.MaGroomer })
                .ToListAsync();

            var daThanhToan = cacPhieu.Where(p => QuyTacThongKe.TinhDoanhThu(p.TrangThai, p.TtThanhToan)).ToList();

            // Theo kỳ: đủ mọi kỳ trong khoảng, kỳ không có phiếu thì bằng 0.
            var theoKy = QuyTacThongKe.CacKy(tuNgay, denNgay, cachXem).Select(ky =>
            {
                var trongKy = cacPhieu.Where(p => QuyTacThongKe.DauKy(DateOnly.FromDateTime(p.NgayGioHen), cachXem) == ky).ToList();
                var traTien = trongKy.Where(p => QuyTacThongKe.TinhDoanhThu(p.TrangThai, p.TtThanhToan)).ToList();
                return new KyDoanhThuDto(ky, QuyTacThongKe.NhanKy(ky, cachXem),
                    traTien.Sum(p => p.TongTien), trongKy.Count, traTien.Count);
            }).ToList();

            // Theo dịch vụ: cộng đơn giá từng dòng chi tiết của phiếu đã thanh toán.
            var dongDichVu = await db.ChiTietPhieu.AsNoTracking()
                .Where(c => c.PhieuDichVu!.NgayGioHen >= tu && c.PhieuDichVu.NgayGioHen < den
                            && c.PhieuDichVu.TtThanhToan == TrangThaiThanhToan.DaThanhToan
                            && c.PhieuDichVu.TrangThai != TrangThaiPhieu.DaHuy
                            && c.PhieuDichVu.TrangThai != TrangThaiPhieu.KhongDen)
                .GroupBy(c => new { c.MaDv, c.DichVu!.TenDv })
                .Select(g => new { g.Key.TenDv, DoanhThu = g.Sum(c => c.DonGia), SoPhieu = g.Count() })
                .ToListAsync();

            var theoDichVu = dongDichVu
                .Select(x => new DoanhThuNhomDto(x.TenDv, x.DoanhThu, x.SoPhieu))
                .OrderByDescending(x => x.DoanhThu).ThenBy(x => x.Ten)
                .ToList();

            // Theo Groomer: tổng tiền của phiếu do Groomer đó phụ trách.
            var maGroomer = daThanhToan.Where(p => p.MaGroomer.HasValue).Select(p => p.MaGroomer!.Value).Distinct().ToList();
            var tenGroomer = await db.NhanVien.AsNoTracking()
                .Where(nv => maGroomer.Contains(nv.MaNv))
                .ToDictionaryAsync(nv => nv.MaNv, nv => nv.HoTen);

            var theoGroomer = daThanhToan
                .GroupBy(p => p.MaGroomer)
                .Select(g => new DoanhThuNhomDto(
                    g.Key is int ma ? tenGroomer.GetValueOrDefault(ma, $"Nhân viên #{ma}") : QuyTacThongKe.ChuaPhanCong,
                    g.Sum(p => p.TongTien), g.Count()))
                .OrderByDescending(x => x.DoanhThu).ThenBy(x => x.Ten)
                .ToList();

            // Số phiếu theo trạng thái: đủ 6 trạng thái theo thứ tự quy trình.
            var demTrangThai = cacPhieu.GroupBy(p => p.TrangThai).ToDictionary(g => g.Key, g => g.Count());
            var theoTrangThai = TrangThaiPhieu.TatCa
                .Select(tt => new SoPhieuTrangThaiDto(tt, demTrangThai.GetValueOrDefault(tt)))
                .ToList();

            return KetQua<ThongKeDoanhThuDto>.Ok(new ThongKeDoanhThuDto(
                daThanhToan.Sum(p => p.TongTien), daThanhToan.Count, cacPhieu.Count,
                theoKy, theoDichVu, theoGroomer, theoTrangThai));
        }
        catch (Exception)
        {
            return KetQua<ThongKeDoanhThuDto>.Loi(LoiKetNoi);
        }
    }
}
