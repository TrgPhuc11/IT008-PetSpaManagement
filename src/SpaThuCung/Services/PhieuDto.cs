namespace SpaThuCung.Services;

/// <summary>Groomer có lịch đúng ngày, đúng ca (BR10), kèm tổng số phút các phiếu đã nhận trong ca (BR11).</summary>
public sealed record GroomerCaDto(int MaNv, string HoTen, int PhutDaNhan)
{
    public string HienThi => $"{HoTen} · đã nhận {PhutDaNhan} phút";
}

/// <summary>Ca chứa giờ hẹn và các Groomer của ca đó trong ngày hẹn.</summary>
public sealed record CaCuaGioHenDto(DateTime GioHen, CaLamViecDto Ca, List<GroomerCaDto> Groomer);

/// <summary>Một lồng đang trống.</summary>
public sealed record LongTrongDto(int MaLong, string TenLong, string LoaiLong);

/// <summary>
/// Một dịch vụ trong phiếu gửi lên để lưu. NhapTay = false thì Service tự tra giá, thời lượng theo loài và cân nặng
/// (DonGia, ThoiLuong bị bỏ qua); NhapTay = true thì dùng DonGia, ThoiLuong và bắt buộc có GhiChu (BR08).
/// </summary>
public sealed record DongDichVuNhap(int MaDv, bool NhapTay, decimal? DonGia, int? ThoiLuong, string? GhiChu);

/// <summary>
/// Phiếu mới (UC10). Đặt lịch: NgayGioHen là giờ hẹn, MaLong bỏ qua, MaGroomer tuỳ chọn.
/// Walk-in: giờ hẹn là lúc lưu (NgayGioHen bỏ qua), bắt buộc MaLong (lồng Walk-in trống) và MaGroomer.
/// </summary>
public sealed record PhieuMoi(
    int MaThuCung,
    string NguonTiepNhan,
    DateTime? NgayGioHen,
    decimal CanNang,
    int? MaGroomer,
    int? MaLong,
    IReadOnlyList<DongDichVuNhap> DichVu);

/// <summary>Kết quả lập phiếu.</summary>
public sealed record KetQuaLapPhieu(int MaPhieu, string TrangThai, DateTime NgayGioHen, decimal TongTien, int TongThoiGian)
{
    /// <summary>Mã dạng PDV0001.</summary>
    public string MaHienThi => QuyTacPhieu.MaHienThi(MaPhieu);
}
