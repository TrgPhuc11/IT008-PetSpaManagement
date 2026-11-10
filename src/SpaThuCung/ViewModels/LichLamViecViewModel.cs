using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Models;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>
/// Màn Lịch làm việc (UC05, FR13, chỉ Quản lý), xem theo nhân viên: hàng là nhân viên, cột là ngày trong kỳ
/// (Tuần / Tháng / Khoảng ngày tối đa 62 ngày). Chọn ô của MỘT nhân viên rồi phân công, gỡ ca; định mức ca là cứng
/// (Service là nguồn chuẩn); có Hoàn tác trong phiên và Lặp lịch tuần.
/// </summary>
public partial class LichLamViecViewModel : ObservableObject, ICoTaiDuLieu
{
    public const double ChieuRongTenCot = 180;
    public const double ChieuRongORong = 120;
    public const double ChieuRongOGon = 28;
    public const double ChieuRongCotRong = 124;
    public const double ChieuRongCotGon = 32;

    private const int SoNgayRong = 10;
    private const int SoHoanTacToiDa = 20;

    private readonly INhanVienService _nhanVien;
    private readonly IHopThoaiService _hopThoai;

    private readonly List<OLichViewModel> _dangChon = new();
    private readonly List<MucHoanTac> _hoanTac = new();

    // Dữ liệu của kỳ đang xem (lấy từ Service).
    private List<CaLamViecDto> _cacCa = new();
    private List<DongLichDto> _cacDong = new();
    private List<NhanVienLichDto> _nhanVienDangLam = new();

    private DateOnly _moc;
    private DateOnly _tu;
    private DateOnly _den;
    private DateOnly _tuKhoang;
    private DateOnly _denKhoang;
    private bool _dangDatNgay;
    private int _soLanTai;
    private (DateOnly Ngay, int MaCa)? _goiY;

    public LichLamViecViewModel(INhanVienService nhanVien, IHopThoaiService hopThoai)
    {
        _nhanVien = nhanVien;
        _hopThoai = hopThoai;

        var homNay = QuyTacLichLamViec.HomNay;
        _moc = homNay;
        _tuKhoang = homNay;
        _denKhoang = new DateOnly(homNay.Year, homNay.Month, DateTime.DaysInMonth(homNay.Year, homNay.Month));
        (_tu, _den) = TinhKy();
        _nhanKy = TinhNhan();

        _tuNgayChon = _tuKhoang.ToDateTime(TimeOnly.MinValue);
        _denNgayChon = _denKhoang.ToDateTime(TimeOnly.MinValue);
        _viTriDangChon = CacViTriChon[0];
    }

    public Task TaiDuLieuAsync() => TaiLichAsync();

