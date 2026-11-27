using SpaThuCung.Models;

namespace SpaThuCung.Services;

/// <summary>
/// Một phiếu trên màn Theo dõi / Lịch hẹn (UC14, UC15): thú cưng, khách, lồng, Groomer, giờ và giờ kết thúc dự kiến (BR14).
/// </summary>
public sealed record PhieuTheoDoiDto(
    int MaPhieu,
    int MaThuCung,
    string TenThuCung,
    string LoaiThuCung,
    string TenKhach,
    string Sdt,
    string TrangThai,
    string NguonTiepNhan,
    DateTime NgayGioHen,
    DateTime? GioNhan,
    DateTime? GioBatDau,
    DateTime? GioTra,
    int? MaLong,
    string? TenLong,
    int? MaGroomer,
    string? TenGroomer,
    decimal TongTien,
    int TongThoiGian,
    string TtThanhToan,
    DateTime? GioXongDuKien,
    string? LyDoHuy)
{
    public string MaHienThi => QuyTacPhieu.MaHienThi(MaPhieu);

    public string ThuCungHienThi => $"{TenThuCung} ({LoaiThuCung})";

    public string KhachHienThi => $"{TenKhach} · {Sdt}";

    public string GioHenHienThi => QuyTacPhieu.NgayGio(NgayGioHen);

    public string GioXongHienThi => GioXongDuKien is null ? "—" : QuyTacPhieu.Gio(GioXongDuKien.Value);

    public string LongHienThi => TenLong ?? "—";

    public string GroomerHienThi => TenGroomer ?? "Chưa phân công";

    public string TongTienHienThi => $"{QuyTacDichVu.TienHienThi(TongTien)} đ";

    /// <summary>Hoàn tất mà chưa trả thú cưng thì ghi "chờ khách đón".</summary>
    public string TrangThaiHienThi =>
        TrangThai == TrangThaiPhieu.HoanTat && GioTra is null ? "Hoàn tất, chờ khách đón" : TrangThai;

    public bool ChoKhachDon => TrangThai == TrangThaiPhieu.HoanTat && GioTra is null;

    /// <summary>Chỉ nhận thú cưng đúng ngày hẹn.</summary>
    public bool CoTheNhan => TrangThai == TrangThaiPhieu.DaDatLich && NgayGioHen.Date == DateTime.Today;

    public bool CoTheBatDau => TrangThai == TrangThaiPhieu.DaNhanThuCung;

    public bool CoTheHoanTat => TrangThai == TrangThaiPhieu.DangThucHien;

    public bool CoTheDoiLong =>
        TrangThai is TrangThaiPhieu.DaNhanThuCung or TrangThaiPhieu.DangThucHien || ChoKhachDon;
}

/// <summary>Tình trạng một lồng: Trống hoặc đang giữ thú cưng nào (UC15, BR13).</summary>
public sealed record LongTinhTrangDto(int MaLong, string TenLong, string LoaiLong, bool Trong, string? ThuCung)
{
    public string TinhTrang => Trong ? "Trống" : $"Đang dùng · {ThuCung}";
}

/// <summary>Một dịch vụ của phiếu lúc nhận. GhiChu khác rỗng nghĩa là dòng đã nhập tay (BR08).</summary>
public sealed record DongNhanDto(int MaDv, string TenDv, decimal DonGia, int ThoiLuong, string? GhiChu);

/// <summary>Giá tra lại theo cân nặng mới cho một dịch vụ; CoGia = false nghĩa là không có khoảng phù hợp, phải nhập tay (BR14).</summary>
public sealed record DongGiaMoiDto(int MaDv, bool CoGia, decimal DonGia, int ThoiLuong);

/// <summary>Mọi thứ form Nhận thú cưng cần: phiếu, dòng dịch vụ, Groomer của ca hiện tại, lồng trống (BR10, BR13, BR14).</summary>
public sealed record NhanThongTinDto(
    PhieuTheoDoiDto Phieu,
    decimal CanNangLucLap,
    List<DongNhanDto> Dong,
    CaCuaGioHenDto Ca,
    List<LongTrongDto> LongDatLich,
    List<LongTrongDto> LongWalkIn);

/// <summary>
/// Yêu cầu nhận thú cưng. Dong: mỗi dịch vụ của phiếu đúng một dòng; NhapTay = false thì Service tra lại giá theo cân nặng mới,
/// NhapTay = true thì dùng DonGia, ThoiLuong, GhiChu nhập tay (BR08). KhachDongY bắt buộc khi tổng tiền đổi (BR14).
/// </summary>
public sealed record NhanThuCungMoi(
    int MaPhieu,
    decimal CanNang,
    int MaGroomer,
    int MaLong,
    bool KhachDongY,
    IReadOnlyList<DongDichVuNhap> Dong);
