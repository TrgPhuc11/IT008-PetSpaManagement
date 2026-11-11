using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>
/// Màn Quản lý khách hàng (UC06, FR03, mọi nhân viên): tìm theo tên hoặc SĐT, thêm, sửa; xem số lần không đến,
/// cờ cảnh báo và danh sách thú cưng của khách đang chọn. Không có xoá (NFR03).
/// </summary>
public partial class KhachHangViewModel : ObservableObject, ICoTaiDuLieu
{
    private readonly IKhachHangService _khachHang;

    private List<KhachHangDto> _tatCa = new();
    private int _maDangSua;
    private int _soLanTaiThuCung;

    public KhachHangViewModel(IKhachHangService khachHang)
    {
        _khachHang = khachHang;
    }

    public ObservableCollection<KhachHangDto> DanhSach { get; } = new();

    /// <summary>Thú cưng của khách đang chọn (panel chi tiết bên phải).</summary>
    public ObservableCollection<ThuCungDto> ThuCungCuaKhach { get; } = new();

    // ---------- Tìm kiếm, chọn dòng ----------

    [ObservableProperty]
    private string _tuKhoa = string.Empty;

    partial void OnTuKhoaChanged(string value) => LocDanhSach();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SuaCommand))]
    [NotifyPropertyChangedFor(nameof(HienChiTiet))]
    private KhachHangDto? _khachDuocChon;

    partial void OnKhachDuocChonChanged(KhachHangDto? value) => _ = TaiThuCungCuaKhachAsync(value);

    [ObservableProperty]
    private string _tomTat = string.Empty;

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
    [NotifyPropertyChangedFor(nameof(DangMoForm))]
    [NotifyPropertyChangedFor(nameof(HienChiTiet))]
    [NotifyPropertyChangedFor(nameof(TieuDeForm))]
    private CheDoForm _cheDo = CheDoForm.Dong;

    [ObservableProperty]
    private string _formHoTen = string.Empty;

    [ObservableProperty]
    private string _formSdt = string.Empty;

    [ObservableProperty]
    private string _formGhiChu = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoFormLoi))]
    private string _formLoi = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LuuCommand))]
    private bool _dangLuu;

    public bool CoFormLoi => !string.IsNullOrEmpty(FormLoi);

    public bool DangMoForm => CheDo != CheDoForm.Dong;

    /// <summary>Panel chi tiết hiện khi có khách được chọn và không mở form.</summary>
    public bool HienChiTiet => !DangMoForm && KhachDuocChon is not null;

    public string TieuDeForm => CheDo == CheDoForm.Them ? "Thêm khách hàng" : "Sửa khách hàng";

    [ObservableProperty]
    private bool _khachChuaCoThuCung;

    // ---------- Tải dữ liệu ----------

    public Task TaiDuLieuAsync() => TaiLaiAsync(null);

    private async Task<bool> TaiLaiAsync(int? chonMa)
    {
        var kq = await _khachHang.LayDanhSachKhachAsync();
        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return false;
        }

        _tatCa = kq.DuLieu ?? new List<KhachHangDto>();
        var soCanhBao = _tatCa.Count(x => x.CoCanhBao);
        TomTat = soCanhBao == 0
            ? $"{_tatCa.Count} khách hàng"
            : $"{_tatCa.Count} khách hàng · {soCanhBao} khách bị gắn cờ cảnh báo không đến";
        LocDanhSach(chonMa);
        return true;
    }

    private void LocDanhSach(int? chonMa = null)
    {
        var ma = chonMa ?? KhachDuocChon?.MaKh;
        var tuKhoa = TimKiemChu.BoDau(TuKhoa.Trim());

        DanhSach.Clear();
        foreach (var kh in _tatCa.Where(x => TimKiemChu.Khop(tuKhoa, x.HoTen, x.Sdt, x.MaHienThi)))
            DanhSach.Add(kh);

        KhachDuocChon = ma is null ? null : DanhSach.FirstOrDefault(x => x.MaKh == ma.Value);
    }

    private async Task TaiThuCungCuaKhachAsync(KhachHangDto? kh)
    {
        var lan = ++_soLanTaiThuCung;
        ThuCungCuaKhach.Clear();
        KhachChuaCoThuCung = false;
        if (kh is null) return;

        var kq = await _khachHang.LayDanhSachThuCungAsync(kh.MaKh);
        if (lan != _soLanTaiThuCung) return;
        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return;
        }

        foreach (var tc in kq.DuLieu ?? new List<ThuCungDto>())
            ThuCungCuaKhach.Add(tc);
        KhachChuaCoThuCung = ThuCungCuaKhach.Count == 0;
    }

    // ---------- Lệnh ----------

    private bool CoChon() => KhachDuocChon is not null;

    [RelayCommand]
    private void Them()
    {
        ThongBao = string.Empty;
        DatLaiForm();

        // Ô tìm đang là một SĐT thì điền sẵn: thường là tìm không thấy rồi bấm Thêm.
        var tuKhoa = TuKhoa.Trim();
        if (tuKhoa.Length > 0 && tuKhoa.All(char.IsAsciiDigit)) FormSdt = tuKhoa;

        CheDo = CheDoForm.Them;
    }

    [RelayCommand(CanExecute = nameof(CoChon))]
    private void Sua()
    {
        var kh = KhachDuocChon;
        if (kh is null) return;

        ThongBao = string.Empty;
        DatLaiForm();
        _maDangSua = kh.MaKh;
        FormHoTen = kh.HoTen;
        FormSdt = kh.Sdt;
        FormGhiChu = kh.GhiChu ?? string.Empty;
        CheDo = CheDoForm.Sua;
    }

    [RelayCommand]
    private void Huy()
    {
        CheDo = CheDoForm.Dong;
        DatLaiForm();
    }

    private bool CoTheLuu() => !DangLuu;

    [RelayCommand(CanExecute = nameof(CoTheLuu))]
    private async Task LuuAsync()
    {
        FormLoi = string.Empty;
        var nhap = new KhachHangNhap(FormHoTen, FormSdt, FormGhiChu);

        // Kiểm tra nhanh trong ViewModel; Service kiểm tra lại đầy đủ.
        var loi = QuyTacKhachHang.KiemTraHoTen(nhap.HoTen)
                  ?? QuyTacKhachHang.KiemTraSdt(nhap.Sdt)
                  ?? QuyTacKhachHang.KiemTraGhiChu(nhap.GhiChu);
        if (loi is not null)
        {
            FormLoi = loi;
            return;
        }

        DangLuu = true;
        try
        {
            if (CheDo == CheDoForm.Them)
            {
                var kq = await _khachHang.ThemKhachAsync(nhap);
                if (!kq.ThanhCong || kq.DuLieu is null)
                {
                    FormLoi = kq.ThongBao;
                    return;
                }

                // SĐT đã có hồ sơ: đóng form, bỏ lọc và mở hồ sơ cũ (FR03).
                await DongFormVaTaiLaiAsync(kq.DuLieu.MaKh, kq.ThongBao,
                    kq.DuLieu.DaCoHoSo ? LoaiThongBao.CanhBao : LoaiThongBao.ThanhCong);
            }
            else
            {
                var kq = await _khachHang.SuaKhachAsync(_maDangSua, nhap);
                if (!kq.ThanhCong)
                {
                    FormLoi = kq.ThongBao;
                    return;
                }
                await DongFormVaTaiLaiAsync(_maDangSua, kq.ThongBao, LoaiThongBao.ThanhCong);
            }
        }
        finally
        {
            DangLuu = false;
        }
    }

    private async Task DongFormVaTaiLaiAsync(int chonMa, string thongBao, LoaiThongBao loai)
    {
        CheDo = CheDoForm.Dong;
        DatLaiForm();

        // Bỏ từ khoá để chắc chắn khách vừa lưu có trong danh sách.
        if (TuKhoa.Length > 0) TuKhoa = string.Empty;

        if (await TaiLaiAsync(chonMa))
        {
            HienThongBao(thongBao, loai);
            // Danh sách thú cưng của khách đang chọn có thể vừa đổi tên chủ: tải lại.
            await TaiThuCungCuaKhachAsync(KhachDuocChon);
        }
    }

    private void DatLaiForm()
    {
        _maDangSua = 0;
        FormHoTen = string.Empty;
        FormSdt = string.Empty;
        FormGhiChu = string.Empty;
        FormLoi = string.Empty;
    }
}
