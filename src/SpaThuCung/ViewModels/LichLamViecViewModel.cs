using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Models;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>
/// Màn Xếp lịch làm việc (UC05, FR13, chỉ Quản lý). Lưới tuần: hàng là ca, cột là Thứ 2 … Chủ nhật.
/// Khung bên phải xếp một nhân viên vào cùng ca, cùng vị trí ở nhiều ngày một lượt.
/// </summary>
public partial class LichLamViecViewModel : ObservableObject, ICoTaiDuLieu
{
    private readonly INhanVienService _nhanVien;
    private readonly IHopThoaiService _hopThoai;

    public LichLamViecViewModel(INhanVienService nhanVien, IHopThoaiService hopThoai)
    {
        _nhanVien = nhanVien;
        _hopThoai = hopThoai;
        _ngayDauTuan = QuyTacLichLamViec.NgayDauTuan(QuyTacLichLamViec.HomNay);
        DatLaiChonNgay();
    }

    public Task TaiDuLieuAsync() => TaiLichAsync();

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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TuanTruocCommand))]
    [NotifyCanExecuteChangedFor(nameof(TuanSauCommand))]
    [NotifyCanExecuteChangedFor(nameof(TuanNayCommand))]
    [NotifyCanExecuteChangedFor(nameof(ThemVaoLichCommand))]
    [NotifyCanExecuteChangedFor(nameof(XoaLichCommand))]
    private bool _dangXuLy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ThemVaoLichCommand))]
    [NotifyCanExecuteChangedFor(nameof(XoaLichCommand))]
    private bool _daTai;

    // =====================================================================
    // Tuần và lưới lịch
    // =====================================================================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NhanTuan))]
    [NotifyPropertyChangedFor(nameof(LaTuanNay))]
    [NotifyCanExecuteChangedFor(nameof(TuanNayCommand))]
    private DateOnly _ngayDauTuan;

    public string NhanTuan => QuyTacLichLamViec.NhanTuan(NgayDauTuan);

    public bool LaTuanNay => NgayDauTuan == QuyTacLichLamViec.NgayDauTuan(QuyTacLichLamViec.HomNay);

    public ObservableCollection<CotNgayViewModel> CacCotNgay { get; } = new();

    public ObservableCollection<HangCaViewModel> CacHangCa { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TomTatDinhMuc))]
    [NotifyPropertyChangedFor(nameof(DuDinhMuc))]
    private int _soOLech;

    public bool DuDinhMuc => SoOLech == 0;

    public string TomTatDinhMuc => SoOLech == 0
        ? "Các ca từ hôm nay đều đủ người"
        : $"{SoOLech} ca chưa đúng định mức";

    [ObservableProperty]
    private bool _khongCoCa;

    private bool CoTheDoiTuan() => !DangXuLy;

    [RelayCommand(CanExecute = nameof(CoTheDoiTuan))]
    private Task TuanTruoc() => DoiTuanAsync(NgayDauTuan.AddDays(-7));

    [RelayCommand(CanExecute = nameof(CoTheDoiTuan))]
    private Task TuanSau() => DoiTuanAsync(NgayDauTuan.AddDays(7));

    private bool CoTheVeTuanNay() => !DangXuLy && !LaTuanNay;

    [RelayCommand(CanExecute = nameof(CoTheVeTuanNay))]
    private Task TuanNay() => DoiTuanAsync(QuyTacLichLamViec.NgayDauTuan(QuyTacLichLamViec.HomNay));

    private async Task DoiTuanAsync(DateOnly ngayDauTuan)
    {
        NgayDauTuan = ngayDauTuan;
        DatLaiChonNgay();
        ThongBao = string.Empty;
        await TaiLichAsync();
    }

    /// <summary>Tải lịch tuần đang xem; giữ lựa chọn nhân viên, ca, vị trí ở khung xếp lịch.</summary>
    private async Task<bool> TaiLichAsync()
    {
        DangXuLy = true;
        try
        {
            var kq = await _nhanVien.LayLichTuanAsync(NgayDauTuan);
            if (!kq.ThanhCong)
            {
                DaTai = false;
                HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
                return false;
            }

            DoLich(kq.DuLieu!);
            DaTai = true;
            return true;
        }
        finally
        {
            DangXuLy = false;
        }
    }

    private void DoLich(LichTuanDto lich)
    {
        var cacNgay = Enumerable.Range(0, 7).Select(i => lich.NgayDauTuan.AddDays(i)).ToList();

        CacCotNgay.Clear();
        foreach (var ngay in cacNgay) CacCotNgay.Add(new CotNgayViewModel(ngay));

        CacHangCa.Clear();
        var soLech = 0;
        foreach (var ca in lich.CacCa)
        {
            var cacO = cacNgay.Select(ngay => new OLichViewModel(ca, ngay,
                    lich.CacDong
                        .Where(x => x.MaCa == ca.MaCa && x.Ngay == ngay)
                        // Lễ tân trước, Groomer sau
                        .OrderBy(x => x.ViTri == ViTri.LeTan ? 0 : 1).ThenBy(x => x.HoTen)
                        .Select(x => new TheLichViewModel(x))
                        .ToList()))
                .ToList();
            soLech += cacO.Count(x => x.CanCanhBao);
            CacHangCa.Add(new HangCaViewModel(ca, cacO));
        }
        SoOLech = soLech;
        KhongCoCa = CacHangCa.Count == 0;

        // Khung xếp lịch: chỉ nhân viên Đang làm việc, chỉ ca đang sử dụng. Giữ lựa chọn cũ nếu còn.
        var maNvCu = NhanVienChon?.MaNv;
        DanhSachNhanVien.Clear();
        foreach (var nv in lich.NhanVienDangLam) DanhSachNhanVien.Add(nv);
        NhanVienChon = DanhSachNhanVien.FirstOrDefault(x => x.MaNv == maNvCu);

        var maCaCu = CaChon?.MaCa;
        DanhSachCa.Clear();
        foreach (var ca in lich.CacCa.Where(x => x.DangSuDung)) DanhSachCa.Add(ca);
        CaChon = DanhSachCa.FirstOrDefault(x => x.MaCa == maCaCu) ?? DanhSachCa.FirstOrDefault();
    }

    // =====================================================================
    // Khung "Xếp lịch nhiều ngày"
    // =====================================================================

    public ObservableCollection<NhanVienChonDto> DanhSachNhanVien { get; } = new();

    public ObservableCollection<CaLamViecDto> DanhSachCa { get; } = new();

    public IReadOnlyList<string> DanhSachViTri => ViTri.TatCa;

    public ObservableCollection<ChonNgayViewModel> CacNgayChon { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ThemVaoLichCommand))]
    private NhanVienChonDto? _nhanVienChon;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ThemVaoLichCommand))]
    private CaLamViecDto? _caChon;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ThemVaoLichCommand))]
    private string _viTriChon = ViTri.Groomer;

    /// <summary>Mặc định tick Thứ 2 đến Thứ 6 (bỏ các ngày đã qua).</summary>
    private void DatLaiChonNgay()
    {
        CacNgayChon.Clear();
        for (var i = 0; i < 7; i++)
            CacNgayChon.Add(new ChonNgayViewModel(NgayDauTuan.AddDays(i), i < 5));
    }

    private bool CoTheThem() =>
        DaTai && !DangXuLy && NhanVienChon is not null && CaChon is not null && !string.IsNullOrEmpty(ViTriChon);

    [RelayCommand(CanExecute = nameof(CoTheThem))]
    private async Task ThemVaoLich()
    {
        if (NhanVienChon is null || CaChon is null) return;

        var cacNgay = CacNgayChon.Where(x => x.DuocChon && x.CoTheChon).Select(x => x.Ngay).ToList();
        if (cacNgay.Count == 0)
        {
            HienThongBao("Chọn ít nhất một ngày (ngày đã qua không xếp được).", LoaiThongBao.Loi);
            return;
        }

        DangXuLy = true;
        KetQua<KetQuaXepLich> kq;
        try
        {
            kq = await _nhanVien.XepLichAsync(NhanVienChon.MaNv, CaChon.MaCa, ViTriChon, cacNgay);
        }
        finally
        {
            DangXuLy = false;
        }

        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return;
        }

        var ketQua = kq.DuLieu!;
        var loai = ketQua.DaLuu.Count == 0
            ? LoaiThongBao.Loi
            : ketQua.TrungNgay.Count > 0 || ketQua.VuotDinhMuc.Count > 0 ? LoaiThongBao.CanhBao : LoaiThongBao.ThanhCong;

        if (await TaiLichAsync()) HienThongBao(kq.ThongBao, loai);
    }

    private bool CoTheXoa(TheLichViewModel? the) => the is not null && the.XoaDuoc && DaTai && !DangXuLy;

    [RelayCommand(CanExecute = nameof(CoTheXoa))]
    private async Task XoaLich(TheLichViewModel? the)
    {
        if (the is null) return;
        if (!_hopThoai.XacNhan($"{the.NhanNutXoa}?", "Lịch làm việc")) return;

        DangXuLy = true;
        KetQua kq;
        try
        {
            kq = await _nhanVien.XoaLichAsync(the.MaLich);
        }
        finally
        {
            DangXuLy = false;
        }

        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return;
        }
        if (await TaiLichAsync()) HienThongBao(kq.ThongBao, LoaiThongBao.ThanhCong);
    }
}
