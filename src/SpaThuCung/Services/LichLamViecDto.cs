namespace SpaThuCung.Services;

/// <summary>Một dòng lịch làm việc để hiển thị trên lưới tuần.</summary>
public sealed record DongLichDto(int MaLich, int MaNv, string HoTen, bool DaNghiViec, DateOnly Ngay, int MaCa, string ViTri);

/// <summary>Nhân viên để chọn khi xếp lịch hoặc phân công (chỉ người Đang làm việc).</summary>
public sealed record NhanVienChonDto(int MaNv, string HoTen, string VaiTro)
{
    public string MaHienThi => $"NV{MaNv:000}";
}

/// <summary>
/// Lịch của một tuần (Thứ 2 đến Chủ nhật). CacCa gồm các ca đang sử dụng và các ca đã ngừng nhưng còn lịch trong tuần.
/// </summary>
public sealed record LichTuanDto(
    DateOnly NgayDauTuan,
    List<CaLamViecDto> CacCa,
    List<DongLichDto> CacDong,
    List<NhanVienChonDto> NhanVienDangLam);

/// <summary>Kết quả xếp một nhân viên vào nhiều ngày.</summary>
public sealed record KetQuaXepLich(
    List<DateOnly> DaLuu,
    List<DateOnly> TrungNgay,
    List<DateOnly> VuotDinhMuc);
