using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Models;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Một lựa chọn trong bộ lọc theo khách ("Tất cả khách" có Ma = null).</summary>
public sealed record LuaChonKhach(int? Ma, string HienThi);

/// <summary>
/// Màn Quản lý thú cưng (UC07, FR04, mọi nhân viên): tìm theo tên thú cưng, chủ, SĐT; lọc theo khách, loài, tiêm phòng;
/// thêm thú cưng cho khách, sửa thông tin và xác nhận tiêm phòng (BR06). Không đổi chủ, không xoá.
/// </summary>
public partial class ThuCungViewModel : ObservableObject, ICoTaiDuLieu
{
    private const string TatCaLoai = "Tất cả loài";

    private readonly IKhachHangService _khachHang;

    private List<ThuCungDto> _tatCa = new();
    private int _maDangSua;
    private bool _dangNapBoLoc;

    public ThuCungViewModel(IKhachHangService khachHang)
    {
        _khachHang = khachHang;
        _loaiLoc = TatCaLoai;
        _khachLoc = CacKhachLoc[0];
        _formLoai = LoaiThuCung.Cho;
    }

    public ObservableCollection<ThuCungDto> DanhSach { get; } = new();

    /// <summary>Danh sách khách cho ComboBox chọn chủ trong form thêm.</summary>
    public ObservableCollection<KhachHangDto> CacKhach { get; } = new();

    /// <summary>Bộ lọc theo khách: "Tất cả khách" rồi từng khách.</summary>
    public ObservableCollection<LuaChonKhach> CacKhachLoc { get; } = new() { new LuaChonKhach(null, "Tất cả khách") };

    public IReadOnlyList<string> CacLoaiLoc { get; } = new[] { TatCaLoai, LoaiThuCung.Cho, LoaiThuCung.Meo };

    public IReadOnlyList<string> CacLoai { get; } = LoaiThuCung.TatCa;

    // ---------- Bộ lọc, chọn dòng ----------

    [ObservableProperty]
    private string _tuKhoa = string.Empty;

    [ObservableProperty]
    private string _loaiLoc;

    [ObservableProperty]
    private LuaChonKhach _khachLoc;

    [ObservableProperty]
    private bool _chiChuaTiemPhong;

    partial void OnTuKhoaChanged(string value) => LocDanhSach();
    partial void OnLoaiLocChanged(string value) => LocDanhSach();
    partial void OnChiChuaTiemPhongChanged(bool value) => LocDanhSach();

