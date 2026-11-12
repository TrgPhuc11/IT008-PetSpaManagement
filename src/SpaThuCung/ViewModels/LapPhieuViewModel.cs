using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Models;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Một lựa chọn Groomer trong form ("Chưa phân công" có Groomer = null).</summary>
public sealed record LuaChonGroomer(GroomerCaDto? Groomer, string HienThi);

/// <summary>Một mốc giờ hẹn chọn được, ví dụ "09:15".</summary>
public sealed record MocGio(TimeSpan Gio)
{
    public string HienThi => QuyTacPhieu.Gio(Gio);
}

/// <summary>
/// Màn Lập phiếu (UC10, FR07, mọi nhân viên): tìm hoặc thêm khách, chọn hoặc thêm thú cưng, cảnh báo khách bị cờ (BR05),
/// chọn dịch vụ với giá và thời lượng tra theo loài, cân nặng (BR08); Đặt lịch chọn ngày giờ theo ca và Groomer (UC12, UC13),
/// Walk-in gán lồng Walk-in trống và Groomer của ca hiện tại (BR13). Cảnh báo quá tải (BR11) chỉ hỏi lại, không chặn.
/// </summary>
public partial class LapPhieuViewModel : ObservableObject, ICoTaiDuLieu
{
    private const int SoKhachHienToiDa = 50;
    private const string TieuDeHoi = "Lập phiếu";

    private readonly IKhachHangService _khachHang;
    private readonly IDichVuService _dichVu;
    private readonly IPhieuService _phieu;
    private readonly ICauHinhService _cauHinh;
    private readonly IHopThoaiService _hopThoai;

    private List<KhachHangDto> _tatCaKhach = new();
    private List<CaLamViecDto> _cacCa = new();
    private CaCuaGioHenDto? _caHienTai;

    // Tăng mỗi lần gửi yêu cầu, để bỏ kết quả của yêu cầu cũ về muộn.
    private int _lanTraGia;
    private int _lanTaiCa;
    private int _lanKiemTraThuCung;

    public LapPhieuViewModel(IKhachHangService khachHang, IDichVuService dichVu, IPhieuService phieu,
        ICauHinhService cauHinh, IHopThoaiService hopThoai)
    {
        _khachHang = khachHang;
        _dichVu = dichVu;
        _phieu = phieu;
        _cauHinh = cauHinh;
        _hopThoai = hopThoai;
        _tcMoiLoai = LoaiThuCung.Cho;
        _ngayHen = DateTime.Today;
    }

    public IReadOnlyList<string> CacLoai { get; } = LoaiThuCung.TatCa;

    // ---------- Hộp thông báo chung ----------

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

    // ---------- 1. Khách hàng ----------

    public ObservableCollection<KhachHangDto> KetQuaKhach { get; } = new();

    /// <summary>
    /// Dòng đang chọn trong danh sách kết quả tìm. Tách khỏi KhachChon để gõ tìm tiếp (danh sách đổi) không làm mất khách đã chọn.
    /// </summary>
    [ObservableProperty]
    private KhachHangDto? _khachTrongDanhSach;

    partial void OnKhachTrongDanhSachChanged(KhachHangDto? value)
    {
        if (value is not null && value.MaKh != KhachChon?.MaKh) KhachChon = value;
    }

    [ObservableProperty]
    private string _tuKhoaKhach = string.Empty;

