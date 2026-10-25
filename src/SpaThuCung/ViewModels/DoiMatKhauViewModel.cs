using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Đổi mật khẩu của chính người đang đăng nhập (UC02).</summary>
public partial class DoiMatKhauViewModel : ObservableObject
{
    private readonly IAuthService _auth;

    public DoiMatKhauViewModel(IAuthService auth, IPhienLamViec phien)
    {
        _auth = auth;
        TaiKhoan = $"Tài khoản: {phien.NhanVienHienTai?.TenDangNhap}";
    }

    public string TaiKhoan { get; }

    [ObservableProperty]
    private string _matKhauHienTai = string.Empty;

    [ObservableProperty]
    private string _matKhauMoi = string.Empty;

    [ObservableProperty]
    private string _nhapLai = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoLoi))]
    private string _thongBaoLoi = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoThongBaoOk))]
    private string _thongBaoOk = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LuuCommand))]
    [NotifyPropertyChangedFor(nameof(ChuNutLuu))]
    private bool _dangLuu;

    /// <summary>Đã đổi xong: khoá ô nhập, ẩn nút Lưu, nút Huỷ thành Đóng.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LuuCommand))]
    [NotifyPropertyChangedFor(nameof(CoTheNhap))]
    [NotifyPropertyChangedFor(nameof(ChuNutDong))]
    private bool _daXong;

    public bool CoLoi => !string.IsNullOrEmpty(ThongBaoLoi);

    public bool CoThongBaoOk => !string.IsNullOrEmpty(ThongBaoOk);

    public bool CoTheNhap => !DaXong;

    public string ChuNutDong => DaXong ? "Đóng" : "Huỷ";

    public string ChuNutLuu => DangLuu ? "Đang lưu..." : "Lưu mật khẩu";

    /// <summary>View lắng nghe để đóng cửa sổ.</summary>
    public event Action? YeuCauDong;

    // Gõ lại vào ô thì xoá thông báo lỗi.
    partial void OnMatKhauHienTaiChanged(string value) => ThongBaoLoi = string.Empty;

    partial void OnMatKhauMoiChanged(string value) => ThongBaoLoi = string.Empty;

    partial void OnNhapLaiChanged(string value) => ThongBaoLoi = string.Empty;

    private bool CoTheLuu() => !DangLuu && !DaXong;

    [RelayCommand(CanExecute = nameof(CoTheLuu))]
    private async Task LuuAsync()
    {
        ThongBaoLoi = string.Empty;

        // Kiểm tra nhanh trong ViewModel; quy tắc đầy đủ do AuthService kiểm tra lại.
        if (string.IsNullOrEmpty(MatKhauHienTai))
        {
            ThongBaoLoi = "Nhập mật khẩu hiện tại.";
            return;
        }
        if (MatKhauMoi != NhapLai)
        {
            ThongBaoLoi = "Nhập lại mật khẩu mới chưa khớp.";
            return;
        }

        DangLuu = true;
        try
        {
            var kq = await _auth.DoiMatKhauAsync(MatKhauHienTai, MatKhauMoi);
            if (!kq.ThanhCong)
            {
                ThongBaoLoi = kq.ThongBao;
                return;
            }

            MatKhauHienTai = string.Empty;
            MatKhauMoi = string.Empty;
            NhapLai = string.Empty;
            DaXong = true;
            ThongBaoOk = kq.ThongBao;
        }
        finally
        {
            DangLuu = false;
        }
    }

    [RelayCommand]
    private void Dong() => YeuCauDong?.Invoke();
}
