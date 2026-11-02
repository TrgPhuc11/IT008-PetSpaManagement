namespace SpaThuCung.Models;

/// <summary>
/// Một ca làm việc do Quản lý cấu hình (FR14, BR09). Thời lượng ca (giờ kết thúc − giờ bắt đầu)
/// là số phút tối đa của một Groomer trong ca, dùng cho cảnh báo quá tải.
/// </summary>
public class CaLamViec
{
    public int MaCa { get; set; }
    public string TenCa { get; set; } = string.Empty;
    public TimeSpan GioBatDau { get; set; }
    public TimeSpan GioKetThuc { get; set; }

    /// <summary>Số phút giờ kết thúc dự kiến của phiếu được vượt giờ kết thúc ca trước khi cảnh báo.</summary>
    public int PhutVuotChoPhep { get; set; } = 30;

    public int SoLeTan { get; set; } = 1;
    public int SoGroomer { get; set; } = 2;
    public bool DangSuDung { get; set; } = true;
}
