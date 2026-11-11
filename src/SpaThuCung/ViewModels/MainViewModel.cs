using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using SpaThuCung.Services;
using Wpf.Ui.Controls;

namespace SpaThuCung.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IPhienLamViec _phien;
    private readonly IDieuHuongCuaSo _dieuHuong;
    private readonly IServiceProvider _sp;
    private bool _daHuy;

    public MainViewModel(IPhienLamViec phien, IDieuHuongCuaSo dieuHuong, IServiceProvider sp)
    {
        _phien = phien;
        _dieuHuong = dieuHuong;
        _sp = sp;

        // Một ListBox duy nhất: "Quản lý" là nhãn nhóm không chọn được, chỉ có khi là Quản lý.
        var ds = new List<MucMenu>
        {
            new("Trang chủ", LoaiMuc.TrangChu, SymbolRegular.Home24),
            new("Dịch vụ & bảng giá", LoaiMuc.DichVu, SymbolRegular.Tag24),
        };
        if (_phien.LaQuanLy)
        {
            ds.Add(new MucMenu("Quản lý", LoaiMuc.TieuDeNhom));
            ds.Add(new MucMenu("Quản lý nhân viên", LoaiMuc.NhanVien, SymbolRegular.People24));
            ds.Add(new MucMenu("Lịch làm việc", LoaiMuc.LichLamViec, SymbolRegular.CalendarLtr24));
            ds.Add(new MucMenu("Thống kê doanh thu", LoaiMuc.ThongKe, SymbolRegular.DataBarVertical24));
            ds.Add(new MucMenu("Cấu hình hệ thống", LoaiMuc.CauHinh, SymbolRegular.Settings24));
        }
        DanhSachMenu = ds;

        _phien.ThongTinThayDoi += KhiThongTinThayDoi;

        // Vào là mở Trang chủ.
        MucDangChon = ds[0];
    }

    public IReadOnlyList<MucMenu> DanhSachMenu { get; }

    public string HoTen => _phien.NhanVienHienTai?.HoTen ?? string.Empty;

    public string VaiTro => _phien.NhanVienHienTai?.VaiTro ?? string.Empty;

    /// <summary>Menu trái đang mở rộng (232px) hay thu gọn chỉ còn icon (52px). Chỉ trong phiên.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MenuThuGon))]
    [NotifyPropertyChangedFor(nameof(ChieuRongMenu))]
    private bool _menuMoRong = true;

    public bool MenuThuGon => !MenuMoRong;

    public GridLength ChieuRongMenu => new(MenuMoRong ? 232 : 52);

    [RelayCommand]
    private void DoiTrangThaiMenu() => MenuMoRong = !MenuMoRong;

    [ObservableProperty]
    private MucMenu? _mucDangChon;

    /// <summary>ViewModel của màn hình đang hiển thị (View ghép bằng DataTemplate).</summary>
    [ObservableProperty]
    private object? _manHinhHienTai;

    partial void OnMucDangChonChanged(MucMenu? value)
    {
        if (value is null || value.LaTieuDe) return;
        _ = MoManHinhAsync(value);
    }

    private async Task MoManHinhAsync(MucMenu muc)
    {
        object? vm = null;
        if (muc.Loai == LoaiMuc.TrangChu)
            vm = _sp.GetRequiredService<TrangChuViewModel>();
        else if (muc.Loai == LoaiMuc.DichVu)
            vm = _sp.GetRequiredService<DichVuViewModel>();
        else if (muc.Loai == LoaiMuc.NhanVien && _phien.LaQuanLy)
            vm = _sp.GetRequiredService<NhanVienViewModel>();
        else if (muc.Loai == LoaiMuc.LichLamViec && _phien.LaQuanLy)
            vm = _sp.GetRequiredService<LichLamViecViewModel>();
        else if (muc.Loai == LoaiMuc.CauHinh && _phien.LaQuanLy)
            vm = _sp.GetRequiredService<CauHinhViewModel>();
        else if (muc.Loai == LoaiMuc.ThongKe && _phien.LaQuanLy)
            vm = _sp.GetRequiredService<ThongKeViewModel>();

        if (vm is null) return;

        ManHinhHienTai = vm;

        if (vm is ICoTaiDuLieu coDuLieu)
        {
            try
            {
                await coDuLieu.TaiDuLieuAsync();
            }
            catch (Exception)
            {
                // ViewModel tự hiển thị lỗi tải dữ liệu; không để async void làm sập app.
            }
        }
    }

    private void KhiThongTinThayDoi()
    {
        OnPropertyChanged(nameof(HoTen));
        OnPropertyChanged(nameof(VaiTro));
    }

    public void Dispose()
    {
        if (_daHuy) return;
        _daHuy = true;
        _phien.ThongTinThayDoi -= KhiThongTinThayDoi;
    }

    [RelayCommand]
    private void DoiMatKhau() => _dieuHuong.MoDoiMatKhau();

    [RelayCommand]
    private void DangXuat()
    {
        Dispose();
        _phien.DangXuat();
        _dieuHuong.VeDangNhap();
    }
}
