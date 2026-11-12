using Wpf.Ui.Controls;

namespace SpaThuCung.ViewModels;

public enum LoaiMuc
{
    TrangChu,
    KhachHang,
    ThuCung,
    DichVu,
    NhanVien,
    LichLamViec,
    CauHinh,
    ThongKe,
    SaoLuu,

    /// <summary>Nhãn nhóm (không chọn được).</summary>
    TieuDeNhom,
}

/// <summary>Một dòng trong menu trái.</summary>
public sealed record MucMenu(string TieuDe, LoaiMuc Loai, SymbolRegular Icon = SymbolRegular.Empty)
{
    public bool LaTieuDe => Loai == LoaiMuc.TieuDeNhom;
}
