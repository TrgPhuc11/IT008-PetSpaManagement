using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>
/// Một dịch vụ của phiếu trong form nhận. Giá tra lại theo cân nặng mới; không có khoảng phù hợp hoặc dòng đã nhập tay trước đó
/// thì nhập tay giá, thời lượng, lý do (BR08, BR14).
/// </summary>
public partial class DongNhanViewModel : ObservableObject
{
    public DongNhanViewModel(DongNhanDto dong)
    {
        MaDv = dong.MaDv;
        TenDv = dong.TenDv;
        DonGiaCu = dong.DonGia;
        ThoiLuongCu = dong.ThoiLuong;
        _giaTra = dong.DonGia;
        _phutTra = dong.ThoiLuong;

        // Dòng đã nhập tay lúc lập thì giữ nhập tay, điền sẵn giá trị cũ.
        if (!string.IsNullOrWhiteSpace(dong.GhiChu))
        {
            _nhapTay = true;
            _donGiaNhap = dong.DonGia.ToString("0", CultureInfo.InvariantCulture);
            _thoiLuongNhap = dong.ThoiLuong.ToString(CultureInfo.InvariantCulture);
            _ghiChu = dong.GhiChu;
        }
    }

    /// <summary>Báo cho form tính lại tổng khi dòng đổi.</summary>
    public event Action? ThayDoi;

    public int MaDv { get; }

    public string TenDv { get; }

    public decimal DonGiaCu { get; }

    public int ThoiLuongCu { get; }

    public string CuHienThi => $"{QuyTacDichVu.TienHienThi(DonGiaCu)} đ · {ThoiLuongCu} phút";

    /// <summary>Có khoảng giá phù hợp với cân nặng mới trong bảng giá.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KhongCoGia))]
    [NotifyPropertyChangedFor(nameof(MoiHienThi))]
    [NotifyPropertyChangedFor(nameof(DuocTichNhapTay))]
    private bool _coGiaTra = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MoiHienThi))]
    private decimal _giaTra;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MoiHienThi))]
    private int _phutTra;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MoiHienThi))]
    private bool _nhapTay;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MoiHienThi))]
    private string _donGiaNhap = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MoiHienThi))]
    private string _thoiLuongNhap = string.Empty;

    [ObservableProperty]
    private string _ghiChu = string.Empty;

    partial void OnCoGiaTraChanged(bool value) => ThayDoi?.Invoke();

    partial void OnGiaTraChanged(decimal value) => ThayDoi?.Invoke();

    partial void OnPhutTraChanged(int value) => ThayDoi?.Invoke();

    partial void OnNhapTayChanged(bool value) => ThayDoi?.Invoke();

    partial void OnDonGiaNhapChanged(string value) => ThayDoi?.Invoke();

    partial void OnThoiLuongNhapChanged(string value) => ThayDoi?.Invoke();

    public bool KhongCoGia => !CoGiaTra;

    /// <summary>Chỉ được bỏ nhập tay khi bảng giá có khoảng phù hợp.</summary>
    public bool DuocTichNhapTay => CoGiaTra;

    /// <summary>Giá, thời lượng đang áp dụng; null nếu chưa hợp lệ.</summary>
    public (decimal DonGia, int ThoiLuong)? GiaTriMoi
    {
        get
        {
            if (!NhapTay) return CoGiaTra ? (GiaTra, PhutTra) : null;
            if (!DocSoNguyen(DonGiaNhap, out var gia) || !int.TryParse(ThoiLuongNhap.Trim(), out var phut)) return null;
            return (gia, phut);
        }
    }

    public string MoiHienThi => GiaTriMoi is { } v
        ? $"{QuyTacDichVu.TienHienThi(v.DonGia)} đ · {v.ThoiLuong} phút"
        : (NhapTay || KhongCoGia ? "Nhập giá, thời lượng" : "—");

    /// <summary>Dòng gửi lên Service.</summary>
    public DongDichVuNhap? DongGui()
    {
        if (!NhapTay) return new DongDichVuNhap(MaDv, false, null, null, null);
        if (GiaTriMoi is not { } v) return null;
        return new DongDichVuNhap(MaDv, true, v.DonGia, v.ThoiLuong, GhiChu);
    }

    /// <summary>Bỏ dấu chấm, dấu phẩy, khoảng trắng phân cách hàng nghìn (150.000 = 150000).</summary>
    private static bool DocSoNguyen(string? s, out decimal so)
    {
        var chuoi = new string((s ?? string.Empty).Where(c => c is not ('.' or ',' or ' ')).ToArray());
        so = 0;
        return chuoi.Length > 0 && chuoi.All(c => c is >= '0' and <= '9')
            && decimal.TryParse(chuoi, NumberStyles.None, CultureInfo.InvariantCulture, out so);
    }
}

