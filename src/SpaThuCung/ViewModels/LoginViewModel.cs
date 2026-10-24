using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Data;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly IDieuHuongCuaSo _dieuHuong;
    private readonly KhoiTaoCsdl _csdl;

    public LoginViewModel(IAuthService auth, IDieuHuongCuaSo dieuHuong, KhoiTaoCsdl csdl)
    {
        _auth = auth;
        _dieuHuong = dieuHuong;
        _csdl = csdl;
    }

    [ObservableProperty]
    private string _tenDangNhap = string.Empty;

    [ObservableProperty]
    private string _matKhau = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoThongBaoLoi))]
    private string _thongBaoLoi = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DangNhapCommand))]
    [NotifyPropertyChangedFor(nameof(ChuNutDangNhap))]
    private bool _dangXuLy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DangNhapCommand))]
    [NotifyPropertyChangedFor(nameof(ChuNutDangNhap))]
    private bool _dangKetNoi;

    public bool CoThongBaoLoi => !string.IsNullOrEmpty(ThongBaoLoi);

    public string ChuNutDangNhap =>
        DangKetNoi ? "Đang kết nối CSDL..." :
        DangXuLy ? "Đang đăng nhập..." : "Đăng nhập";

    // Gõ lại vào ô thì xoá thông báo lỗi.
    partial void OnTenDangNhapChanged(string value) => ThongBaoLoi = string.Empty;

    partial void OnMatKhauChanged(string value) => ThongBaoLoi = string.Empty;

    private bool CoTheDangNhap() => !DangXuLy && !DangKetNoi;

    /// <summary>
    /// Kết nối SQL Server và tạo CSDL nếu chưa có. Gọi khi cửa sổ đã hiện,
    /// nên kết nối chậm hay lỗi thì người dùng vẫn thấy màn hình và thông báo.
    /// </summary>
    public async Task KhoiTaoCsdlAsync()
    {
        if (_csdl.DaXong) return;

        DangKetNoi = true;
        try
        {
            await _csdl.ChayAsync();
        }
        catch (Exception ex)
        {
            ThongBaoLoi = "Không kết nối được SQL Server. Kiểm tra SQL Server đang chạy và "
                        + "chuỗi kết nối trong appsettings.json, rồi bấm Đăng nhập để thử lại.\n"
                        + "Chi tiết: " + ex.Message;
        }
        finally
        {
            DangKetNoi = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CoTheDangNhap))]
    private async Task DangNhapAsync()
    {
        if (string.IsNullOrWhiteSpace(TenDangNhap) || string.IsNullOrEmpty(MatKhau))
        {
            ThongBaoLoi = "Nhập tên đăng nhập và mật khẩu.";
            return;
        }

        // Lần trước kết nối CSDL lỗi thì thử lại.
        if (!_csdl.DaXong)
        {
            await KhoiTaoCsdlAsync();
            if (!_csdl.DaXong) return;
        }

        DangXuLy = true;
        ThongBaoLoi = string.Empty;
        try
        {
            var kq = await _auth.DangNhapAsync(TenDangNhap, MatKhau);
            if (kq.ThanhCong)
                _dieuHuong.MoManHinhChinh();
            else
                ThongBaoLoi = kq.ThongBao;
        }
        finally
        {
            DangXuLy = false;
        }
    }
}
