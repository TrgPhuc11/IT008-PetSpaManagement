using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Một lựa chọn trong ComboBox "Xem theo".</summary>
public sealed record LuaChonCachXem(CachXemThongKe GiaTri, string HienThi);

/// <summary>Một dòng của bảng doanh thu theo dịch vụ / Groomer, kèm thanh tỷ lệ.</summary>
public sealed record DongNhomViewModel(string Ten, string DoanhThu, int SoPhieu, string TyLe, double PhanTram);

/// <summary>Một dòng của bảng chi tiết theo ngày / tháng.</summary>
public sealed record DongKyViewModel(string Ky, int SoPhieu, int SoPhieuDaThanhToan, string DoanhThu);

/// <summary>
/// Màn Thống kê doanh thu (UC18, FR12, chỉ Quản lý): chọn khoảng thời gian (theo ngày hẹn) và xem theo ngày hoặc tháng;
/// biểu đồ doanh thu + số phiếu mỗi kỳ, doanh thu theo dịch vụ hoặc theo Groomer, số phiếu theo trạng thái.
/// </summary>
public partial class ThongKeViewModel : ObservableObject, ICoTaiDuLieu
{
    private static readonly SKColor MauCot = new(0xA9, 0x48, 0x1F);
    private static readonly SKColor MauDuong = new(0x33, 0x56, 0x1E);
    private static readonly SKColor MauChuTruc = new(0x6E, 0x62, 0x53);
    private static readonly SKColor MauLuoi = new(0xE7, 0xDC, 0xCB);

    private readonly IThongKeService _thongKe;

    private ThongKeDoanhThuDto? _ketQua;
    private int _soLanTai;

    public ThongKeViewModel(IThongKeService thongKe)
    {
        _thongKe = thongKe;

        // Mặc định: tháng hiện tại, xem theo ngày.
        var homNay = DateTime.Today;
        _tuNgay = new DateTime(homNay.Year, homNay.Month, 1);
        _denNgay = _tuNgay.Value.AddMonths(1).AddDays(-1);
        _cachXemDangChon = CacCachXem[0];
    }

    public Task TaiDuLieuAsync() => XemAsync();

    // =====================================================================
    // Bộ lọc
    // =====================================================================

    public IReadOnlyList<LuaChonCachXem> CacCachXem { get; } = new[]
    {
        new LuaChonCachXem(CachXemThongKe.TheoNgay, "Theo ngày"),
        new LuaChonCachXem(CachXemThongKe.TheoThang, "Theo tháng"),
    };

    [ObservableProperty]
    private DateTime? _tuNgay;

    [ObservableProperty]
    private DateTime? _denNgay;

    [ObservableProperty]
    private LuaChonCachXem _cachXemDangChon;

    /// <summary>Bảng phân nhóm đang xem doanh thu theo dịch vụ (true) hay theo Groomer (false).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(XemTheoGroomer))]
    [NotifyPropertyChangedFor(nameof(TieuDeNhom))]
    [NotifyPropertyChangedFor(nameof(TieuDeCotNhom))]
    private bool _xemTheoDichVu = true;

    public bool XemTheoGroomer
    {
        get => !XemTheoDichVu;
        set => XemTheoDichVu = !value;
    }

    public string TieuDeNhom => XemTheoDichVu ? "Doanh thu theo dịch vụ" : "Doanh thu theo Groomer";

    public string TieuDeCotNhom => XemTheoDichVu ? "Dịch vụ" : "Groomer";

    partial void OnXemTheoDichVuChanged(bool value) => DungBangNhom();

    /// <summary>Khoảng nhanh: chọn xong là xem luôn.</summary>
    [RelayCommand]
    private async Task ChonKhoangNhanh(string? loai)
    {
        var homNay = DateTime.Today;
        var dauThang = new DateTime(homNay.Year, homNay.Month, 1);
        (DateTime tu, DateTime den, CachXemThongKe cach) = loai switch
        {
            "7Ngay" => (homNay.AddDays(-6), homNay, CachXemThongKe.TheoNgay),
            "ThangTruoc" => (dauThang.AddMonths(-1), dauThang.AddDays(-1), CachXemThongKe.TheoNgay),
            "NamNay" => (new DateTime(homNay.Year, 1, 1), new DateTime(homNay.Year, 12, 31), CachXemThongKe.TheoThang),
            _ => (dauThang, dauThang.AddMonths(1).AddDays(-1), CachXemThongKe.TheoNgay),
        };

        TuNgay = tu;
        DenNgay = den;
        CachXemDangChon = CacCachXem.First(x => x.GiaTri == cach);
        await XemAsync();
    }