    // =====================================================================
    // Thông báo
    // =====================================================================

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
    [NotifyCanExecuteChangedFor(nameof(PhanCongCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(HoanTacCommand))]
    [NotifyCanExecuteChangedFor(nameof(LapLichTuanCommand))]
    private bool _dangXuLy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LapLichTuanCommand))]
    private bool _daTai;

    // =====================================================================
    // Kỳ đang xem
    // =====================================================================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LaCheDoTuan))]
    [NotifyPropertyChangedFor(nameof(LaCheDoThang))]
    [NotifyPropertyChangedFor(nameof(LaCheDoKhoang))]
    [NotifyPropertyChangedFor(nameof(KhongPhaiKhoang))]
    [NotifyPropertyChangedFor(nameof(NhanKy))]
    [NotifyCanExecuteChangedFor(nameof(LapLichTuanCommand))]
    private CheDoLich _cheDo = CheDoLich.Tuan;

    public bool LaCheDoTuan => CheDo == CheDoLich.Tuan;
    public bool LaCheDoThang => CheDo == CheDoLich.Thang;
    public bool LaCheDoKhoang => CheDo == CheDoLich.KhoangNgay;
    public bool KhongPhaiKhoang => CheDo != CheDoLich.KhoangNgay;

    /// <summary>Ví dụ "Tuần 09/11 – 15/11/2026", "Tháng 11/2026". Chế độ Khoảng ngày dùng 2 DatePicker nên để trống.</summary>
    [ObservableProperty]
    private string _nhanKy = string.Empty;

    /// <summary>Giá trị 2 DatePicker của chế độ Khoảng ngày.</summary>
    [ObservableProperty]
    private DateTime? _tuNgayChon;

    [ObservableProperty]
    private DateTime? _denNgayChon;

    private (DateOnly Tu, DateOnly Den) TinhKy() => CheDo switch
    {
        CheDoLich.Tuan => (QuyTacLichLamViec.NgayDauTuan(_moc), QuyTacLichLamViec.NgayDauTuan(_moc).AddDays(6)),
        CheDoLich.Thang => (new DateOnly(_moc.Year, _moc.Month, 1),
            new DateOnly(_moc.Year, _moc.Month, DateTime.DaysInMonth(_moc.Year, _moc.Month))),
        _ => (_tuKhoang, _denKhoang),
    };

    private string TinhNhan() => CheDo switch
    {
        CheDoLich.Tuan => QuyTacLichLamViec.NhanTuan(_tu),
        CheDoLich.Thang => $"Tháng {_tu.Month:00}/{_tu.Year}",
        _ => string.Empty,
    };

    /// <summary>Đổi kỳ (hoặc chế độ): bỏ chọn, xoá thông báo cũ rồi tải lại.</summary>
    private void DoiKy()
    {
        (_tu, _den) = TinhKy();
        NhanKy = TinhNhan();
        ThongBao = string.Empty;
        BoChonTatCa();
        TatGoiY();
        _ = TaiLichAsync();
    }

    [RelayCommand]
    private void ChonCheDoTuan() => DatCheDo(CheDoLich.Tuan);

    [RelayCommand]
    private void ChonCheDoThang() => DatCheDo(CheDoLich.Thang);

    [RelayCommand]
    private void ChonCheDoKhoang() => DatCheDo(CheDoLich.KhoangNgay);

    private void DatCheDo(CheDoLich cheDo)
    {
        if (CheDo == cheDo) return;
        CheDo = cheDo;
        DoiKy();
    }

    [RelayCommand]
    private void KyTruoc() => DichKy(-1);

    [RelayCommand]
    private void KySau() => DichKy(1);

    private void DichKy(int huong)
    {
        switch (CheDo)
        {
            case CheDoLich.Tuan:
                _moc = _moc.AddDays(7 * huong);
                break;
            case CheDoLich.Thang:
                _moc = new DateOnly(_moc.Year, _moc.Month, 1).AddMonths(huong);
                break;
            default:
                var soNgay = _denKhoang.DayNumber - _tuKhoang.DayNumber + 1;
                DatKhoang(_tuKhoang.AddDays(soNgay * huong), _denKhoang.AddDays(soNgay * huong));
                break;
        }

        DoiKy();
    }

    [RelayCommand]
    private void HomNay()
    {
        var homNay = QuyTacLichLamViec.HomNay;
        _moc = homNay;
        if (CheDo == CheDoLich.KhoangNgay)
        {
            var soNgay = _denKhoang.DayNumber - _tuKhoang.DayNumber + 1;
            DatKhoang(homNay, homNay.AddDays(soNgay - 1));
        }

        DoiKy();
    }

    /// <summary>Đặt khoảng ngày và cập nhật 2 DatePicker (không kích hoạt lại các handler).</summary>
    private void DatKhoang(DateOnly tu, DateOnly den)
    {
        _tuKhoang = tu;
        _denKhoang = den;
        DatNgayChon(tu, den);
    }

    private void DatNgayChon(DateOnly tu, DateOnly den)
    {
        void Dat()
        {
            _dangDatNgay = true;
            try
            {
                TuNgayChon = tu.ToDateTime(TimeOnly.MinValue);
                DenNgayChon = den.ToDateTime(TimeOnly.MinValue);
            }
            finally
            {
                _dangDatNgay = false;
            }
        }

        Dat();

        // DatePicker không luôn cập nhật khi giá trị nguồn bị đặt lại ngay trong lúc nó đang ghi: đặt lại một lần nữa sau đó.
        var dispatcher = Application.Current?.Dispatcher;
        dispatcher?.BeginInvoke(DispatcherPriority.Background, Dat);
    }

    partial void OnTuNgayChonChanged(DateTime? value)
    {
        if (_dangDatNgay || value is null) return;

        var tu = DateOnly.FromDateTime(value.Value);
        var den = _denKhoang;
        string? canhBao = null;
        if (den < tu)
        {
            den = tu;
            canhBao = "Đến ngày đang trước Từ ngày, đã đặt Đến ngày bằng Từ ngày.";
        }

        var toiDa = tu.AddDays(QuyTacLichLamViec.SoNgayToiDa - 1);
        if (den > toiDa)
        {
            den = toiDa;
            canhBao = $"Khoảng ngày tối đa {QuyTacLichLamViec.SoNgayToiDa} ngày, đã tự lùi Đến ngày.";
        }

        DatKhoang(tu, den);
        if (CheDo != CheDoLich.KhoangNgay) return;

        DoiKy();
        if (canhBao is not null) HienThongBao(canhBao, LoaiThongBao.CanhBao);
    }

    partial void OnDenNgayChonChanged(DateTime? value)
    {
        if (_dangDatNgay || value is null) return;

        var den = DateOnly.FromDateTime(value.Value);
        string? loi = null;
        if (den < _tuKhoang)
            loi = "Đến ngày phải sau hoặc bằng Từ ngày.";
        else if (den.DayNumber - _tuKhoang.DayNumber + 1 > QuyTacLichLamViec.SoNgayToiDa)
            loi = $"Khoảng ngày tối đa {QuyTacLichLamViec.SoNgayToiDa} ngày.";

        if (loi is not null)
        {
            DatNgayChon(_tuKhoang, _denKhoang); // không đổi
            HienThongBao(loi, LoaiThongBao.Loi);
            return;
        }

        _denKhoang = den;
        if (CheDo == CheDoLich.KhoangNgay) DoiKy();
    }

    // =====================================================================
    // Tải dữ liệu và dựng lưới
    // =====================================================================

    [ObservableProperty]
    private IReadOnlyList<CotNgayViewModel> _cacCotNgay = Array.Empty<CotNgayViewModel>();

    [ObservableProperty]
    private IReadOnlyList<CotThieuViewModel> _cacCotThieu = Array.Empty<CotThieuViewModel>();

    [ObservableProperty]
    private IReadOnlyList<HangLichViewModel> _cacHang = Array.Empty<HangLichViewModel>();

    [ObservableProperty]
    private IReadOnlyList<LuaChonCa> _cacCaChon = Array.Empty<LuaChonCa>();

    public IReadOnlyList<LuaChonViTri> CacViTriChon { get; } = new[]
    {
        new LuaChonViTri(null, "Theo vị trí thường làm"),
        new LuaChonViTri(ViTri.LeTan, ViTri.LeTan),
        new LuaChonViTri(ViTri.Groomer, ViTri.Groomer),
    };

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PhanCongCommand))]
    private LuaChonCa? _caDangChon;

    [ObservableProperty]
    private LuaChonViTri _viTriDangChon;

    /// <summary>Gợi ý dưới lưới, đổi khi đang gợi ý người rảnh cho một ca thiếu.</summary>
    [ObservableProperty]
    private string _huongDan = HuongDanMacDinh;

    private const string HuongDanMacDinh =
        "Mỗi lần chọn ô của một nhân viên; bấm sang người khác sẽ bỏ chọn người trước. Bấm tên để chọn mọi ngày trống của họ.";

    public async Task TaiLichAsync()
    {
        var lan = ++_soLanTai;
        var tu = _tu;
        var den = _den;

        KetQua<LichKhoangDto> kq;
        try
        {
            kq = await _nhanVien.LayLichKhoangAsync(tu, den);
        }
        catch (Exception)
        {
            kq = KetQua<LichKhoangDto>.Loi("Không kết nối được cơ sở dữ liệu.");
        }

        if (lan != _soLanTai) return; // người dùng đã đổi kỳ trong lúc tải

        if (!kq.ThanhCong || kq.DuLieu is null)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return;
        }

        _cacCa = kq.DuLieu.CacCa;
        _cacDong = kq.DuLieu.CacDong;
        _nhanVienDangLam = kq.DuLieu.NhanVienDangLam;
        DaTai = true;
        XayLuoi();
    }

    private static string KyHieuTho(string tenCa)
    {
        var ten = tenCa.Trim();
        if (ten.StartsWith("Ca ", StringComparison.OrdinalIgnoreCase)) ten = ten[3..].Trim();
        if (ten.Length == 0) ten = tenCa.Trim();
        return ten.Length == 0 ? "?" : char.ToUpper(ten[0], CultureInfo.CurrentCulture).ToString();
    }

    /// <summary>Ký hiệu ổn định cho từng ca: chữ cái đầu của tên ca (bỏ "Ca "); trùng nhau thì dùng số thứ tự.</summary>
    private Dictionary<int, string> TinhKyHieu()
    {
        var thuTu = _cacCa.OrderBy(x => x.GioBatDau).ThenBy(x => x.MaCa).ToList();
        var tho = thuTu.ToDictionary(x => x.MaCa, x => KyHieuTho(x.TenCa));
        var trung = tho.Values.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        var kq = new Dictionary<int, string>();
        for (var i = 0; i < thuTu.Count; i++)
            kq[thuTu[i].MaCa] = trung.Contains(tho[thuTu[i].MaCa]) ? (i + 1).ToString() : tho[thuTu[i].MaCa];
        return kq;
    }

    private static string HienGio(int phut) =>
        Math.Round(phut / 60.0, 2).ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');

    private void XayLuoi()
    {
        BoChonTatCa();
        TatGoiY();

        var soNgay = _den.DayNumber - _tu.DayNumber + 1;
        var rong = soNgay <= SoNgayRong;
        var cacNgay = Enumerable.Range(0, soNgay).Select(i => _tu.AddDays(i)).ToList();
        var kyHieu = TinhKyHieu();
        var caTheoMa = _cacCa.ToDictionary(x => x.MaCa);
        var caDangDung = _cacCa.Where(x => x.DangSuDung).OrderBy(x => x.GioBatDau).ThenBy(x => x.MaCa).ToList();

        CacCotNgay = cacNgay.Select(n => new CotNgayViewModel(n, rong)).ToList();

        // Hàng "Ca còn thiếu": mỗi ngày chưa qua, mỗi ca đang sử dụng một chip.
        var dem = _cacDong
            .GroupBy(x => (x.Ngay, x.MaCa, x.ViTri))
            .ToDictionary(g => g.Key, g => g.Count());

        var cotThieu = new List<CotThieuViewModel>();
        foreach (var ngay in cacNgay)
        {
            var chip = new List<ChipCaViewModel>();
            if (!QuyTacLichLamViec.DaQua(ngay))
            {
                foreach (var ca in caDangDung)
                {
                    var lt = QuyTacLichLamViec.DinhMuc(ca, ViTri.LeTan) - dem.GetValueOrDefault((ngay, ca.MaCa, ViTri.LeTan));
                    var gr = QuyTacLichLamViec.DinhMuc(ca, ViTri.Groomer) - dem.GetValueOrDefault((ngay, ca.MaCa, ViTri.Groomer));
                    var thua = lt < 0 || gr < 0;
                    var thieuLt = Math.Max(0, lt);
                    var thieuGr = Math.Max(0, gr);
                    var kh = kyHieu[ca.MaCa];

                    var phan = new List<string>();
                    if (thieuLt > 0) phan.Add($"{thieuLt} LT");
                    if (thieuGr > 0) phan.Add($"{thieuGr} GR");

                    var loai = phan.Count > 0 ? LoaiChipCa.Thieu : thua ? LoaiChipCa.Thua : LoaiChipCa.Du;
                    var text = rong
                        ? loai switch
                        {
                            LoaiChipCa.Thieu => $"{kh}: thiếu {string.Join(", ", phan)}",
                            LoaiChipCa.Thua => $"{kh}: thừa người",
                            _ => $"{kh}: đủ",
                        }
                        : loai switch
                        {
                            LoaiChipCa.Thieu => $"{kh}−{thieuLt + thieuGr}",
                            LoaiChipCa.Thua => $"{kh}!",
                            _ => $"{kh}✓",
                        };
                    var goiY = $"{ca.TenCa} {QuyTacLichLamViec.NgayNgan(ngay)}: " + loai switch
                    {
                        LoaiChipCa.Thieu => $"thiếu {string.Join(", ", phan)}. Bấm để gợi ý người rảnh.",
                        LoaiChipCa.Thua => "thừa người",
                        _ => "đủ người",
                    };
                    var viTriThieu = thieuLt > 0 ? ViTri.LeTan : thieuGr > 0 ? ViTri.Groomer : null;

                    chip.Add(new ChipCaViewModel(ngay, ca.MaCa, text, goiY, loai, viTriThieu, rong, KhiBamChip));
                }
            }

            cotThieu.Add(new CotThieuViewModel(ngay, rong, chip));
        }

        CacCotThieu = cotThieu;

        // Các hàng nhân viên, chia nhóm theo vị trí thường làm.
        var dongTheoKhoa = _cacDong.ToDictionary(x => (x.MaNv, x.Ngay));
        var cacHang = new List<HangLichViewModel>();
        foreach (var (viTri, tieuDe) in new[]
                 {
                     (ViTri.LeTan, "Thường làm Lễ tân"),
                     (ViTri.Groomer, "Thường làm Groomer"),
                 })
        {
            var nhom = _nhanVienDangLam.Where(x => x.ViTriThuong == viTri).ToList();
            if (nhom.Count == 0) continue;

            cacHang.Add(new TieuDeNhomLichViewModel(tieuDe));
            foreach (var nv in nhom)
            {
                var cacO = new List<OLichViewModel>();
                var soCa = 0;
                var phut = 0;
                foreach (var ngay in cacNgay)
                {
                    dongTheoKhoa.TryGetValue((nv.MaNv, ngay), out var dong);
                    CaLamViecDto? ca = null;
                    if (dong is not null)
                    {
                        caTheoMa.TryGetValue(dong.MaCa, out ca);
                        soCa++;
                        phut += ca?.ThoiLuongPhut ?? 0;
                    }

                    cacO.Add(new OLichViewModel(nv.MaNv, nv.HoTen, ngay, dong, ca,
                        dong is not null && kyHieu.TryGetValue(dong.MaCa, out var k) ? k : "?", rong, KhiBamO));
                }

                var phuDe = (nv.VaiTro == VaiTro.QuanLy ? "Quản lý · " : string.Empty) + $"{soCa} ca · {HienGio(phut)} giờ";
                cacHang.Add(new HangNhanVienViewModel(nv.MaNv, nv.HoTen, phuDe, cacO, KhiBamTen));
            }
        }

        CacHang = cacHang;

        // Giữ lựa chọn Ca nếu còn, không thì ca đầu tiên.
        CacCaChon = caDangDung
            .Select(x => new LuaChonCa(x.MaCa, x.TenCa, $"{x.TenCa} ({x.GioBatDau:hh\\:mm}–{x.GioKetThuc:hh\\:mm})"))
            .ToList();
        CaDangChon = CacCaChon.FirstOrDefault(x => x.MaCa == CaDangChon?.MaCa) ?? CacCaChon.FirstOrDefault();
    }

    // =====================================================================
    // Gợi ý người rảnh cho ca thiếu
    // =====================================================================

    private void KhiBamChip(ChipCaViewModel chip)
    {
        var dangBat = chip.DangChon;
        TatGoiY();
        if (dangBat) return;

        chip.DangChon = true;
        _goiY = (chip.Ngay, chip.MaCa);
        CaDangChon = CacCaChon.FirstOrDefault(x => x.MaCa == chip.MaCa) ?? CaDangChon;
        ViTriDangChon = CacViTriChon.First(x => x.ViTri == chip.ViTriThieu);
        ThongBao = string.Empty;

        var ten = _cacCa.FirstOrDefault(x => x.MaCa == chip.MaCa)?.TenCa;
        HuongDan = $"Đang gợi ý người rảnh cho {ten} {QuyTacLichLamViec.NgayNgan(chip.Ngay)}. Bấm ô viền chấm để chọn.";
        foreach (var o in TatCaO().Where(o => o.Ngay == chip.Ngay && o.Trong && !o.DaQua))
            o.GoiY = true;
    }

    private void TatGoiY()
    {
        if (_goiY is null && HuongDan == HuongDanMacDinh) return;

        _goiY = null;
        HuongDan = HuongDanMacDinh;
        foreach (var cot in CacCotThieu)
            foreach (var chip in cot.CacChip)
                chip.DangChon = false;
        foreach (var o in TatCaO())
            o.GoiY = false;
    }

    private IEnumerable<OLichViewModel> TatCaO() =>
        CacHang.OfType<HangNhanVienViewModel>().SelectMany(h => h.CacO);

    // =====================================================================
    // Chọn ô (chỉ một nhân viên mỗi lần)
    // =====================================================================

    private void KhiBamO(OLichViewModel o) => DoiChon(new[] { o });

    /// <summary>Bấm tên: chọn mọi ô trống chưa qua của người đó trong kỳ; bấm lại thì bỏ chọn.</summary>
    private void KhiBamTen(HangNhanVienViewModel hang) =>
        DoiChon(hang.CacO.Where(o => o.Trong && !o.DaQua).ToList());

    private void DoiChon(IReadOnlyList<OLichViewModel> cacO)
    {
        if (cacO.Count == 0) return;
        if (cacO.Any(o => o.DaQua)) return;

        // Bấm ô hoặc tên của người khác thì bỏ chọn người trước.
        if (_dangChon.Count > 0 && _dangChon[0].MaNv != cacO[0].MaNv)
            BoChonTatCa();

        var tatCaDaChon = cacO.All(o => o.DaChon);
        foreach (var o in cacO)
        {
            if (tatCaDaChon)
            {
                o.DaChon = false;
                _dangChon.Remove(o);
            }
            else if (!o.DaChon)
            {
                o.DaChon = true;
                _dangChon.Add(o);
            }
        }

        ThongBao = string.Empty;
        CapNhatThanhPhanCong();
    }

    [RelayCommand]
    private void BoChon()
    {
        BoChonTatCa();
    }

    private void BoChonTatCa()
    {
        foreach (var o in _dangChon) o.DaChon = false;
        _dangChon.Clear();
        CapNhatThanhPhanCong();
    }

    // =====================================================================
    // Thanh phân công
    // =====================================================================

    public bool CoChon => _dangChon.Count > 0;

    public string TieuDeChon => _dangChon.Count == 0
        ? string.Empty
        : $"Đã chọn {_dangChon.Count} ô của {_dangChon[0].HoTen}";

    public string PhuDeChon
    {
        get
        {
            var coCa = _dangChon.Count(o => o.CoCa);
            var trong = _dangChon.Count - coCa;
            var phan = new List<string>();
            if (trong > 0) phan.Add($"{trong} ô trống");
            if (coCa > 0) phan.Add($"{coCa} ô đã có ca (sẽ đổi)");
            return string.Join(" · ", phan);
        }
    }

    public string NhanPhanCong => $"Phân công {_dangChon.Count} ô";

    private void CapNhatThanhPhanCong()
    {
        OnPropertyChanged(nameof(CoChon));
        OnPropertyChanged(nameof(TieuDeChon));
        OnPropertyChanged(nameof(PhuDeChon));
        OnPropertyChanged(nameof(NhanPhanCong));
        PhanCongCommand.NotifyCanExecuteChanged();
        GoCaCommand.NotifyCanExecuteChanged();
    }

    // =====================================================================
    // Phân công (định mức cứng, kiểm tra ở Service)
    // =====================================================================

    private bool CoTheDatPhanCong => !DangXuLy && _dangChon.Count > 0 && CaDangChon is not null;

    [RelayCommand(CanExecute = nameof(CoTheDatPhanCong))]
    private async Task PhanCongAsync()
    {
        if (!CoTheDatPhanCong) return;

        var maNv = _dangChon[0].MaNv;
        var nv = _nhanVienDangLam.FirstOrDefault(x => x.MaNv == maNv);
        if (nv is null || CaDangChon is null) return;

        var ca = CaDangChon;
        var viTri = ViTriDangChon.ViTri ?? nv.ViTriThuong;
        var cacNgay = _dangChon.Select(o => o.Ngay).Distinct().OrderBy(x => x).ToList();

        DangXuLy = true;
        try
        {
            var kt = await _nhanVien.KiemTraPhanCongAsync(maNv, ca.MaCa, viTri, cacNgay);
            if (!kt.ThanhCong || kt.DuLieu is null)
            {
                HienThongBao(kt.ThongBao, LoaiThongBao.Loi);
                return;
            }

            var thayDoi = new List<ThayDoiLich>(kt.DuLieu.HopLe);
            var soDoi = 0;
            if (kt.DuLieu.VuongDinhMuc.Count > 0)
            {
                // Vướng định mức: không lưu gì cho tới khi Quản lý xác nhận.
                var hop = new XungDotDinhMucViewModel(kt.DuLieu.VuongDinhMuc, kt.DuLieu.HoTenNhanVien, kt.DuLieu.HopLe.Count);
                if (_hopThoai.MoHopThoai(hop) != true) return;

                var doi = hop.LayCacThayDoiDoi();
                soDoi = doi.Count / 2;
                thayDoi.AddRange(doi);
            }

            if (thayDoi.Count == 0)
            {
                BoChonTatCa();
                HienThongBao("Không có ô nào được phân công: các ô đã chọn đã đúng ca này, đã đủ người hoặc không đổi gì.",
                    LoaiThongBao.CanhBao);
                return;
            }

            var soO = kt.DuLieu.HopLe.Count;
            var phan = new List<string>();
            if (soO > 0) phan.Add($"{soO} ô");
            if (soDoi > 0) phan.Add($"đổi người {soDoi} ô");
            var chuPhan = string.Join(", ", phan);

            await ApDungVaTaiLaiAsync(thayDoi, $"Phân công {ca.TenCa} ({chuPhan})",
                $"Đã phân công {ca.TenCa}: {chuPhan}.", LoaiThongBao.ThanhCong);
        }
        finally
        {
            DangXuLy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CoTheGoCa))]
    private async Task GoCaAsync()
    {
        if (!CoTheGoCa) return;

        var thayDoi = _dangChon
            .Where(o => o.Dong is not null)
            .Select(o => ThayDoiLich.Xoa(o.MaNv, o.Ngay, o.Dong!.MaCa, o.Dong.ViTri))
            .ToList();

        DangXuLy = true;
        try
        {
            await ApDungVaTaiLaiAsync(thayDoi, $"Gỡ {thayDoi.Count} ca",
                $"Đã gỡ {thayDoi.Count} ca khỏi lịch.", LoaiThongBao.ThanhCong);
        }
        finally
        {
            DangXuLy = false;
        }
    }

    private bool CoTheGoCa => !DangXuLy && _dangChon.Any(o => o.CoCa);

    /// <summary>Lưu một lô trong một giao dịch, đưa vào ngăn xếp Hoàn tác rồi tải lại lưới.</summary>
    private async Task<bool> ApDungVaTaiLaiAsync(IReadOnlyList<ThayDoiLich> thayDoi, string nhan, string thongBaoOk, LoaiThongBao loai)
    {
        var kq = await _nhanVien.ApDungThayDoiLichAsync(thayDoi);
        if (!kq.ThanhCong || kq.DuLieu is null)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return false;
        }

        _hoanTac.Add(new MucHoanTac(nhan, kq.DuLieu.NghichDao));
        if (_hoanTac.Count > SoHoanTacToiDa) _hoanTac.RemoveRange(0, _hoanTac.Count - SoHoanTacToiDa);
        CapNhatHoanTac();

        await TaiLichAsync();
        HienThongBao(thongBaoOk + " Bấm “Hoàn tác” nếu cần quay lại.", loai);
        return true;
    }

    // =====================================================================
    // Hoàn tác (chỉ trong phiên)
    // =====================================================================

    public bool CoTheHoanTac => _hoanTac.Count > 0;

    public string GoiYHoanTac => _hoanTac.Count > 0
        ? $"Hoàn tác: {_hoanTac[^1].Nhan}"
        : "Chưa có thao tác để hoàn tác";

    private void CapNhatHoanTac()
    {
        OnPropertyChanged(nameof(CoTheHoanTac));
        OnPropertyChanged(nameof(GoiYHoanTac));
        HoanTacCommand.NotifyCanExecuteChanged();
    }

    private bool CoTheDatHoanTac => !DangXuLy && _hoanTac.Count > 0;

    [RelayCommand(CanExecute = nameof(CoTheDatHoanTac))]
    private async Task HoanTacAsync()
    {
        if (!CoTheDatHoanTac) return;

        var muc = _hoanTac[^1];

        // Ngày đã qua thì không hoàn tác được nữa (giữ lịch sử phân công); bỏ mục này để các mục trước vẫn dùng được.
        if (muc.NghichDao.Any(x => QuyTacLichLamViec.DaQua(x.Ngay)))
        {
            _hoanTac.RemoveAt(_hoanTac.Count - 1);
            CapNhatHoanTac();
            HienThongBao($"Không hoàn tác được “{muc.Nhan}” vì có ngày đã qua.", LoaiThongBao.CanhBao);
            return;
        }

        DangXuLy = true;
        try
        {
            var kq = await _nhanVien.ApDungThayDoiLichAsync(muc.NghichDao);
            if (!kq.ThanhCong)
            {
                HienThongBao($"Không hoàn tác được “{muc.Nhan}”: {kq.ThongBao}", LoaiThongBao.Loi);
                return;
            }

            _hoanTac.RemoveAt(_hoanTac.Count - 1);
            CapNhatHoanTac();
            await TaiLichAsync();
            HienThongBao($"Đã hoàn tác: {muc.Nhan}.", LoaiThongBao.ThanhCong);
        }
        finally
        {
            DangXuLy = false;
        }
    }

    // =====================================================================
    // Lặp lịch tuần
    // =====================================================================

    private bool CoTheLapLich => !DangXuLy && DaTai && CheDo == CheDoLich.Tuan;

    [RelayCommand(CanExecute = nameof(CoTheLapLich))]
    private async Task LapLichTuanAsync()
    {
        if (!CoTheLapLich) return;

        var danhSach = _nhanVienDangLam
            .Select(nv => (nv, _cacDong.Count(d => d.MaNv == nv.MaNv)))
            .ToList();
        var hop = new LapLichTuanViewModel(_nhanVien, _tu, danhSach);
        if (_hopThoai.MoHopThoai(hop) != true || hop.CacThayDoi.Count == 0) return;

        var soTuan = hop.SoTuan;
        var soCa = hop.CacThayDoi.Count;
        var boQua = hop.SoOBoQuaDoDay;

        var thongBao = $"Đã lặp lịch tuần sang {soTuan} tuần tiếp theo: thêm {soCa} ca.";
        if (boQua > 0) thongBao += $" Bỏ qua {boQua} ô đã có ca hoặc ca đã đủ.";

        DangXuLy = true;
        try
        {
            await ApDungVaTaiLaiAsync(hop.CacThayDoi, $"Lặp lịch tuần sang {soTuan} tuần", thongBao,
                boQua > 0 ? LoaiThongBao.CanhBao : LoaiThongBao.ThanhCong);
        }
        finally
        {
            DangXuLy = false;
        }
    }
}
