namespace SpaThuCung.Services;

/// <summary>Xem doanh thu gom theo từng ngày hay từng tháng (UC18).</summary>
public enum CachXemThongKe
{
    TheoNgay,
    TheoThang,
}

/// <summary>Một kỳ (ngày hoặc tháng) trên biểu đồ và bảng chi tiết.</summary>
/// <param name="Nhan">Ví dụ "05/11" (theo ngày) hoặc "11/2026" (theo tháng).</param>
/// <param name="SoPhieu">Mọi phiếu có ngày hẹn trong kỳ (mọi trạng thái).</param>
/// <param name="SoPhieuDaThanhToan">Phiếu tính vào doanh thu.</param>
public sealed record KyDoanhThuDto(DateOnly TuNgay, string Nhan, decimal DoanhThu, int SoPhieu, int SoPhieuDaThanhToan);

/// <summary>Doanh thu của một dịch vụ hoặc một Groomer trong khoảng thời gian.</summary>
/// <param name="SoPhieu">Số phiếu đã thanh toán có dịch vụ này / do Groomer này phụ trách.</param>
public sealed record DoanhThuNhomDto(string Ten, decimal DoanhThu, int SoPhieu);

public sealed record SoPhieuTrangThaiDto(string TrangThai, int SoPhieu);

/// <summary>Kết quả thống kê doanh thu (UC18, FR12). Doanh thu chỉ tính phiếu "Đã thanh toán" (BR17).</summary>
public sealed record ThongKeDoanhThuDto(
    decimal TongDoanhThu,
    int SoPhieuDaThanhToan,
    int TongSoPhieu,
    List<KyDoanhThuDto> TheoKy,
    List<DoanhThuNhomDto> TheoDichVu,
    List<DoanhThuNhomDto> TheoGroomer,
    List<SoPhieuTrangThaiDto> TheoTrangThai);
