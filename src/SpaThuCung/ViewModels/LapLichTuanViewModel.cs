using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Một nhân viên trong hộp thoại Lặp lịch tuần. Không có ca trong tuần nguồn thì tắt và khoá.</summary>
public sealed partial class NhanVienLapViewModel : ObservableObject
{
    private readonly Action _khiDoi;

    public NhanVienLapViewModel(int maNv, string hoTen, int soCa, Action khiDoi)
    {
        MaNv = maNv;
        HoTen = hoTen;
        SoCa = soCa;
        _khiDoi = khiDoi;
        _bat = soCa > 0;
    }

    public int MaNv { get; }
    public string HoTen { get; }
    public int SoCa { get; }
    public bool CoTheChon => SoCa > 0;
    public string GhiChu => SoCa > 0 ? $"{SoCa} ca trong tuần nguồn" : "Tuần nguồn chưa có ca";

    [ObservableProperty]
    private bool _bat;

    partial void OnBatChanged(bool value) => _khiDoi();
}

/// <summary>
/// Hộp thoại Lặp lịch tuần (chỉ ở chế độ Tuần): chép lịch tuần đang xem sang 1 đến 8 tuần tiếp theo.
/// Kế hoạch do Service lập (chưa lưu); ViewModel màn lịch lưu CacThayDoi khi người dùng bấm Lặp lịch.
/// </summary>
public sealed partial class LapLichTuanViewModel : HopThoaiViewModel
{
    private readonly INhanVienService _nhanVien;
    private readonly DateOnly _ngayDauTuan;
    private int _lanTinh;

    public LapLichTuanViewModel(INhanVienService nhanVien, DateOnly ngayDauTuan,
        IEnumerable<(NhanVienLichDto NhanVien, int SoCa)> danhSach)
    {
        _nhanVien = nhanVien;
        _ngayDauTuan = ngayDauTuan;
        NhanVien = danhSach
            .Select(x => new NhanVienLapViewModel(x.NhanVien.MaNv, x.NhanVien.HoTen, x.SoCa, KhiDoiLuaChon))
            .ToList();
        _ = CapNhatXemTruocAsync();
    }

    public IReadOnlyList<int> CacSoTuan { get; } = new[] { 1, 2, 3, 4, 8 };

    public IReadOnlyList<NhanVienLapViewModel> NhanVien { get; }

    public string Nguon =>
        $"Nguồn: tuần {QuyTacLichLamViec.NgayNgan(_ngayDauTuan)} – {_ngayDauTuan.AddDays(6).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} (tuần đang xem)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DenNgayText))]
    private int _soTuan = 4;

    public string DenNgayText =>
        $"đến hết {_ngayDauTuan.AddDays(7 * SoTuan + 6).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}";

    /// <summary>Các thay đổi sẽ lưu khi bấm Lặp lịch (kế hoạch mới nhất).</summary>
    public IReadOnlyList<ThayDoiLich> CacThayDoi { get; private set; } = Array.Empty<ThayDoiLich>();

    /// <summary>Số ô bỏ qua vì ô đã có ca hoặc ca đã đủ người (để báo cảnh báo sau khi lưu).</summary>
    public int SoOBoQuaDoDay { get; private set; }

    [ObservableProperty]
    private string _tomTatChinh = "Đang tính...";

    [ObservableProperty]
    private string _tomTatBoQua = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoLoi))]
    private string _thongBaoLoi = string.Empty;

    public bool CoLoi => !string.IsNullOrEmpty(ThongBaoLoi);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChuNutLap))]
    [NotifyCanExecuteChangedFor(nameof(LapCommand))]
    private int _soCaSeThem;

    public string ChuNutLap => SoCaSeThem > 0 ? $"Lặp lịch ({SoCaSeThem} ca)" : "Lặp lịch";

    partial void OnSoTuanChanged(int value) => KhiDoiLuaChon();

    private void KhiDoiLuaChon() => _ = CapNhatXemTruocAsync();

    /// <summary>Đang tính lại kế hoạch: khoá nút Lặp lịch để không lưu kế hoạch cũ (trước khi đổi lựa chọn).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LapCommand))]
    private bool _dangTinh;

    private async Task CapNhatXemTruocAsync()
    {
        var lan = ++_lanTinh;
        DangTinh = true;
        var maNv = NhanVien.Where(x => x.Bat && x.CoTheChon).Select(x => x.MaNv).ToList();
        var soTuan = SoTuan;

        var kq = await _nhanVien.XemTruocLapLichTuanAsync(_ngayDauTuan, soTuan, maNv);
        if (lan != _lanTinh) return; // đã có lần tính mới hơn
        DangTinh = false;

        if (!kq.ThanhCong || kq.DuLieu is null)
        {
            ThongBaoLoi = kq.ThongBao;
            CacThayDoi = Array.Empty<ThayDoiLich>();
            SoOBoQuaDoDay = 0;
            SoCaSeThem = 0;
            TomTatChinh = "Không tính được kế hoạch.";
            TomTatBoQua = string.Empty;
            return;
        }

        ThongBaoLoi = string.Empty;
        var d = kq.DuLieu;
        CacThayDoi = d.CacThayDoi;
        SoOBoQuaDoDay = d.BoQuaDaCoCa + d.BoQuaDayNguoi;
        SoCaSeThem = d.CacThayDoi.Count;
        TomTatChinh = $"Sẽ thêm {d.CacThayDoi.Count} ca";

        var bo = new List<string>();
        if (d.BoQuaDaCoCa > 0) bo.Add($"{d.BoQuaDaCoCa} ô người đó đã có ca");
        if (d.BoQuaDayNguoi > 0) bo.Add($"{d.BoQuaDayNguoi} ô vì ca đã đủ người");
        if (d.BoQuaNgayDaQua > 0) bo.Add($"{d.BoQuaNgayDaQua} ô ngày đã qua");
        if (d.BoQuaCaNgung > 0) bo.Add($"{d.BoQuaCaNgung} ô vì ca đã ngừng sử dụng");
        TomTatBoQua = bo.Count > 0 ? $"Bỏ qua: {string.Join(", ", bo)}." : "Không có ô nào bị bỏ qua.";
    }

    private bool CoTheLap => !DangTinh && SoCaSeThem > 0;

    [RelayCommand(CanExecute = nameof(CoTheLap))]
    private void Lap() => Dong(true);

    [RelayCommand]
    private void Huy() => Dong(false);
}
