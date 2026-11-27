namespace SpaThuCung.Services;

/// <summary>
/// Vòng đời phiếu sau khi lập: theo dõi thú cưng (UC15), lịch hẹn theo ngày, nhận thú cưng, chuyển trạng thái, đổi lồng (UC14, FR09).
/// Mọi nhân viên đã đăng nhập dùng được. Thanh toán, trả thú cưng, huỷ, Không đến, sửa phiếu làm ở đợt sau.
/// </summary>
public interface ITheoDoiService
{
    /// <summary>Phiếu Đã nhận thú cưng, Đang thực hiện chăm sóc và Hoàn tất chăm sóc chờ khách đón (chưa trả thú cưng).</summary>
    Task<KetQua<List<PhieuTheoDoiDto>>> LayDangOTiemAsync();

    /// <summary>Phiếu có giờ hẹn trong một ngày, lọc theo trạng thái nếu có (null = tất cả), sắp theo giờ hẹn.</summary>
    Task<KetQua<List<PhieuTheoDoiDto>>> LayLichHenAsync(DateTime ngay, string? trangThai);

    /// <summary>Mọi lồng kèm tình trạng Trống / Đang dùng (BR13).</summary>
    Task<KetQua<List<LongTinhTrangDto>>> LayTinhTrangLongAsync();

    /// <summary>Dữ liệu cho form nhận thú cưng của một phiếu Đã đặt lịch đến đúng ngày hẹn.</summary>
    Task<KetQua<NhanThongTinDto>> LayThongTinNhanAsync(int maPhieu);

    /// <summary>Tra lại giá, thời lượng các dịch vụ của phiếu theo cân nặng thực tế (BR14).</summary>
    Task<KetQua<List<DongGiaMoiDto>>> TraGiaLaiAsync(int maPhieu, decimal canNang);

    /// <summary>
    /// Nhận thú cưng (BR10, BR13, BR14, BR15): Đã đặt lịch → Đã nhận thú cưng. Ghi giờ nhận thực tế (giờ hẹn gốc giữ nguyên),
    /// cân nặng, giá, Groomer, lồng và nhật ký. Cân nặng mới cũng được cập nhật vào hồ sơ thú cưng.
    /// </summary>
    Task<KetQua> NhanThuCungAsync(NhanThuCungMoi yeuCau);

    /// <summary>
    /// Đã nhận thú cưng → Đang thực hiện chăm sóc (BR16): phiếu phải có Groomer, Groomer không có phiếu khác đang thực hiện,
    /// đã vào ca, và nếu khách đến sớm thì không còn phiếu hẹn sớm hơn của Groomer đang chờ (BR14). Ghi giờ bắt đầu.
    /// </summary>
    Task<KetQua> BatDauChamSocAsync(int maPhieu);

    /// <summary>Đang thực hiện chăm sóc → Hoàn tất chăm sóc. Thú cưng ở lại lồng chờ khách đón.</summary>
    Task<KetQua> HoanTatChamSocAsync(int maPhieu);

    /// <summary>Lồng trống có thể đổi sang cho phiếu: phiếu Walk-in chỉ dùng lồng Walk-in, phiếu đặt lịch dùng cả hai loại (BR13).</summary>
    Task<KetQua<List<LongTrongDto>>> LayLongDeDoiAsync(int maPhieu);

    /// <summary>Đổi lồng cho phiếu đang giữ thú cưng, ghi nhật ký (BR13).</summary>
    Task<KetQua> DoiLongAsync(int maPhieu, int maLongMoi);
}
