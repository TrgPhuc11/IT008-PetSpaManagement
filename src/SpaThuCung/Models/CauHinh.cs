namespace SpaThuCung.Models;

/// <summary>Một thông số vận hành do Quản lý cấu hình (FR14). Mã lấy từ <see cref="MaCauHinh"/>.</summary>
public class CauHinh
{
    public string MaCauHinh { get; set; } = string.Empty;
    public string GiaTri { get; set; } = string.Empty;
    public string? MoTa { get; set; }
}
