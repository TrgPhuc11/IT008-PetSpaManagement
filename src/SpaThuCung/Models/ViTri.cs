namespace SpaThuCung.Models;

/// <summary>Các giá trị hợp lệ của cột vi_tri trong LichLamViec.</summary>
public static class ViTri
{
    public const string LeTan = "Lễ tân";
    public const string Groomer = "Groomer";

    public static readonly IReadOnlyList<string> TatCa = new[] { LeTan, Groomer };
}