/// <summary>
/// Hộp thoại Nhận thú cưng (UC14, BR10, BR13, BR14): nhập cân nặng thực tế (giá, thời lượng tra lại), chọn Groomer của ca,
/// chọn lồng trống (Đặt lịch, hết thì mượn Walk-in), tick khách đồng ý nếu tổng tiền đổi.
/// </summary>
public partial class NhanThuCungViewModel : HopThoaiViewModel
{
    private readonly ITheoDoiService _service;
    private readonly NhanThongTinDto _thongTin;
    private int _lanTraGia;

    public NhanThuCungViewModel(ITheoDoiService service, NhanThongTinDto thongTin)
    {
        _service = service;
        _thongTin = thongTin;

        foreach (var d in thongTin.Dong)
        {
            var dong = new DongNhanViewModel(d);
            dong.ThayDoi += TinhTong;
            CacDong.Add(dong);
        }

        CacGroomer = thongTin.Ca.Groomer;
        _groomerChon = CacGroomer.FirstOrDefault(x => x.MaNv == thongTin.Phieu.MaGroomer);

        // BR13: lồng Đặt lịch trước, hết mới mượn lồng Walk-in.
        var muon = thongTin.LongDatLich.Count == 0;
        var nguon = muon ? thongTin.LongWalkIn : thongTin.LongDatLich;
        CacLong = nguon.Select(l => new LuaChonLong(l.MaLong, $"{l.TenLong} ({l.LoaiLong})")).ToList();
        _longChon = CacLong.FirstOrDefault();
        GhiChuLong = muon
            ? (CacLong.Count == 0
                ? "Đã hết cả lồng Đặt lịch lẫn lồng Walk-in trống, chưa nhận được thú cưng."
                : "Đã hết lồng Đặt lịch trống, đang dùng lồng Walk-in trống.")
            : "Chọn lồng Đặt lịch trống.";

        _canNang = QuyTacDichVu.KgHienThi(thongTin.CanNangLucLap);
        TinhTong();
    }

    public string TieuDe => $"Nhận thú cưng: {_thongTin.Phieu.TenThuCung}";

    public string MoTa => $"{_thongTin.Phieu.MaHienThi} · {_thongTin.Phieu.KhachHienThi} · hẹn {_thongTin.Phieu.GioHenHienThi} · "
                          + $"{_thongTin.Ca.Ca.TenCa}";

    public ObservableCollection<DongNhanViewModel> CacDong { get; } = new();

    public IReadOnlyList<GroomerCaDto> CacGroomer { get; }

    public IReadOnlyList<LuaChonLong> CacLong { get; }

    public string GhiChuLong { get; }

    public string TongCuHienThi => $"{QuyTacDichVu.TienHienThi(_thongTin.Phieu.TongTien)} đ";

    // ---------- Cân nặng ----------

    [ObservableProperty]
    private string _canNang;

    partial void OnCanNangChanged(string value) => _ = TraGiaAsync();

    private async Task TraGiaAsync()
    {
        var lan = ++_lanTraGia;
        if (!DongGiaViewModel.DocKg(CanNang, out var kg) || QuyTacKhachHang.KiemTraCanNang(kg) is not null)
        {
            Loi = "Nhập cân nặng hợp lệ (lớn hơn 0, tối đa 2 chữ số thập phân).";
            return;
        }
        Loi = string.Empty;

        var kq = await _service.TraGiaLaiAsync(_thongTin.Phieu.MaPhieu, kg);
        if (lan != _lanTraGia) return;
        if (!kq.ThanhCong)
        {
            Loi = kq.ThongBao;
            return;
        }

        foreach (var g in kq.DuLieu!)
        {
            var dong = CacDong.FirstOrDefault(x => x.MaDv == g.MaDv);
            if (dong is null) continue;
            dong.CoGiaTra = g.CoGia;
            if (g.CoGia)
            {
                dong.GiaTra = g.DonGia;
                dong.PhutTra = g.ThoiLuong;
            }
            else
            {
                // Không có khoảng phù hợp: bắt buộc nhập tay.
                dong.NhapTay = true;
            }
        }
        TinhTong();
    }

