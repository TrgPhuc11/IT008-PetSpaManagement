namespace SpaThuCung.Models;

/// <summary>Các giá trị hợp lệ của cột loai_long (Long) và nguon_tiep_nhan (PhieuDichVu): cùng hai giá trị.</summary>
public static class LoaiLong
{
    public const string DatLich = "Đặt lịch";
    public const string WalkIn = "Walk-in";

    public static readonly IReadOnlyList<string> TatCa = new[] { DatLich, WalkIn };
}

/// <summary>Nguồn tiếp nhận của phiếu (cột nguon_tiep_nhan).</summary>
public static class NguonTiepNhan
{
    public const string DatLich = LoaiLong.DatLich;
    public const string WalkIn = LoaiLong.WalkIn;

    public static readonly IReadOnlyList<string> TatCa = LoaiLong.TatCa;
}
