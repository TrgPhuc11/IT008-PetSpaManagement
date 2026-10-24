using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Models;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

public enum LoaiThongBao
{
    ThanhCong,
    CanhBao,
    Loi,
}

public enum CheDoForm
{
    Dong,
    Them,
    Sua,
    DatLaiMatKhau,
}

public partial class NhanVienViewModel : ObservableObject, ICoTaiDuLieu
{
    private readonly INhanVienService _nhanVien;
    private readonly IAuthService _auth;
    private readonly IPhienLamViec _phien;
    private readonly IHopThoaiService _hopThoai;

    private List<NhanVienDto> _tatCa = new();

    // Nhân viên đang được sửa / đặt lại mật khẩu (giữ lại để đổi dòng chọn không ảnh hưởng form).
    private int _maDangThaoTac;

    public NhanVienViewModel(INhanVienService nhanVien, IAuthService auth, IPhienLamViec phien, IHopThoaiService hopThoai)
    {
        _nhanVien = nhanVien;
        _auth = auth;
        _phien = phien;
        _hopThoai = hopThoai;
    }

    public ObservableCollection<NhanVienDto> DanhSach { get; } = new();

    public IReadOnlyList<string> DanhSachVaiTro { get; } = new[] { VaiTro.NhanVien, VaiTro.QuanLy };

    // ---------- Tìm kiếm, chọn dòng ----------

