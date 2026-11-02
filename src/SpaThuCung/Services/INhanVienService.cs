namespace SpaThuCung.Services;

/// <summary>Quản lý nhân viên (chỉ vai trò Quản lý). Đặt lại mật khẩu nằm ở IAuthService.</summary>
public interface INhanVienService
{
    Task<KetQua<List<NhanVienDto>>> LayDanhSachAsync();

    Task<KetQua<NhanVienDto>> ThemAsync(NhanVienMoi du);

    Task<KetQua<NhanVienDto>> CapNhatAsync(int maNv, string hoTen, string? sdt, string vaiTro);

    /// <summary>Đảo Đang làm việc và Đã nghỉ việc. Không có chức năng xoá.</summary>
    Task<KetQua<NhanVienDto>> DoiTrangThaiAsync(int maNv);

    // ---------- Lịch làm việc (UC05, FR13, BR02) ----------

    /// <summary>Lịch của tuần chứa ngày đã cho, kèm danh sách ca và nhân viên đang làm việc để xếp. Chỉ Quản lý.</summary>
    Task<KetQua<LichTuanDto>> LayLichTuanAsync(DateOnly ngayTrongTuan);

    /// <summary>
    /// Xếp một nhân viên vào cùng một ca, một vị trí ở nhiều ngày. Ngày nhân viên đã có lịch thì bỏ qua (không lưu),
    /// các ngày còn lại vẫn lưu. Vượt số người cấu hình của ca chỉ cảnh báo. Không xếp ngày đã qua. Chỉ Quản lý.
    /// </summary>
    Task<KetQua<KetQuaXepLich>> XepLichAsync(int maNv, int maCa, string viTri, IReadOnlyCollection<DateOnly> cacNgay);

    /// <summary>Xoá một dòng lịch (không xoá được lịch của ngày đã qua). Chỉ Quản lý.</summary>
    Task<KetQua> XoaLichAsync(int maLich);

    /// <summary>Groomer có lịch đúng ngày, đúng ca, vị trí Groomer (BR10). Mọi nhân viên đã đăng nhập dùng được.</summary>
    Task<KetQua<List<NhanVienChonDto>>> LayGroomerTrongCaAsync(DateOnly ngay, int maCa);
}
