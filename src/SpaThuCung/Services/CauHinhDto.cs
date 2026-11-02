namespace SpaThuCung.Services;

/// <summary>
/// Các thông số ở thẻ Thông số. GiaTriSo theo mã trong <see cref="SpaThuCung.Models.MaCauHinh"/>
/// (đủ các mã trong <see cref="QuyTacCauHinh.ThongSoSo"/>).
/// </summary>
public sealed record ThongSoDto(IReadOnlyDictionary<string, int> GiaTriSo, string ThuMucAnh);

/// <summary>Một ca làm việc. MaCa = 0 khi thêm ca mới.</summary>
public sealed record CaLamViecDto(
    int MaCa,
    string TenCa,
    TimeSpan GioBatDau,
    TimeSpan GioKetThuc,
    int PhutVuotChoPhep,
    int SoLeTan,
    int SoGroomer,
    bool DangSuDung)
{
    /// <summary>Thời lượng ca = số phút tối đa của một Groomer trong ca (cảnh báo quá tải).</summary>
    public int ThoiLuongPhut => (int)(GioKetThuc - GioBatDau).TotalMinutes;

    /// <summary>Giờ kết thúc dự kiến muộn nhất của phiếu trong ca trước khi cảnh báo.</summary>
    public TimeSpan GioLoToiDa => GioKetThuc + TimeSpan.FromMinutes(PhutVuotChoPhep);
}
