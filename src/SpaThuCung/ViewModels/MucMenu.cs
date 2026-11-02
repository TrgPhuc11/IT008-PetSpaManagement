namespace SpaThuCung.ViewModels;

public enum LoaiMuc
{
    TrangChu,
    DichVu,
    NhanVien,
    CauHinh,

    /// <summary>Nhãn nhóm (không chọn được).</summary>
    TieuDeNhom,
}

/// <summary>Một dòng trong menu trái.</summary>
public sealed record MucMenu(string TieuDe, LoaiMuc Loai)
{
    public bool LaTieuDe => Loai == LoaiMuc.TieuDeNhom;
}
