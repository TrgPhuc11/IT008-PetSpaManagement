namespace SpaThuCung.Models;

/// <summary>Một dòng lịch làm việc: nhân viên làm ca nào, vị trí gì trong một ngày (FR13, BR02).</summary>
public class LichLamViec
{
    public int MaLich { get; set; }
    public int MaNv { get; set; }
    public DateOnly Ngay { get; set; }
    public int MaCa { get; set; }
    public string ViTri { get; set; } = Models.ViTri.Groomer;

    public NhanVien? NhanVien { get; set; }
    public CaLamViec? CaLamViec { get; set; }
}