    partial void OnKhachLocChanged(LuaChonKhach value)
    {
        if (!_dangNapBoLoc) LocDanhSach();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SuaCommand))]
    private ThuCungDto? _thuCungDuocChon;

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

    // ---------- Form ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DangMoForm))]
    [NotifyPropertyChangedFor(nameof(LaThem))]
    [NotifyPropertyChangedFor(nameof(LaSua))]
    [NotifyPropertyChangedFor(nameof(TieuDeForm))]
    private CheDoForm _cheDo = CheDoForm.Dong;

    [ObservableProperty]
    private KhachHangDto? _formChu;

    /// <summary>Tên chủ hiển thị khi sửa (không đổi chủ).</summary>
    [ObservableProperty]
    private string _formTenChu = string.Empty;

    [ObservableProperty]
    private string _formTen = string.Empty;

    [ObservableProperty]
    private string _formLoai;

    [ObservableProperty]
    private string _formCanNang = string.Empty;

    [ObservableProperty]
    private bool _formDaTiemPhong;

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
    public bool LaThem => CheDo == CheDoForm.Them;
    public bool LaSua => CheDo == CheDoForm.Sua;
    public string TieuDeForm => LaThem ? "Thêm thú cưng" : "Sửa thú cưng";

    // ---------- Tải dữ liệu ----------

    public Task TaiDuLieuAsync() => TaiLaiAsync(null);

    private async Task<bool> TaiLaiAsync(int? chonMa)
    {
        var khach = await _khachHang.LayDanhSachKhachAsync();
        if (!khach.ThanhCong)
        {
            HienThongBao(khach.ThongBao, LoaiThongBao.Loi);
            return false;
        }

        var thuCung = await _khachHang.LayDanhSachThuCungAsync();
        if (!thuCung.ThanhCong)
        {
            HienThongBao(thuCung.ThongBao, LoaiThongBao.Loi);
            return false;
        }

        NapKhach(khach.DuLieu ?? new List<KhachHangDto>());
        _tatCa = thuCung.DuLieu ?? new List<ThuCungDto>();

        var chuaTiem = _tatCa.Count(x => !x.DaTiemPhong);
        TomTat = chuaTiem == 0
            ? $"{_tatCa.Count} thú cưng"
            : $"{_tatCa.Count} thú cưng · {chuaTiem} chưa xác nhận tiêm phòng (chưa lập phiếu được)";

        LocDanhSach(chonMa);
        return true;
    }

    /// <summary>Nạp lại danh sách khách cho bộ lọc và form, giữ khách đang lọc.</summary>
    private void NapKhach(List<KhachHangDto> ds)
    {
        var maLoc = KhachLoc.Ma;
        _dangNapBoLoc = true;
        try
        {
            CacKhach.Clear();
            foreach (var kh in ds) CacKhach.Add(kh);

            while (CacKhachLoc.Count > 1) CacKhachLoc.RemoveAt(CacKhachLoc.Count - 1);
            foreach (var kh in ds) CacKhachLoc.Add(new LuaChonKhach(kh.MaKh, kh.TenVaSdt));

            KhachLoc = CacKhachLoc.FirstOrDefault(x => x.Ma == maLoc) ?? CacKhachLoc[0];
        }
        finally
        {
            _dangNapBoLoc = false;
        }
    }

    private void LocDanhSach(int? chonMa = null)
    {
        var ma = chonMa ?? ThuCungDuocChon?.MaThuCung;
        var tuKhoa = TimKiemChu.BoDau(TuKhoa.Trim());

        var kq = _tatCa.Where(x =>
            (KhachLoc.Ma is null || x.MaKh == KhachLoc.Ma)
            && (LoaiLoc == TatCaLoai || x.Loai == LoaiLoc)
            && (!ChiChuaTiemPhong || !x.DaTiemPhong)
            && TimKiemChu.Khop(tuKhoa, x.Ten, x.TenChu, x.SdtChu, x.MaHienThi));

        DanhSach.Clear();
        foreach (var tc in kq) DanhSach.Add(tc);

        ThuCungDuocChon = ma is null ? null : DanhSach.FirstOrDefault(x => x.MaThuCung == ma.Value);
    }

    // ---------- Lệnh ----------

    private bool CoChon() => ThuCungDuocChon is not null;

    [RelayCommand]
    private void Them()
    {
        if (CacKhach.Count == 0)
        {
            HienThongBao("Chưa có khách hàng nào. Thêm khách ở trang Khách hàng trước.", LoaiThongBao.CanhBao);
            return;
        }

        ThongBao = string.Empty;
        DatLaiForm();
        // Đang lọc theo một khách thì chọn sẵn khách đó làm chủ.
        FormChu = KhachLoc.Ma is int ma ? CacKhach.FirstOrDefault(x => x.MaKh == ma) : null;
        CheDo = CheDoForm.Them;
    }

    [RelayCommand(CanExecute = nameof(CoChon))]
    private void Sua()
    {
        var tc = ThuCungDuocChon;
        if (tc is null) return;

        ThongBao = string.Empty;
        DatLaiForm();
        _maDangSua = tc.MaThuCung;
        FormTenChu = $"{tc.TenChu} · {tc.SdtChu}";
        FormTen = tc.Ten;
        FormLoai = tc.Loai;
        FormCanNang = tc.CanNangHienThi;
        FormDaTiemPhong = tc.DaTiemPhong;
        FormGhiChu = tc.GhiChu ?? string.Empty;
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

        if (LaThem && FormChu is null)
        {
            FormLoi = "Chọn khách hàng (chủ thú cưng).";
            return;
        }
        if (!DongGiaViewModel.DocKg(FormCanNang, out var kg))
        {
            FormLoi = "Cân nặng phải là số kg, ví dụ 4,5.";
            return;
        }

        var nhap = new ThuCungNhap(FormChu?.MaKh ?? 0, FormTen, FormLoai, kg, FormDaTiemPhong, FormGhiChu);
        var loi = QuyTacKhachHang.KiemTraTenThuCung(nhap.Ten)
                  ?? QuyTacKhachHang.KiemTraLoai(nhap.Loai)
                  ?? QuyTacKhachHang.KiemTraCanNang(nhap.CanNang)
                  ?? QuyTacKhachHang.KiemTraGhiChu(nhap.GhiChu);
        if (loi is not null)
        {
            FormLoi = loi;
            return;
        }

        DangLuu = true;
        try
        {
            int chonMa;
            string thongBao;
            if (LaThem)
            {
                var kq = await _khachHang.ThemThuCungAsync(nhap);
                if (!kq.ThanhCong)
                {
                    FormLoi = kq.ThongBao;
                    return;
                }
                (chonMa, thongBao) = (kq.DuLieu, kq.ThongBao);
            }
            else
            {
                var kq = await _khachHang.SuaThuCungAsync(_maDangSua, nhap);
                if (!kq.ThanhCong)
                {
                    FormLoi = kq.ThongBao;
                    return;
                }
                (chonMa, thongBao) = (_maDangSua, kq.ThongBao);
            }

            var chuaTiem = !nhap.DaTiemPhong;
            CheDo = CheDoForm.Dong;
            DatLaiForm();

            // Bỏ các bộ lọc chữ/tiêm phòng để chắc chắn thấy dòng vừa lưu.
            if (TuKhoa.Length > 0) TuKhoa = string.Empty;
            if (ChiChuaTiemPhong && !chuaTiem) ChiChuaTiemPhong = false;
            if (LoaiLoc != TatCaLoai && LoaiLoc != nhap.Loai) LoaiLoc = TatCaLoai;
            if (nhap.MaKh != 0 && KhachLoc.Ma is int maLoc && maLoc != nhap.MaKh) KhachLoc = CacKhachLoc[0];

            if (await TaiLaiAsync(chonMa))
            {
                if (chuaTiem)
                    HienThongBao(thongBao + " Chưa xác nhận tiêm phòng nên chưa lập phiếu được.", LoaiThongBao.CanhBao);
                else
                    HienThongBao(thongBao, LoaiThongBao.ThanhCong);
            }
        }
        finally
        {
            DangLuu = false;
        }
    }

    private void DatLaiForm()
    {
        _maDangSua = 0;
        FormChu = null;
        FormTenChu = string.Empty;
        FormTen = string.Empty;
        FormLoai = LoaiThuCung.Cho;
        FormCanNang = string.Empty;
        FormDaTiemPhong = false;
        FormGhiChu = string.Empty;
        FormLoi = string.Empty;
    }
}