    partial void OnTuKhoaKhachChanged(string value) => LocKhach();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoKhach))]
    [NotifyPropertyChangedFor(nameof(CoCanhBaoKhach))]
    [NotifyPropertyChangedFor(nameof(CanhBaoKhach))]
    [NotifyPropertyChangedFor(nameof(ThongTinKhach))]
    [NotifyCanExecuteChangedFor(nameof(MoThemThuCungCommand))]
    private KhachHangDto? _khachChon;

    public bool CoKhach => KhachChon is not null;

    /// <summary>BR05: khách có số lần Không đến từ ngưỡng cảnh báo trở lên. Chỉ cảnh báo, không chặn.</summary>
    public bool CoCanhBaoKhach => KhachChon?.CoCanhBao == true;

    public string CanhBaoKhach => KhachChon is { CoCanhBao: true } k
        ? $"Lưu ý: {k.HoTen} đã không đến {k.SoLanKhongDen} lần. Nên gọi xác nhận trước giờ hẹn; vẫn lập phiếu được."
        : string.Empty;

    public string ThongTinKhach => KhachChon is null
        ? "Chưa chọn khách"
        : $"{KhachChon.HoTen} · {KhachChon.Sdt}"
          + (KhachChon.SoLanKhongDen > 0 ? $" · không đến {KhachChon.SoLanKhongDen} lần" : string.Empty);

    partial void OnKhachChonChanged(KhachHangDto? value)
    {
        DangThemThuCung = false;
        ThuCungChon = null;
        CacThuCung.Clear();
        if (value is not null) _ = TaiThuCungAsync(value.MaKh, null);
    }

    private void LocKhach()
    {
        var tuKhoa = TimKiemChu.BoDau(TuKhoaKhach.Trim());
        KetQuaKhach.Clear();
        foreach (var k in _tatCaKhach.Where(x => TimKiemChu.Khop(tuKhoa, x.HoTen, x.Sdt, x.MaHienThi)).Take(SoKhachHienToiDa))
            KetQuaKhach.Add(k);
    }

    // Thêm khách nhanh (UC10 bước 1: chưa có thì tạo hồ sơ mới).

    [ObservableProperty]
    private bool _dangThemKhach;

    [ObservableProperty]
    private string _khachMoiHoTen = string.Empty;

    [ObservableProperty]
    private string _khachMoiSdt = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoLoiKhachMoi))]
    private string _loiKhachMoi = string.Empty;

    public bool CoLoiKhachMoi => !string.IsNullOrEmpty(LoiKhachMoi);

    [RelayCommand]
    private void MoThemKhach()
    {
        // Ô tìm đang là số thì điền sẵn vào SĐT, là chữ thì điền vào họ tên.
        var tk = TuKhoaKhach.Trim();
        var laSo = tk.Length > 0 && tk.All(c => char.IsDigit(c) || c == ' ');
        KhachMoiSdt = laSo ? tk.Replace(" ", string.Empty) : string.Empty;
        KhachMoiHoTen = laSo ? string.Empty : tk;
        LoiKhachMoi = string.Empty;
        DangThemKhach = true;
    }

    [RelayCommand]
    private void HuyThemKhach() => DangThemKhach = false;

    [RelayCommand]
    private async Task LuuKhachMoiAsync()
    {
        var nhap = new KhachHangNhap(KhachMoiHoTen, KhachMoiSdt, null);
        var loi = QuyTacKhachHang.KiemTraHoTen(nhap.HoTen) ?? QuyTacKhachHang.KiemTraSdt(nhap.Sdt);
        if (loi is not null)
        {
            LoiKhachMoi = loi;
            return;
        }

        var kq = await _khachHang.ThemKhachAsync(nhap);
        if (!kq.ThanhCong)
        {
            LoiKhachMoi = kq.ThongBao;
            return;
        }

        DangThemKhach = false;
        await TaiKhachAsync();
        KhachChon = _tatCaKhach.FirstOrDefault(x => x.MaKh == kq.DuLieu!.MaKh);
        TuKhoaKhach = KhachChon?.Sdt ?? string.Empty;
        KhachTrongDanhSach = KhachChon;
        HienThongBao(kq.ThongBao, kq.DuLieu!.DaCoHoSo ? LoaiThongBao.CanhBao : LoaiThongBao.ThanhCong);
    }

    // ---------- 2. Thú cưng ----------

    public ObservableCollection<ThuCungDto> CacThuCung { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoThuCung))]
    private ThuCungDto? _thuCungChon;

    public bool CoThuCung => ThuCungChon is not null;

    /// <summary>Lý do không lập được phiếu cho thú cưng đang chọn (chưa tiêm phòng, đang có phiếu); rỗng nếu được.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoLoiThuCung))]
    private string _loiThuCung = string.Empty;

    public bool CoLoiThuCung => !string.IsNullOrEmpty(LoiThuCung);

    /// <summary>Cân nặng lúc lập phiếu (mặc định lấy từ hồ sơ, sửa được nếu vừa cân lại).</summary>
    [ObservableProperty]
    private string _canNang = string.Empty;

    partial void OnThuCungChonChanged(ThuCungDto? value)
    {
        LoiThuCung = string.Empty;
        CacDichVu.Clear();
        TinhTong();
        if (value is null)
        {
            CanNang = string.Empty;
            return;
        }

        if (!value.DaTiemPhong)
            LoiThuCung = $"{value.Ten} chưa được xác nhận tiêm phòng nên chưa lập phiếu được (BR06). "
                         + "Xác nhận ở trang Thú cưng hoặc bấm Sửa hồ sơ.";
        else
            _ = KiemTraPhieuDangCoAsync(value.MaThuCung);

        // Đổi CanNang sẽ tra giá; cùng giá trị thì tra trực tiếp.
        var kg = value.CanNangHienThi;
        if (CanNang == kg) _ = TraGiaAsync();
        else CanNang = kg;
    }

    partial void OnCanNangChanged(string value) => _ = TraGiaAsync();

    private async Task TaiThuCungAsync(int maKh, int? chonMa)
    {
        var kq = await _khachHang.LayDanhSachThuCungAsync(maKh);
        if (KhachChon?.MaKh != maKh) return;
        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return;
        }

        CacThuCung.Clear();
        foreach (var tc in kq.DuLieu!) CacThuCung.Add(tc);

        ThuCungChon = chonMa is int ma
            ? CacThuCung.FirstOrDefault(x => x.MaThuCung == ma)
            : CacThuCung.Count == 1 ? CacThuCung[0] : null;
    }

    private async Task KiemTraPhieuDangCoAsync(int maThuCung)
    {
        var lan = ++_lanKiemTraThuCung;
        var kq = await _phieu.TimPhieuDangHoatDongAsync(maThuCung);
        var tc = ThuCungChon;
        if (lan != _lanKiemTraThuCung || tc is null || tc.MaThuCung != maThuCung) return;

        if (!kq.ThanhCong) LoiThuCung = kq.ThongBao;
        else if (kq.DuLieu is not null)
            LoiThuCung = $"{tc.Ten} {kq.DuLieu}, không lập thêm phiếu. Mở phiếu đó để sửa hoặc nhận thú cưng.";
    }

    // Thêm thú cưng nhanh (UC10 bước 2).

    [ObservableProperty]
    private bool _dangThemThuCung;

    [ObservableProperty]
    private string _tcMoiTen = string.Empty;

    [ObservableProperty]
    private string _tcMoiLoai;

    [ObservableProperty]
    private string _tcMoiCanNang = string.Empty;

    [ObservableProperty]
    private bool _tcMoiDaTiemPhong;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoLoiThuCungMoi))]
    private string _loiThuCungMoi = string.Empty;

    public bool CoLoiThuCungMoi => !string.IsNullOrEmpty(LoiThuCungMoi);

    [RelayCommand(CanExecute = nameof(CoKhach))]
    private void MoThemThuCung()
    {
        TcMoiTen = string.Empty;
        TcMoiLoai = LoaiThuCung.Cho;
        TcMoiCanNang = string.Empty;
        TcMoiDaTiemPhong = false;
        LoiThuCungMoi = string.Empty;
        DangThemThuCung = true;
    }

    [RelayCommand]
    private void HuyThemThuCung() => DangThemThuCung = false;

    [RelayCommand]
    private async Task LuuThuCungMoiAsync()
    {
        var khach = KhachChon;
        if (khach is null) return;

        if (!DongGiaViewModel.DocKg(TcMoiCanNang, out var kg))
        {
            LoiThuCungMoi = "Cân nặng phải là số kg, ví dụ 4,5.";
            return;
        }

        var nhap = new ThuCungNhap(khach.MaKh, TcMoiTen, TcMoiLoai, kg, TcMoiDaTiemPhong, null);
        var loi = QuyTacKhachHang.KiemTraTenThuCung(nhap.Ten)
                  ?? QuyTacKhachHang.KiemTraLoai(nhap.Loai)
                  ?? QuyTacKhachHang.KiemTraCanNang(nhap.CanNang);
        if (loi is not null)
        {
            LoiThuCungMoi = loi;
            return;
        }

        var kq = await _khachHang.ThemThuCungAsync(nhap);
        if (!kq.ThanhCong)
        {
            LoiThuCungMoi = kq.ThongBao;
            return;
        }

        DangThemThuCung = false;
        HienThongBao(kq.ThongBao, LoaiThongBao.ThanhCong);
        await TaiThuCungAsync(khach.MaKh, kq.DuLieu);
    }

    // ---------- 3. Dịch vụ ----------

    public ObservableCollection<DichVuPhieuViewModel> CacDichVu { get; } = new();

    [ObservableProperty]
    private string _ghiChuDichVu = "Chọn thú cưng để xem dịch vụ và giá.";

    [ObservableProperty]
    private string _tongTienHienThi = "0";

    [ObservableProperty]
    private string _tongThoiLuongHienThi = "0 phút";

    [ObservableProperty]
    private string _gioXongDuKien = string.Empty;

    /// <summary>Tổng thời lượng các dịch vụ đã chọn (dòng nhập tay chưa hợp lệ tính 0).</summary>
    private int _tongPhut;

    private async Task TraGiaAsync()
    {
        var tc = ThuCungChon;
        var lan = ++_lanTraGia;
        if (tc is null) return;

        if (!DongGiaViewModel.DocKg(CanNang, out var kg) || QuyTacKhachHang.KiemTraCanNang(kg) is not null)
        {
            GhiChuDichVu = "Cân nặng phải là số kg lớn hơn 0, ví dụ 4,5.";
            return;
        }

        var kq = await _dichVu.TraGiaAsync(tc.Loai, kg);
        if (lan != _lanTraGia || ThuCungChon?.MaThuCung != tc.MaThuCung) return;
        if (!kq.ThanhCong)
        {
            GhiChuDichVu = kq.ThongBao;
            return;
        }

        // Giữ lựa chọn và ô nhập tay của các dịch vụ khi tra lại theo cân nặng mới.
        var cu = CacDichVu.ToDictionary(x => x.MaDv);
        CacDichVu.Clear();
        foreach (var g in kq.DuLieu!)
        {
            var dong = new DichVuPhieuViewModel(g, TinhTong);
            if (cu.TryGetValue(g.MaDv, out var dongCu)) dong.ChepTu(dongCu);
            CacDichVu.Add(dong);
        }

        GhiChuDichVu = CacDichVu.Count == 0
            ? "Chưa có dịch vụ nào đang cung cấp."
            : $"Giá theo {tc.Loai} {QuyTacDichVu.KgHienThi(kg)} kg. Không có khoảng cân phù hợp thì nhập tay và ghi lý do.";
        TinhTong();
    }

    private void TinhTong()
    {
        var chon = CacDichVu.Where(x => x.Chon).ToList();
        TongTienHienThi = QuyTacDichVu.TienHienThi(chon.Sum(x => x.DonGia ?? 0));
        _tongPhut = chon.Sum(x => x.ThoiLuong ?? 0);
        TongThoiLuongHienThi = $"{_tongPhut} phút";
        CapNhatCanhBao();
    }

    // ---------- 4. Tiếp nhận ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LaWalkIn))]
    private bool _laDatLich = true;

    public bool LaWalkIn
    {
        get => !LaDatLich;
        set => LaDatLich = !value;
    }

    partial void OnLaDatLichChanged(bool value) => _ = TaiCaVaLongAsync();

    [ObservableProperty]
    private DateTime? _ngayHen;

    partial void OnNgayHenChanged(DateTime? value) => _ = TaiCaVaLongAsync();

    public ObservableCollection<MocGio> CacMocGio { get; } = new();

    [ObservableProperty]
    private MocGio? _gioHenChon;

    partial void OnGioHenChonChanged(MocGio? value) => _ = TaiCaVaLongAsync();

    public ObservableCollection<LuaChonGroomer> CacGroomer { get; } = new();

    [ObservableProperty]
    private LuaChonGroomer? _groomerChon;

    partial void OnGroomerChonChanged(LuaChonGroomer? value) => CapNhatCanhBao();

    public ObservableCollection<LongTrongDto> CacLongTrong { get; } = new();

    [ObservableProperty]
    private LongTrongDto? _longChon;

    /// <summary>Ví dụ "Ca sáng 08:00–14:30 · 2 Groomer có lịch" hoặc lý do không xếp được.</summary>
    [ObservableProperty]
    private string _thongTinCa = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoLoiCa))]
    private string _loiCa = string.Empty;

    public bool CoLoiCa => !string.IsNullOrEmpty(LoiCa);

    [ObservableProperty]
    private string _thongTinLong = string.Empty;

    /// <summary>Cảnh báo không chặn (BR11), mỗi dòng một cảnh báo.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoCanhBaoQuaTai))]
    private string _canhBaoQuaTai = string.Empty;

    public bool CoCanhBaoQuaTai => !string.IsNullOrEmpty(CanhBaoQuaTai);

    [RelayCommand]
    private Task LamMoiTiepNhanAsync() => TaiCaVaLongAsync();

    /// <summary>Giờ hẹn theo lựa chọn hiện tại: Walk-in là bây giờ; Đặt lịch là ngày + mốc giờ đã chọn.</summary>
    private DateTime? GioHenHienTai()
    {
        if (LaWalkIn) return QuyTacPhieu.CatGiay(DateTime.Now);
        if (NgayHen is null || GioHenChon is null) return null;
        return NgayHen.Value.Date + GioHenChon.Gio;
    }

    private async Task TaiCaVaLongAsync()
    {
        var lan = ++_lanTaiCa;
        _caHienTai = null;
        LoiCa = string.Empty;
        ThongTinCa = string.Empty;

        var gioHen = GioHenHienTai();
        var maGroomerCu = GroomerChon?.Groomer?.MaNv;
        var maLongCu = LongChon?.MaLong;

        if (gioHen is null)
        {
            NapGroomer(null, maGroomerCu);
            ThongTinCa = "Chọn ngày và giờ hẹn.";
            return;
        }

        var kqCa = await _phieu.LayCaVaGroomerAsync(gioHen.Value);
        List<LongTrongDto>? longTrong = null;
        string? loiLong = null;
        if (LaWalkIn)
        {
            var kqLong = await _phieu.LayLongTrongAsync(LoaiLong.WalkIn);
            if (kqLong.ThanhCong) longTrong = kqLong.DuLieu;
            else loiLong = kqLong.ThongBao;
        }
        if (lan != _lanTaiCa) return;

        if (!kqCa.ThanhCong)
        {
            LoiCa = kqCa.ThongBao;
            NapGroomer(null, maGroomerCu);
        }
        else
        {
            _caHienTai = kqCa.DuLieu!;
            var ca = _caHienTai.Ca;
            ThongTinCa = $"{ca.TenCa} {QuyTacPhieu.Gio(ca.GioBatDau)}–{QuyTacPhieu.Gio(ca.GioKetThuc)} · "
                         + $"{_caHienTai.Groomer.Count} Groomer có lịch"
                         + (LaWalkIn ? $" · giờ tiếp nhận {QuyTacPhieu.Gio(gioHen.Value)}" : string.Empty);
            if (_caHienTai.Groomer.Count == 0)
                LoiCa = $"{ca.TenCa} ngày {gioHen.Value:dd/MM/yyyy} chưa có Groomer trong lịch làm việc nên chưa nhận phiếu được (BR10).";
            NapGroomer(_caHienTai, maGroomerCu);
        }

        CacLongTrong.Clear();
        if (LaWalkIn)
        {
            foreach (var l in longTrong ?? new List<LongTrongDto>()) CacLongTrong.Add(l);
            LongChon = CacLongTrong.FirstOrDefault(x => x.MaLong == maLongCu) ?? CacLongTrong.FirstOrDefault();
            ThongTinLong = loiLong
                           ?? (CacLongTrong.Count == 0
                               ? "Đã hết lồng Walk-in trống, không nhận được khách Walk-in (BR13)."
                               : $"Còn {CacLongTrong.Count} lồng Walk-in trống.");
        }
        else
        {
            LongChon = null;
            ThongTinLong = string.Empty;
        }

        CapNhatCanhBao();
    }

    private void NapGroomer(CaCuaGioHenDto? ca, int? maGiuLai)
    {
        CacGroomer.Clear();
        // Đặt lịch được để Groomer trống và phân công sau (UC13); Walk-in phải chọn ngay.
        if (LaDatLich) CacGroomer.Add(new LuaChonGroomer(null, "Chưa phân công"));
        foreach (var g in ca?.Groomer ?? new List<GroomerCaDto>())
            CacGroomer.Add(new LuaChonGroomer(g, g.HienThi));

        GroomerChon = CacGroomer.FirstOrDefault(x => x.Groomer?.MaNv == maGiuLai && maGiuLai is not null)
                      ?? (LaWalkIn
                          ? CacGroomer.OrderBy(x => x.Groomer!.PhutDaNhan).FirstOrDefault()
                          : CacGroomer.FirstOrDefault());
    }

    private void CapNhatCanhBao()
    {
        var gioHen = GioHenHienTai();
        if (_caHienTai is null || gioHen is null)
        {
            CanhBaoQuaTai = string.Empty;
            GioXongDuKien = string.Empty;
            return;
        }

        GioXongDuKien = _tongPhut > 0
            ? $"Dự kiến xong lúc {QuyTacPhieu.Gio(gioHen.Value.AddMinutes(_tongPhut))}"
            : string.Empty;
        CanhBaoQuaTai = string.Join(Environment.NewLine,
            QuyTacPhieu.CanhBaoQuaTai(_caHienTai.Ca, gioHen.Value, _tongPhut, GroomerChon?.Groomer));
    }

    // ---------- Tải dữ liệu, lưu ----------

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LuuCommand))]
    private bool _dangLuu;

    public async Task TaiDuLieuAsync()
    {
        var kqCa = await _cauHinh.LayDanhSachCaAsync();
        if (!kqCa.ThanhCong)
        {
            HienThongBao(kqCa.ThongBao, LoaiThongBao.Loi);
            return;
        }
        _cacCa = kqCa.DuLieu!;
        CacMocGio.Clear();
        foreach (var t in QuyTacPhieu.CacMocGio(_cacCa)) CacMocGio.Add(new MocGio(t));

        if (!await TaiKhachAsync()) return;
        ChonMocGioMacDinh();
        await TaiCaVaLongAsync();
    }

    private async Task<bool> TaiKhachAsync()
    {
        var kq = await _khachHang.LayDanhSachKhachAsync();
        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return false;
        }
        _tatCaKhach = kq.DuLieu!;
        LocKhach();
        return true;
    }

    /// <summary>Mốc giờ đầu tiên sau bây giờ của ngày đang chọn (hôm nay) hoặc mốc đầu ngày.</summary>
    private void ChonMocGioMacDinh()
    {
        var ngay = NgayHen?.Date ?? DateTime.Today;
        var bayGio = DateTime.Now;
        GioHenChon = CacMocGio.FirstOrDefault(x => ngay + x.Gio > bayGio);
        if (GioHenChon is null && CacMocGio.Count > 0 && ngay == DateTime.Today)
        {
            // Hôm nay đã hết giờ làm: chuyển sang ngày mai.
            NgayHen = DateTime.Today.AddDays(1);
            GioHenChon = CacMocGio[0];
        }
    }

    private bool CoTheLuu() => !DangLuu;

    [RelayCommand(CanExecute = nameof(CoTheLuu))]
    private async Task LuuAsync()
    {
        ThongBao = string.Empty;

        var tc = ThuCungChon;
        if (KhachChon is null || tc is null)
        {
            HienThongBao("Chọn khách và thú cưng.", LoaiThongBao.Loi);
            return;
        }
        if (CoLoiThuCung)
        {
            HienThongBao(LoiThuCung, LoaiThongBao.Loi);
            return;
        }
        if (!DongGiaViewModel.DocKg(CanNang, out var kg) || QuyTacKhachHang.KiemTraCanNang(kg) is not null)
        {
            HienThongBao("Cân nặng phải là số kg lớn hơn 0, ví dụ 4,5.", LoaiThongBao.Loi);
            return;
        }

        var chon = CacDichVu.Where(x => x.Chon).ToList();
        if (chon.Count == 0)
        {
            HienThongBao("Chọn ít nhất một dịch vụ.", LoaiThongBao.Loi);
            return;
        }
        var dong = new List<DongDichVuNhap>();
        foreach (var d in chon)
        {
            var loi = d.ThuChuyen(out var nhap);
            if (loi is not null)
            {
                HienThongBao(loi, LoaiThongBao.Loi);
                return;
            }
            dong.Add(nhap!);
        }

        var gioHen = GioHenHienTai();
        if (gioHen is null)
        {
            HienThongBao("Chọn ngày và giờ hẹn.", LoaiThongBao.Loi);
            return;
        }
        if (CoLoiCa)
        {
            HienThongBao(LoiCa, LoaiThongBao.Loi);
            return;
        }
        if (LaWalkIn && LongChon is null)
        {
            HienThongBao("Chọn lồng Walk-in trống.", LoaiThongBao.Loi);
            return;
        }
        if (LaWalkIn && GroomerChon?.Groomer is null)
        {
            HienThongBao("Chọn Groomer cho phiếu Walk-in.", LoaiThongBao.Loi);
            return;
        }

        // BR11: chỉ cảnh báo, người dùng quyết định có lưu không.
        if (CoCanhBaoQuaTai
            && !_hopThoai.XacNhan(CanhBaoQuaTai + Environment.NewLine + Environment.NewLine + "Vẫn lập phiếu?", TieuDeHoi))
            return;

        var phieu = new PhieuMoi(tc.MaThuCung, LaWalkIn ? NguonTiepNhan.WalkIn : NguonTiepNhan.DatLich,
            LaWalkIn ? null : gioHen, kg, GroomerChon?.Groomer?.MaNv, LaWalkIn ? LongChon!.MaLong : null, dong);

        DangLuu = true;
        try
        {
            var kq = await _phieu.LapPhieuAsync(phieu);
            if (!kq.ThanhCong)
            {
                HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
                // Lồng, Groomer có thể vừa đổi ở máy khác.
                await TaiCaVaLongAsync();
                return;
            }

            var r = kq.DuLieu!;
            var thongBao = $"{kq.ThongBao} Tổng {QuyTacDichVu.TienHienThi(r.TongTien)} đ, {r.TongThoiGian} phút.";
            var coCo = CoCanhBaoKhach;
            await LamLaiAsync();
            HienThongBao(thongBao, coCo ? LoaiThongBao.CanhBao : LoaiThongBao.ThanhCong);
        }
        finally
        {
            DangLuu = false;
        }
    }

    /// <summary>Xoá form để lập phiếu mới (giữ ngày hẹn và nguồn tiếp nhận).</summary>
    [RelayCommand]
    private async Task LamLaiAsync()
    {
        ThongBao = string.Empty;
        DangThemKhach = false;
        DangThemThuCung = false;
        TuKhoaKhach = string.Empty;
        KhachChon = null;
        KhachTrongDanhSach = null;
        await TaiKhachAsync();
        ChonMocGioMacDinh();
        await TaiCaVaLongAsync();
    }
}
