namespace SpaThuCung.Services;

/// <summary>Một dòng lịch làm việc để hiển thị trên lưới.</summary>
public sealed record DongLichDto(int MaLich, int MaNv, string HoTen, bool DaNghiViec, DateOnly Ngay, int MaCa, string ViTri);

/// <summary>Nhân viên để chọn khi xếp lịch hoặc phân công (chỉ người Đang làm việc).</summary>
public sealed record NhanVienChonDto(int MaNv, string HoTen, string VaiTro)
{
    public string MaHienThi => $"NV{MaNv:000}";
}

/// <summary>Nhân viên Đang làm việc trên lưới lịch, kèm vị trí thường làm (để chia nhóm và chọn vị trí tự động).</summary>
public sealed record NhanVienLichDto(int MaNv, string HoTen, string VaiTro, string ViTriThuong);

/// <summary>Lịch của một khoảng ngày (tối đa 62 ngày). CacCa gồm ca đang sử dụng và ca đã ngừng nhưng còn lịch trong khoảng.</summary>
public sealed record LichKhoangDto(
    DateOnly Tu,
    DateOnly Den,
    List<CaLamViecDto> CacCa,
    List<DongLichDto> CacDong,
    List<NhanVienLichDto> NhanVienDangLam);

public enum LoaiThayDoiLich
{
    Them,
    Sua,
    Xoa,
}

/// <summary>
/// Một thay đổi lịch của một nhân viên trong một ngày (khoá là MaNv + Ngay, vì UNIQUE(ma_nv, ngay)).
/// Giữ đủ giá trị trước và sau để đảo ngược được (Hoàn tác).
/// </summary>
public sealed record ThayDoiLich(
    LoaiThayDoiLich Loai,
    int MaNv,
    DateOnly Ngay,
    int? MaCaCu,
    string? ViTriCu,
    int? MaCaMoi,
    string? ViTriMoi)
{
    public static ThayDoiLich Them(int maNv, DateOnly ngay, int maCa, string viTri) =>
        new(LoaiThayDoiLich.Them, maNv, ngay, null, null, maCa, viTri);

    public static ThayDoiLich Sua(int maNv, DateOnly ngay, int maCaCu, string viTriCu, int maCaMoi, string viTriMoi) =>
        new(LoaiThayDoiLich.Sua, maNv, ngay, maCaCu, viTriCu, maCaMoi, viTriMoi);

    public static ThayDoiLich Xoa(int maNv, DateOnly ngay, int maCaCu, string viTriCu) =>
        new(LoaiThayDoiLich.Xoa, maNv, ngay, maCaCu, viTriCu, null, null);

    /// <summary>Thay đổi làm ngược lại: thêm thì xoá, xoá thì thêm lại, sửa thì đặt lại giá trị cũ.</summary>
    public ThayDoiLich DaoNguoc() => Loai switch
    {
        LoaiThayDoiLich.Them => new(LoaiThayDoiLich.Xoa, MaNv, Ngay, MaCaMoi, ViTriMoi, null, null),
        LoaiThayDoiLich.Xoa => new(LoaiThayDoiLich.Them, MaNv, Ngay, null, null, MaCaCu, ViTriCu),
        _ => new(LoaiThayDoiLich.Sua, MaNv, Ngay, MaCaMoi, ViTriMoi, MaCaCu, ViTriCu),
    };
}

/// <summary>Kết quả áp dụng một lô thay đổi. NghichDao là lô thay đổi để hoàn tác.</summary>
public sealed record KetQuaApDungLich(List<ThayDoiLich> NghichDao, int SoThem, int SoSua, int SoXoa);

public sealed record NguoiGiuCaDto(int MaNv, string HoTen);

/// <summary>Một ô đã đủ người cho vị trí trong ca; ThayDoiChoNguoiMoi chỉ lưu khi Quản lý chọn đổi người.</summary>
public sealed record OVuongDinhMuc(
    DateOnly Ngay,
    int MaCa,
    string TenCa,
    string ViTri,
    int DinhMuc,
    List<NguoiGiuCaDto> NguoiGiu,
    ThayDoiLich ThayDoiChoNguoiMoi);

/// <summary>Kết quả kiểm tra phân công một nhân viên vào nhiều ngày (chưa lưu gì).</summary>
public sealed record KetQuaKiemTraPhanCong(
    string HoTenNhanVien,
    string TenCa,
    List<ThayDoiLich> HopLe,
    List<OVuongDinhMuc> VuongDinhMuc,
    int SoKhongDoi);

/// <summary>Kế hoạch lặp lịch tuần (chưa lưu). Các số BoQua chia theo lý do.</summary>
public sealed record KetQuaXemTruocLap(
    List<ThayDoiLich> CacThayDoi,
    int BoQuaDaCoCa,
    int BoQuaDayNguoi,
    int BoQuaNgayDaQua,
    int BoQuaCaNgung,
    DateOnly DenNgay);
