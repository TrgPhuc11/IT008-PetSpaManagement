namespace SpaThuCung.Models;

/// <summary>Một dịch vụ chăm sóc (tắm, cắt tỉa...). Giá và thời lượng nằm ở BangGia.</summary>
public class DichVu
{
    public int MaDv { get; set; }
    public string TenDv { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string TrangThai { get; set; } = TrangThaiDichVu.DangCungCap;

    public List<BangGia> BangGia { get; set; } = new();
}