    // ---------- Chọn Groomer, lồng ----------

    [ObservableProperty]
    private GroomerCaDto? _groomerChon;

    [ObservableProperty]
    private LuaChonLong? _longChon;

    // ---------- Tổng tiền ----------

    [ObservableProperty]
    private string _tongMoiHienThi = "—";

    [ObservableProperty]
    private bool _tongDoi;

    [ObservableProperty]
    private bool _khachDongY;

    [ObservableProperty]
    private string _canhBaoGio = string.Empty;

    private decimal? _tongMoi;

    private void TinhTong()
    {
        decimal tong = 0;
        var phut = 0;
        var hopLe = true;
        foreach (var d in CacDong)
        {
            if (d.GiaTriMoi is not { } v)
            {
                hopLe = false;
                continue;
            }
            tong += v.DonGia;
            phut += v.ThoiLuong;
        }

        _tongMoi = hopLe ? tong : null;
        TongMoiHienThi = hopLe ? $"{QuyTacDichVu.TienHienThi(tong)} đ · {phut} phút" : "—";
        TongDoi = hopLe && tong != _thongTin.Phieu.TongTien;
        if (!TongDoi) KhachDongY = false;

        var canhBao = hopLe ? QuyTacPhieu.CanhBaoQuaTai(_thongTin.Ca.Ca, DateTime.Now, phut, null) : new List<string>();
        CanhBaoGio = string.Join(" ", canhBao);
    }

    // ---------- Lưu ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoLoi))]
    private string _loi = string.Empty;

    public bool CoLoi => !string.IsNullOrEmpty(Loi);

    /// <summary>Thông báo kết quả để màn Theo dõi hiển thị sau khi đóng.</summary>
    public string KetQuaThongBao { get; private set; } = string.Empty;

    public bool CoCanhBaoSauNhan { get; private set; }

    [ObservableProperty]
    private bool _dangLuu;

    [RelayCommand]
    private async Task XacNhanAsync()
    {
        if (DangLuu) return;

        if (!DongGiaViewModel.DocKg(CanNang, out var kg) || QuyTacKhachHang.KiemTraCanNang(kg) is not null)
        {
            Loi = "Nhập cân nặng hợp lệ (lớn hơn 0, tối đa 2 chữ số thập phân).";
            return;
        }
        if (GroomerChon is null)
        {
            Loi = "Chọn Groomer cho phiếu.";
            return;
        }
        if (LongChon is null)
        {
            Loi = "Chọn lồng trống cho thú cưng.";
            return;
        }

        var dongGui = new List<DongDichVuNhap>();
        foreach (var d in CacDong)
        {
            var g = d.DongGui();
            if (g is null)
            {
                Loi = $"{d.TenDv}: nhập giá (số nguyên VNĐ) và thời lượng (phút) hợp lệ.";
                return;
            }
            dongGui.Add(g);
        }
        if (TongDoi && !KhachDongY)
        {
            Loi = "Tổng tiền thay đổi, hãy báo khách và tick \"khách đồng ý chi phí mới\".";
            return;
        }

        Loi = string.Empty;
        DangLuu = true;
        try
        {
            var kq = await _service.NhanThuCungAsync(new NhanThuCungMoi(
                _thongTin.Phieu.MaPhieu, kg, GroomerChon.MaNv, LongChon.MaLong, KhachDongY, dongGui));
            if (!kq.ThanhCong)
            {
                Loi = kq.ThongBao;
                return;
            }

            KetQuaThongBao = kq.ThongBao;
            CoCanhBaoSauNhan = kq.ThongBao.Contains("Lưu ý", StringComparison.Ordinal);
            Dong(true);
        }
        finally
        {
            DangLuu = false;
        }
    }

    [RelayCommand]
    private void Huy() => Dong(false);
}
