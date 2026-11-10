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

    /// <summary>
    /// Lịch từ ngày tu đến ngày den (tối đa 62 ngày): ca, dòng lịch và nhân viên Đang làm việc
    /// kèm vị trí thường làm. Chỉ Quản lý.
    /// </summary>
    Task<KetQua<LichKhoangDto>> LayLichKhoangAsync(DateOnly tu, DateOnly den);

    /// <summary>
    /// Kiểm tra (chưa lưu) việc xếp một nhân viên vào một ca, một vị trí ở nhiều ngày. Định mức Lễ tân/Groomer của ca
    /// là cứng: ô đã đủ người được trả về trong VuongDinhMuc kèm người đang giữ ca. Không xếp ngày đã qua. Chỉ Quản lý.
    /// </summary>
    Task<KetQua<KetQuaKiemTraPhanCong>> KiemTraPhanCongAsync(int maNv, int maCa, string viTri, IReadOnlyCollection<DateOnly> cacNgay);

    /// <summary>
    /// Áp dụng một lô thay đổi lịch trong MỘT giao dịch (tất cả hoặc không gì cả). Kiểm tra lại ngày đã qua, nhân viên
    /// Đang làm việc, ca đang sử dụng, giá trị trước của từng dòng, UNIQUE và định mức của ca. Trả về lô thay đổi ngược
    /// để Hoàn tác. Chỉ Quản lý.
    /// </summary>
    Task<KetQua<KetQuaApDungLich>> ApDungThayDoiLichAsync(IReadOnlyList<ThayDoiLich> cacThayDoi);

    /// <summary>
    /// Lập kế hoạch (chưa lưu) chép lịch của tuần chứa ngayTrongTuanNguon sang soTuan tuần tiếp theo cho các nhân viên đã chọn.
    /// Chỉ điền ô trống, bỏ qua ô đủ người, ngày đã qua, ca đã ngừng. Chỉ Quản lý.
    /// </summary>
    Task<KetQua<KetQuaXemTruocLap>> XemTruocLapLichTuanAsync(DateOnly ngayTrongTuanNguon, int soTuan, IReadOnlyCollection<int> cacMaNv);

    /// <summary>Groomer có lịch đúng ngày, đúng ca, vị trí Groomer (BR10). Mọi nhân viên đã đăng nhập dùng được.</summary>
    Task<KetQua<List<NhanVienChonDto>>> LayGroomerTrongCaAsync(DateOnly ngay, int maCa);
}
