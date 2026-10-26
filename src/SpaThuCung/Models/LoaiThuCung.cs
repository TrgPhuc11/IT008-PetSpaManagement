namespace SpaThuCung.Models;

/// <summary>Các giá trị hợp lệ của cột loai (ThuCung, BangGia).</summary>
public static class LoaiThuCung
{
    public const string Cho = "Chó";
    public const string Meo = "Mèo";

    public static readonly IReadOnlyList<string> TatCa = new[] { Cho, Meo };
}