    // =====================================================================
    // Trạng thái tải, thông báo
    // =====================================================================

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(XemCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChonKhoangNhanhCommand))]
    private bool _dangTai;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoThongBao))]
    private string _thongBao = string.Empty;

    [ObservableProperty]
    private LoaiThongBao _loaiThongBao;

    public bool CoThongBao => !string.IsNullOrEmpty(ThongBao);

    /// <summary>Đã có kết quả để hiện (ẩn vùng kết quả khi lần tải đầu bị lỗi).</summary>
    [ObservableProperty]
    private bool _coKetQua;

    // =====================================================================
    // Kết quả
    // =====================================================================

    [ObservableProperty]
    private string _tongDoanhThu = "0";

    [ObservableProperty]
    private string _soPhieuDaThanhToan = "0";

    [ObservableProperty]
    private string _trungBinhMoiPhieu = "0";

    [ObservableProperty]
    private string _tongSoPhieu = "0";

    /// <summary>Ví dụ "01/11/2026 – 30/11/2026 · theo ngày".</summary>
    [ObservableProperty]
    private string _moTaKhoang = string.Empty;

    [ObservableProperty]
    private string _tieuDeBangKy = "Chi tiết theo ngày";

    [ObservableProperty]
    private ISeries[] _bieuDo = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _trucX = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _trucY = Array.Empty<Axis>();

    public ObservableCollection<DongKyViewModel> DanhSachKy { get; } = new();

    public ObservableCollection<DongNhomViewModel> DanhSachNhom { get; } = new();

    public ObservableCollection<SoPhieuTrangThaiDto> DanhSachTrangThai { get; } = new();

    [ObservableProperty]
    private bool _nhomTrong;

    // =====================================================================
    // Xem thống kê
    // =====================================================================

    private bool CoTheXem() => !DangTai;

    [RelayCommand(CanExecute = nameof(CoTheXem))]
    private async Task XemAsync()
    {
        if (TuNgay is null || DenNgay is null)
        {
            HienThongBao("Chọn đủ ngày bắt đầu và ngày kết thúc.", LoaiThongBao.Loi);
            return;
        }

        var tu = DateOnly.FromDateTime(TuNgay.Value);
        var den = DateOnly.FromDateTime(DenNgay.Value);
        var cach = CachXemDangChon.GiaTri;

        var loi = QuyTacThongKe.KiemTraKhoang(tu, den, cach);
        if (loi is not null)
        {
            HienThongBao(loi, LoaiThongBao.Loi);
            return;
        }

        var lan = ++_soLanTai;
        DangTai = true;
        try
        {
            var kq = await _thongKe.DoanhThuAsync(tu, den, cach);
            if (lan != _soLanTai) return;

            if (!kq.ThanhCong || kq.DuLieu is null)
            {
                HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
                return;
            }

            _ketQua = kq.DuLieu;
            HienKetQua(kq.DuLieu, tu, den, cach);

            // UC18: khoảng thời gian không có phiếu đã thanh toán thì doanh thu bằng 0.
            if (kq.DuLieu.SoPhieuDaThanhToan == 0)
                HienThongBao("Khoảng thời gian này chưa có phiếu đã thanh toán, doanh thu bằng 0.", LoaiThongBao.CanhBao);
            else
                ThongBao = string.Empty;
        }
        finally
        {
            if (lan == _soLanTai) DangTai = false;
        }
    }

    private void HienThongBao(string noiDung, LoaiThongBao loai)
    {
        LoaiThongBao = loai;
        ThongBao = noiDung;
    }

    private void HienKetQua(ThongKeDoanhThuDto kq, DateOnly tu, DateOnly den, CachXemThongKe cach)
    {
        TongDoanhThu = QuyTacDichVu.TienHienThi(kq.TongDoanhThu);
        SoPhieuDaThanhToan = kq.SoPhieuDaThanhToan.ToString("N0", QuyTacDichVu.Vn);
        TongSoPhieu = kq.TongSoPhieu.ToString("N0", QuyTacDichVu.Vn);
        TrungBinhMoiPhieu = kq.SoPhieuDaThanhToan == 0
            ? "0"
            : QuyTacDichVu.TienHienThi(Math.Round(kq.TongDoanhThu / kq.SoPhieuDaThanhToan, 0));

        var theo = cach == CachXemThongKe.TheoNgay ? "theo ngày" : "theo tháng";
        MoTaKhoang = $"{tu:dd/MM/yyyy} – {den:dd/MM/yyyy} · {theo}";
        TieuDeBangKy = cach == CachXemThongKe.TheoNgay ? "Chi tiết theo ngày" : "Chi tiết theo tháng";

        DanhSachKy.Clear();
        foreach (var ky in kq.TheoKy)
            DanhSachKy.Add(new DongKyViewModel(ky.Nhan, ky.SoPhieu, ky.SoPhieuDaThanhToan, QuyTacDichVu.TienHienThi(ky.DoanhThu)));

        DanhSachTrangThai.Clear();
        foreach (var tt in kq.TheoTrangThai)
            DanhSachTrangThai.Add(tt);

        DungBieuDo(kq);
        DungBangNhom();
        CoKetQua = true;
    }

    /// <summary>Cột: doanh thu mỗi kỳ (trục trái). Đường: số phiếu mỗi kỳ (trục phải).</summary>
    private void DungBieuDo(ThongKeDoanhThuDto kq)
    {
        BieuDo = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Doanh thu",
                Values = kq.TheoKy.Select(k => (double)k.DoanhThu).ToArray(),
                Fill = new SolidColorPaint(MauCot),
                MaxBarWidth = 28,
                ScalesYAt = 0,
            },
            new LineSeries<int>
            {
                Name = "Số phiếu",
                Values = kq.TheoKy.Select(k => k.SoPhieu).ToArray(),
                Stroke = new SolidColorPaint(MauDuong, 2),
                Fill = null,
                GeometrySize = 6,
                GeometryStroke = new SolidColorPaint(MauDuong, 2),
                GeometryFill = new SolidColorPaint(SKColors.White),
                LineSmoothness = 0,
                ScalesYAt = 1,
            },
        };

        TrucX = new[]
        {
            new Axis
            {
                Labels = kq.TheoKy.Select(k => k.Nhan).ToArray(),
                LabelsPaint = new SolidColorPaint(MauChuTruc),
                TextSize = 12,
                MinStep = 1,
            },
        };

        TrucY = new[]
        {
            new Axis
            {
                MinLimit = 0,
                Labeler = v => RutGonTien(v),
                LabelsPaint = new SolidColorPaint(MauChuTruc),
                SeparatorsPaint = new SolidColorPaint(MauLuoi, 1),
                TextSize = 12,
            },
            new Axis
            {
                MinLimit = 0,
                MinStep = 1,
                Position = AxisPosition.End,
                Labeler = v => v.ToString("N0", QuyTacDichVu.Vn),
                LabelsPaint = new SolidColorPaint(MauDuong),
                ShowSeparatorLines = false,
                TextSize = 12,
            },
        };
    }

    /// <summary>Nhãn trục tiền gọn: 1.500.000 → "1,5 tr", 250.000 → "250 N".</summary>
    private static string RutGonTien(double v)
    {
        if (Math.Abs(v) >= 1_000_000) return (v / 1_000_000).ToString("0.#", QuyTacDichVu.Vn) + " tr";
        if (Math.Abs(v) >= 1_000) return (v / 1_000).ToString("0.#", QuyTacDichVu.Vn) + " N";
        return v.ToString("0", QuyTacDichVu.Vn);
    }

    private void DungBangNhom()
    {
        DanhSachNhom.Clear();
        if (_ketQua is null)
        {
            NhomTrong = true;
            return;
        }

        var ds = XemTheoDichVu ? _ketQua.TheoDichVu : _ketQua.TheoGroomer;
        var tong = ds.Sum(x => x.DoanhThu);
        foreach (var x in ds)
        {
            var phanTram = tong == 0 ? 0 : (double)(x.DoanhThu / tong * 100);
            DanhSachNhom.Add(new DongNhomViewModel(x.Ten, QuyTacDichVu.TienHienThi(x.DoanhThu), x.SoPhieu,
                phanTram.ToString("0.#", QuyTacDichVu.Vn) + "%", phanTram));
        }
        NhomTrong = DanhSachNhom.Count == 0;
    }
}