    [ObservableProperty]
    private string _tuKhoa = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SuaCommand))]
    [NotifyCanExecuteChangedFor(nameof(DoiTrangThaiCommand))]
    [NotifyCanExecuteChangedFor(nameof(DatLaiMatKhauCommand))]
    private NhanVienDto? _nhanVienDuocChon;

    partial void OnTuKhoaChanged(string value) => LocDanhSach();

    // ---------- Hộp thông báo ----------

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

    // ---------- Form bên phải ----------

    [ObservableProperty]
    private CheDoForm _cheDo = CheDoForm.Dong;

    [ObservableProperty]
    private string _formHoTen = string.Empty;

    [ObservableProperty]
    private string _formSdt = string.Empty;

    [ObservableProperty]
    private string _formTenDangNhap = string.Empty;

    [ObservableProperty]
    private string _formVaiTro = VaiTro.NhanVien;

    [ObservableProperty]
    private string _formMatKhau = string.Empty;

    [ObservableProperty]
    private string _formNhapLai = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoFormLoi))]
    private string _formLoi = string.Empty;

    [ObservableProperty]
    private string _dongNhanVienDatLai = string.Empty;

    /// <summary>false khi đang sửa chính tài khoản đăng nhập (không cho đổi vai trò).</summary>
    [ObservableProperty]
    private bool _choPhepDoiVaiTro = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LuuCommand))]
    private bool _dangLuu;

    public bool CoFormLoi => !string.IsNullOrEmpty(FormLoi);

    public bool DangMoForm => CheDo != CheDoForm.Dong;
    public bool LaThem => CheDo == CheDoForm.Them;
    public bool LaSua => CheDo == CheDoForm.Sua;
    public bool LaDatLai => CheDo == CheDoForm.DatLaiMatKhau;
    public bool LaThemHoacSua => CheDo is CheDoForm.Them or CheDoForm.Sua;
    public bool CoOMatKhau => CheDo is CheDoForm.Them or CheDoForm.DatLaiMatKhau;

    public string TieuDeForm => CheDo switch
    {
        CheDoForm.Them => "Thêm nhân viên",
        CheDoForm.Sua => "Sửa nhân viên",
        CheDoForm.DatLaiMatKhau => "Đặt lại mật khẩu",
        _ => string.Empty,
    };

    public string NhanMatKhau => CheDo == CheDoForm.Them ? "Mật khẩu ban đầu *" : "Mật khẩu mới *";

    partial void OnCheDoChanged(CheDoForm value)
    {
        OnPropertyChanged(nameof(DangMoForm));
        OnPropertyChanged(nameof(LaThem));
        OnPropertyChanged(nameof(LaSua));
        OnPropertyChanged(nameof(LaDatLai));
        OnPropertyChanged(nameof(LaThemHoacSua));
        OnPropertyChanged(nameof(CoOMatKhau));
        OnPropertyChanged(nameof(TieuDeForm));
        OnPropertyChanged(nameof(NhanMatKhau));
    }

    // ---------- Tải dữ liệu ----------

    public async Task TaiDuLieuAsync() => await TaiLaiAsync(null);

    /// <summary>Tải lại danh sách từ CSDL; chonMa là nhân viên cần chọn lại (null: giữ dòng đang chọn).</summary>
    private async Task<bool> TaiLaiAsync(int? chonMa)
    {
        var kq = await _nhanVien.LayDanhSachAsync();
        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return false;
        }

        _tatCa = kq.DuLieu ?? new List<NhanVienDto>();
        LocDanhSach(chonMa);
        return true;
    }

    /// <summary>Lọc trên danh sách đã tải, không phân biệt hoa thường và dấu.</summary>
    private void LocDanhSach(int? chonMa = null)
    {
        // Xoá danh sách làm DataGrid đẩy NhanVienDuocChon = null, nên nhớ mã trước.
        var ma = chonMa ?? NhanVienDuocChon?.MaNv;
        var tuKhoa = BoDau(TuKhoa.Trim());

        IEnumerable<NhanVienDto> kq = _tatCa;
        if (tuKhoa.Length > 0)
        {
            kq = kq.Where(x => BoDau(x.HoTen).Contains(tuKhoa)
                            || BoDau(x.Sdt ?? string.Empty).Contains(tuKhoa)
                            || BoDau(x.TenDangNhap).Contains(tuKhoa)
                            || BoDau(x.MaHienThi).Contains(tuKhoa));
        }

        DanhSach.Clear();
        foreach (var nv in kq)
            DanhSach.Add(nv);

        NhanVienDuocChon = ma is null ? null : DanhSach.FirstOrDefault(x => x.MaNv == ma.Value);
    }

    /// <summary>Bỏ dấu tiếng Việt và hạ chữ thường để so khớp.</summary>
    private static string BoDau(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        var chuan = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(chuan.Length);
        foreach (var c in chuan)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(c == 'đ' ? 'd' : c);
        }
        return sb.ToString();
    }

    // ---------- Lệnh trên thanh công cụ ----------

    private bool CoChon() => NhanVienDuocChon is not null;

    [RelayCommand]
    private void Them()
    {
        ThongBao = string.Empty;
        DatLaiForm();
        FormVaiTro = VaiTro.NhanVien;
        ChoPhepDoiVaiTro = true;
        CheDo = CheDoForm.Them;
    }

    [RelayCommand(CanExecute = nameof(CoChon))]
    private void Sua()
    {
        var nv = NhanVienDuocChon;
        if (nv is null) return;

        ThongBao = string.Empty;
        DatLaiForm();
        _maDangThaoTac = nv.MaNv;
        FormHoTen = nv.HoTen;
        FormSdt = nv.Sdt ?? string.Empty;
        FormTenDangNhap = nv.TenDangNhap;
        FormVaiTro = nv.VaiTro;
        ChoPhepDoiVaiTro = nv.MaNv != _phien.NhanVienHienTai?.MaNv;
        CheDo = CheDoForm.Sua;
    }

    [RelayCommand(CanExecute = nameof(CoChon))]
    private void DatLaiMatKhau()
    {
        var nv = NhanVienDuocChon;
        if (nv is null) return;

        if (nv.DaNghiViec)
        {
            HienThongBao($"{nv.HoTen} đã nghỉ việc, không đặt lại mật khẩu.", LoaiThongBao.CanhBao);
            return;
        }

        ThongBao = string.Empty;
        DatLaiForm();
        _maDangThaoTac = nv.MaNv;
        DongNhanVienDatLai = $"Nhân viên: {nv.HoTen} ({nv.TenDangNhap})";
        CheDo = CheDoForm.DatLaiMatKhau;
    }

    [RelayCommand(CanExecute = nameof(CoChon))]
    private async Task DoiTrangThaiAsync()
    {
        var nv = NhanVienDuocChon;
        if (nv is null) return;

        if (nv.MaNv == _phien.NhanVienHienTai?.MaNv)
        {
            HienThongBao("Không thể đổi trạng thái/vai trò của tài khoản đang đăng nhập.", LoaiThongBao.Loi);
            return;
        }

        // Chuyển sang Đã nghỉ việc thì hỏi lại; chuyển ngược lại thì làm luôn.
        if (!nv.DaNghiViec)
        {
            var dongY = _hopThoai.XacNhan(
                $"Chuyển {nv.HoTen} sang Đã nghỉ việc? Nhân viên sẽ không đăng nhập được nữa.",
                "Đổi trạng thái");
            if (!dongY) return;
        }

        var kq = await _nhanVien.DoiTrangThaiAsync(nv.MaNv);
        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return;
        }

        if (await TaiLaiAsync(nv.MaNv))
            HienThongBao(kq.ThongBao, LoaiThongBao.ThanhCong);
    }

    // ---------- Lệnh trong form ----------

    [RelayCommand]
    private void Huy()
    {
        CheDo = CheDoForm.Dong;
        DatLaiForm();
    }

    [RelayCommand(CanExecute = nameof(CoTheLuu))]
    private async Task LuuAsync()
    {
        FormLoi = string.Empty;

        // Kiểm tra nhanh trong ViewModel; quy tắc đầy đủ do Service kiểm tra lại.
        if (LaThemHoacSua && string.IsNullOrWhiteSpace(FormHoTen))
        {
            FormLoi = "Nhập họ tên.";
            return;
        }
        if (LaThem && string.IsNullOrWhiteSpace(FormTenDangNhap))
        {
            FormLoi = "Nhập tên đăng nhập.";
            return;
        }
        if (CoOMatKhau)
        {
            if (string.IsNullOrEmpty(FormMatKhau))
            {
                FormLoi = "Nhập mật khẩu.";
                return;
            }
            if (FormMatKhau != FormNhapLai)
            {
                FormLoi = "Hai lần nhập mật khẩu không khớp.";
                return;
            }
        }

        DangLuu = true;
        try
        {
            switch (CheDo)
            {
                case CheDoForm.Them:
                {
                    var kq = await _nhanVien.ThemAsync(new NhanVienMoi(
                        FormHoTen, FormSdt, FormTenDangNhap, FormVaiTro, FormMatKhau));
                    await XuLyKetQuaLuuAsync(kq.ThanhCong, kq.ThongBao, kq.DuLieu?.MaNv);
                    break;
                }
                case CheDoForm.Sua:
                {
                    var kq = await _nhanVien.CapNhatAsync(_maDangThaoTac, FormHoTen, FormSdt, FormVaiTro);
                    await XuLyKetQuaLuuAsync(kq.ThanhCong, kq.ThongBao, _maDangThaoTac);
                    break;
                }
                case CheDoForm.DatLaiMatKhau:
                {
                    var kq = await _auth.DatLaiMatKhauAsync(_maDangThaoTac, FormMatKhau);
                    await XuLyKetQuaLuuAsync(kq.ThanhCong, kq.ThongBao, _maDangThaoTac);
                    break;
                }
            }
        }
        finally
        {
            DangLuu = false;
        }
    }

    private bool CoTheLuu() => !DangLuu;

    /// <summary>Lỗi thì giữ form và hiện lỗi trong panel; thành công thì đóng form, tải lại, chọn lại dòng, báo ở hộp trên.</summary>
    private async Task XuLyKetQuaLuuAsync(bool thanhCong, string thongBao, int? chonMa)
    {
        if (!thanhCong)
        {
            FormLoi = thongBao;
            return;
        }

        CheDo = CheDoForm.Dong;
        DatLaiForm();

        if (await TaiLaiAsync(chonMa))
            HienThongBao(thongBao, LoaiThongBao.ThanhCong);
    }

    private void DatLaiForm()
    {
        _maDangThaoTac = 0;
        FormHoTen = string.Empty;
        FormSdt = string.Empty;
        FormTenDangNhap = string.Empty;
        FormVaiTro = VaiTro.NhanVien;
        FormMatKhau = string.Empty;
        FormNhapLai = string.Empty;
        FormLoi = string.Empty;
        DongNhanVienDatLai = string.Empty;
        ChoPhepDoiVaiTro = true;
    }
}
