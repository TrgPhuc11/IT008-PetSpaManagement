namespace SpaThuCung.Models;

/// <summary>
/// Một dòng bảng giá: giá và thời lượng của một dịch vụ cho một loài trong một khoảng cân nặng.
/// Khoảng tính từ trên CanNangTu đến hết CanNangDen (ví dụ 0–5 và 5–15: 5 kg thuộc dòng 0–5).
/// </summary>
public class BangGia
{
    public int MaGia { get; set; }
    public int MaDv { get; set; }
    public string Loai { get; set; } = LoaiThuCung.Cho;
    public decimal CanNangTu { get; set; }
    public decimal CanNangDen { get; set; }
    public decimal Gia { get; set; }

    /// <summary>Thời lượng thực hiện (phút), do Quản lý nhập.</summary>
    public int ThoiLuong { get; set; }

    public DichVu? DichVu { get; set; }
}
