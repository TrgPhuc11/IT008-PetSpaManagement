using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Models;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Một lựa chọn trong ComboBox lọc trạng thái; TrangThai = null nghĩa là tất cả.</summary>
public sealed record LuaChonTrangThaiPhieu(string? TrangThai, string HienThi);

/// <summary>
/// Màn Theo dõi và Lịch hẹn (UC14, UC15, FR09): tab "Đang ở tiệm" (thú cưng đang ở tiệm kèm tình trạng lồng) và tab
/// "Lịch hẹn" (phiếu theo ngày). Từ phiếu đang chọn: nhận thú cưng, bắt đầu chăm sóc, hoàn tất, đổi lồng.
/// </summary>
public partial class TheoDoiViewModel : ObservableObject, ICoTaiDuLieu
{
    private readonly ITheoDoiService _service;
    private readonly IHopThoaiService _hopThoai;

    // Tăng mỗi lần tải, để bỏ kết quả của lần tải cũ về muộn.
    private int _lanTaiLich;

    public TheoDoiViewModel(ITheoDoiService service, IHopThoaiService hopThoai)
    {
        _service = service;
        _hopThoai = hopThoai;

        CacTrangThai = new List<LuaChonTrangThaiPhieu> { new(null, "Tất cả trạng thái") }
            .Concat(TrangThaiPhieu.TatCa.Select(t => new LuaChonTrangThaiPhieu(t, t)))
            .ToList();
        _trangThaiLoc = CacTrangThai[0];
        _ngayLich = DateTime.Today;
    }

    public IReadOnlyList<LuaChonTrangThaiPhieu> CacTrangThai { get; }

    public ObservableCollection<PhieuTheoDoiDto> DangOTiem { get; } = new();

    public ObservableCollection<PhieuTheoDoiDto> LichHen { get; } = new();

    public ObservableCollection<LongTinhTrangDto> LongDatLich { get; } = new();

    public ObservableCollection<LongTinhTrangDto> LongWalkIn { get; } = new();

    // ---------- Thông báo ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoThongBao))]
    private string _thongBao = string.Empty;

    [ObservableProperty]
    private LoaiThongBao _loaiThongBao;

    public bool CoThongBao => !string.IsNullOrEmpty(ThongBao);

    private void HienThongBao(string noiDung, LoaiThongBao loai)
    {
        LoaiThongBao = loai;
        ThongBao = noiDung;
    }

    // ---------- Tóm tắt lồng ----------

    [ObservableProperty]
    private string _tomTatLongDatLich = string.Empty;

    [ObservableProperty]
    private string _tomTatLongWalkIn = string.Empty;

    [ObservableProperty]
    private string _tieuDeLich = string.Empty;

    // ---------- Chọn ngày, lọc ----------

    [ObservableProperty]
    private DateTime _ngayLich;

    partial void OnNgayLichChanged(DateTime value) => _ = TaiLichAsync();

    [ObservableProperty]
    private LuaChonTrangThaiPhieu _trangThaiLoc;

    partial void OnTrangThaiLocChanged(LuaChonTrangThaiPhieu value) => _ = TaiLichAsync();

    [RelayCommand]
    private void NgayTruoc() => NgayLich = NgayLich.AddDays(-1);

    [RelayCommand]
    private void NgaySau() => NgayLich = NgayLich.AddDays(1);

    [RelayCommand]
    private void HomNay() => NgayLich = DateTime.Today;

    // ---------- Phiếu đang chọn ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoPhieuChon))]
    [NotifyCanExecuteChangedFor(nameof(NhanThuCungCommand))]
    [NotifyCanExecuteChangedFor(nameof(BatDauCommand))]
    [NotifyCanExecuteChangedFor(nameof(HoanTatCommand))]
    [NotifyCanExecuteChangedFor(nameof(DoiLongCommand))]
    private PhieuTheoDoiDto? _phieuChon;

    public bool CoPhieuChon => PhieuChon is not null;

    // ---------- Tải dữ liệu ----------

    public async Task TaiDuLieuAsync()
    {
        await TaiDangOTiemAsync();
        await TaiLichAsync();
    }

    [RelayCommand]
    private async Task TaiLaiAsync()
    {
        PhieuChon = null;
        await TaiDuLieuAsync();
    }

    private async Task TaiDangOTiemAsync()
    {
        var kq = await _service.LayDangOTiemAsync();
        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return;
        }

