using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Một lồng trống trong ComboBox chọn lồng.</summary>
public sealed record LuaChonLong(int MaLong, string HienThi);

/// <summary>Hộp thoại Đổi lồng (BR13): chọn một lồng trống khác cho thú cưng đang ở tiệm.</summary>
public partial class DoiLongViewModel : HopThoaiViewModel
{
    private readonly ITheoDoiService _service;
    private readonly PhieuTheoDoiDto _phieu;

    public DoiLongViewModel(ITheoDoiService service, PhieuTheoDoiDto phieu, IReadOnlyList<LongTrongDto> longTrong)
    {
        _service = service;
        _phieu = phieu;
        CacLong = longTrong.Select(l => new LuaChonLong(l.MaLong, $"{l.TenLong} ({l.LoaiLong})")).ToList();
        _longChon = CacLong[0];
    }

    public string TieuDe => $"Đổi lồng cho {_phieu.TenThuCung}";

    public string MoTa => $"{_phieu.MaHienThi} · đang ở lồng {_phieu.LongHienThi}. Chọn lồng trống để chuyển sang.";

    public IReadOnlyList<LuaChonLong> CacLong { get; }

    [ObservableProperty]
    private LuaChonLong _longChon;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoLoi))]
    private string _loi = string.Empty;

    public bool CoLoi => !string.IsNullOrEmpty(Loi);

    /// <summary>Thông báo kết quả để màn Theo dõi hiển thị sau khi đóng.</summary>
    public string KetQuaThongBao { get; private set; } = string.Empty;

    [RelayCommand]
    private async Task XacNhanAsync()
    {
        var kq = await _service.DoiLongAsync(_phieu.MaPhieu, LongChon.MaLong);
        if (!kq.ThanhCong)
        {
            Loi = kq.ThongBao;
            return;
        }

        KetQuaThongBao = kq.ThongBao;
        Dong(true);
    }

    [RelayCommand]
    private void Huy() => Dong(false);
}
