using CommunityToolkit.Mvvm.ComponentModel;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Một ô số ở thẻ Thông số: kiểm tra ngay khi gõ, lỗi hiện dưới ô.</summary>
public partial class ThongSoMucViewModel : ObservableObject
{
    public ThongSoMucViewModel(DinhNghiaThongSo dinhNghia, int giaTri)
    {
        DinhNghia = dinhNghia;
        _giaTri = giaTri.ToString();
    }

    public DinhNghiaThongSo DinhNghia { get; }

    public string Ten => DinhNghia.Ten;
    public string DonVi => DinhNghia.DonVi;
    public string MoTa => $"{DinhNghia.MoTa} Mặc định {DinhNghia.MacDinh} {DinhNghia.DonVi}.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Loi))]
    [NotifyPropertyChangedFor(nameof(CoLoi))]
    private string _giaTri;

    /// <summary>Phát ra khi người dùng sửa ô, để màn hình bật nút Lưu.</summary>
    public event Action? DaSua;

    partial void OnGiaTriChanged(string value) => DaSua?.Invoke();

    public string? Loi => QuyTacCauHinh.KiemTraSo(DinhNghia, GiaTri, out _);

    public bool CoLoi => Loi is not null;

    public bool DocGiaTri(out int giaTri) => QuyTacCauHinh.KiemTraSo(DinhNghia, GiaTri, out giaTri) is null;
}