        DangOTiem.Clear();
        foreach (var p in kq.DuLieu!) DangOTiem.Add(p);

        var long_ = await _service.LayTinhTrangLongAsync();
        if (!long_.ThanhCong)
        {
            HienThongBao(long_.ThongBao, LoaiThongBao.Loi);
            return;
        }

        LongDatLich.Clear();
        LongWalkIn.Clear();
        foreach (var l in long_.DuLieu!)
            (l.LoaiLong == LoaiLong.DatLich ? LongDatLich : LongWalkIn).Add(l);
        TomTatLongDatLich = $"Lồng Đặt lịch: {LongDatLich.Count(x => x.Trong)}/{LongDatLich.Count} trống";
        TomTatLongWalkIn = $"Lồng Walk-in: {LongWalkIn.Count(x => x.Trong)}/{LongWalkIn.Count} trống";
    }

    private async Task TaiLichAsync()
    {
        var lan = ++_lanTaiLich;
        var kq = await _service.LayLichHenAsync(NgayLich, TrangThaiLoc.TrangThai);
        if (lan != _lanTaiLich) return;

        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return;
        }

        LichHen.Clear();
        foreach (var p in kq.DuLieu!) LichHen.Add(p);
        TieuDeLich = $"Ngày {NgayLich:dd/MM/yyyy}: {LichHen.Count} phiếu";
    }

    // ---------- Thao tác trên phiếu ----------

    private bool CoTheNhan() => PhieuChon?.CoTheNhan == true;

    private bool CoTheBatDau() => PhieuChon?.CoTheBatDau == true;

    private bool CoTheHoanTat() => PhieuChon?.CoTheHoanTat == true;

    private bool CoTheDoiLong() => PhieuChon?.CoTheDoiLong == true;

    [RelayCommand(CanExecute = nameof(CoTheNhan))]
    private async Task NhanThuCungAsync()
    {
        var phieu = PhieuChon;
        if (phieu is null) return;

        var tt = await _service.LayThongTinNhanAsync(phieu.MaPhieu);
        if (!tt.ThanhCong)
        {
            HienThongBao(tt.ThongBao, LoaiThongBao.Loi);
            return;
        }

        var vm = new NhanThuCungViewModel(_service, tt.DuLieu!);
        if (_hopThoai.MoHopThoai(vm) == true)
        {
            HienThongBao(vm.KetQuaThongBao, vm.CoCanhBaoSauNhan ? LoaiThongBao.CanhBao : LoaiThongBao.ThanhCong);
            await TaiLaiAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(CoTheBatDau))]
    private async Task BatDauAsync()
    {
        var phieu = PhieuChon;
        if (phieu is null) return;
        await ChayAsync(_service.BatDauChamSocAsync(phieu.MaPhieu));
    }

    [RelayCommand(CanExecute = nameof(CoTheHoanTat))]
    private async Task HoanTatAsync()
    {
        var phieu = PhieuChon;
        if (phieu is null) return;
        if (!_hopThoai.XacNhan($"Groomer đã báo xong {phieu.TenThuCung}? Phiếu sẽ chuyển sang Hoàn tất chăm sóc.", "Hoàn tất chăm sóc"))
            return;
        await ChayAsync(_service.HoanTatChamSocAsync(phieu.MaPhieu));
    }

    [RelayCommand(CanExecute = nameof(CoTheDoiLong))]
    private async Task DoiLongAsync()
    {
        var phieu = PhieuChon;
        if (phieu is null) return;

        var ds = await _service.LayLongDeDoiAsync(phieu.MaPhieu);
        if (!ds.ThanhCong)
        {
            HienThongBao(ds.ThongBao, LoaiThongBao.Loi);
            return;
        }
        if (ds.DuLieu!.Count == 0)
        {
            HienThongBao("Hiện không còn lồng trống nào để đổi.", LoaiThongBao.CanhBao);
            return;
        }

        var vm = new DoiLongViewModel(_service, phieu, ds.DuLieu);
        if (_hopThoai.MoHopThoai(vm) == true)
        {
            HienThongBao(vm.KetQuaThongBao, LoaiThongBao.ThanhCong);
            await TaiLaiAsync();
        }
    }

    private async Task ChayAsync(Task<KetQua> viec)
    {
        var kq = await viec;
        HienThongBao(kq.ThongBao, kq.ThanhCong ? LoaiThongBao.ThanhCong : LoaiThongBao.Loi);
        if (kq.ThanhCong) await TaiLaiAsync();
    }
}
