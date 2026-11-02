using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>
/// Màn Cấu hình hệ thống (UC19, chỉ Quản lý). Thẻ Ca làm việc: thêm, sửa, ngừng/dùng lại ca (form mỗi lần một ca).
/// Thẻ Thông số: các thông số vận hành, một nút Lưu chung.
/// </summary>
public partial class CauHinhViewModel : ObservableObject, ICoTaiDuLieu
{
    private readonly ICauHinhService _cauHinh;
    private readonly IHopThoaiService _hopThoai;

    public CauHinhViewModel(ICauHinhService cauHinh, IHopThoaiService hopThoai)
    {
        _cauHinh = cauHinh;
        _hopThoai = hopThoai;
    }

    public async Task TaiDuLieuAsync()
    {
        await TaiCaAsync();
        await TaiThongSoAsync();
    }

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
    [NotifyCanExecuteChangedFor(nameof(ThemCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(SuaCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(DoiTrangThaiCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(XoaCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(LuuCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(HuyCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(LuuThongSoCommand))]
    [NotifyCanExecuteChangedFor(nameof(HoanTacThongSoCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChonThuMucCommand))]
    private bool _dangLuu;

    // =====================================================================
    // Thẻ Ca làm việc
    // =====================================================================

    public ObservableCollection<CaLamViecMucViewModel> DanhSachCa { get; } = new();

    [ObservableProperty]
    private string _tomTatCa = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ThemCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(SuaCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(DoiTrangThaiCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(XoaCaCommand))]
    private bool _daTaiCa;

    private async Task<bool> TaiCaAsync()
    {
        var kq = await _cauHinh.LayDanhSachCaAsync();
        if (!kq.ThanhCong)
        {
            DaTaiCa = false;
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return false;
        }

        var ds = kq.DuLieu ?? new List<CaLamViecDto>();
        DanhSachCa.Clear();
        foreach (var ca in ds) DanhSachCa.Add(new CaLamViecMucViewModel(ca));

        var mo = QuyTacCauHinh.GioMoCua(ds);
        var dong = QuyTacCauHinh.GioDongCua(ds);
        var soDangDung = ds.Count(x => x.DangSuDung);
        TomTatCa = mo is null || dong is null
            ? "Chưa có ca nào đang sử dụng."
            : $"{soDangDung} ca đang sử dụng · Giờ mở cửa {QuyTacCauHinh.GioHienThi(mo.Value)} · Giờ đóng cửa {QuyTacCauHinh.GioHienThi(dong.Value)}";
        DaTaiCa = true;
        return true;
    }

    // ---------- Form thêm/sửa ca ----------

    /// <summary>Mã ca đang sửa; 0 là thêm ca mới; null là không mở form.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DangMoFormCa))]
    [NotifyPropertyChangedFor(nameof(KhongMoFormCa))]
    [NotifyPropertyChangedFor(nameof(TieuDeFormCa))]
    [NotifyCanExecuteChangedFor(nameof(ThemCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(SuaCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(DoiTrangThaiCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(XoaCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(LuuCaCommand))]
    [NotifyCanExecuteChangedFor(nameof(HuyCaCommand))]
    private int? _maCaDangSua;

    private string _tenCaGoc = string.Empty;

    public bool DangMoFormCa => MaCaDangSua is not null;
    public bool KhongMoFormCa => MaCaDangSua is null;

    public string TieuDeFormCa => MaCaDangSua == 0 ? "Thêm ca" : $"Sửa {_tenCaGoc}";

    [ObservableProperty]
    private string _tenCa = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(XemTruocCa))]
    private string _gioBatDau = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(XemTruocCa))]
    private string _gioKetThuc = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(XemTruocCa))]
    private string _phutVuot = string.Empty;

    [ObservableProperty]
    private string _soLeTan = string.Empty;

    [ObservableProperty]
    private string _soGroomer = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoLoiCa))]
    private string _loiCa = string.Empty;

    public bool CoLoiCa => !string.IsNullOrEmpty(LoiCa);

    /// <summary>Tính lại ngay khi gõ: số phút của một Groomer trong ca và giờ được lố tới.</summary>
    public string XemTruocCa
    {
        get
        {
            var bd = QuyTacCauHinh.DocGio(GioBatDau);
            var kt = QuyTacCauHinh.DocGio(GioKetThuc);
            if (bd is null || kt is null) return "Nhập giờ dạng HH:mm, ví dụ 08:00.";
            if (kt <= bd) return "Giờ kết thúc phải sau giờ bắt đầu.";

            var phut = (int)(kt.Value - bd.Value).TotalMinutes;
            var chu = $"Thời lượng ca: {phut} phút (số phút tối đa của 1 Groomer).";
            if (int.TryParse(PhutVuot, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lo) && lo >= 0)
                chu += $" Phiếu được kết thúc dự kiến muộn nhất {QuyTacCauHinh.GioHienThi(kt.Value + TimeSpan.FromMinutes(lo))}.";
            return chu;
        }
    }

    private bool CoTheBatDauSuaCa() => DaTaiCa && !DangLuu && MaCaDangSua is null;

    private bool DangMoFormVaRanh() => MaCaDangSua is not null && !DangLuu;

    [RelayCommand(CanExecute = nameof(CoTheBatDauSuaCa))]
    private void ThemCa()
    {
        _tenCaGoc = string.Empty;
        TenCa = string.Empty;
        GioBatDau = string.Empty;
        GioKetThuc = string.Empty;
        PhutVuot = "30";
        SoLeTan = "1";
        SoGroomer = "2";
        LoiCa = string.Empty;
        MaCaDangSua = 0;
    }

    [RelayCommand(CanExecute = nameof(CoTheBatDauSuaCaMuc))]
    private void SuaCa(CaLamViecMucViewModel? muc)
    {
        if (muc is null) return;
        var ca = muc.Ca;
        _tenCaGoc = ca.TenCa;
        TenCa = ca.TenCa;
        GioBatDau = QuyTacCauHinh.GioHienThi(ca.GioBatDau);
        GioKetThuc = QuyTacCauHinh.GioHienThi(ca.GioKetThuc);
        PhutVuot = ca.PhutVuotChoPhep.ToString(CultureInfo.InvariantCulture);
        SoLeTan = ca.SoLeTan.ToString(CultureInfo.InvariantCulture);
        SoGroomer = ca.SoGroomer.ToString(CultureInfo.InvariantCulture);
        LoiCa = string.Empty;
        MaCaDangSua = ca.MaCa;
    }

    private bool CoTheBatDauSuaCaMuc(CaLamViecMucViewModel? muc) => muc is not null && CoTheBatDauSuaCa();

    [RelayCommand(CanExecute = nameof(DangMoFormVaRanh))]
    private void HuyCa()
    {
        LoiCa = string.Empty;
        MaCaDangSua = null;
    }

    [RelayCommand(CanExecute = nameof(DangMoFormVaRanh))]
    private async Task LuuCa()
    {
        if (MaCaDangSua is not int maCa) return;

        var loi = DocFormCa(maCa, out var ca);
        if (loi is not null)
        {
            LoiCa = loi;
            return;
        }

        // Kiểm tra trước trên danh sách đang có để báo lỗi ngay; Service vẫn kiểm tra lại trên CSDL.
        loi = QuyTacCauHinh.KiemTraCa(ca, DanhSachCa.Select(x => x.Ca));
        if (loi is not null)
        {
            LoiCa = loi;
            return;
        }

        DangLuu = true;
        try
        {
            var kq = maCa == 0 ? ToKetQua(await _cauHinh.ThemCaAsync(ca)) : await _cauHinh.SuaCaAsync(ca);
            if (!kq.ThanhCong)
            {
                LoiCa = kq.ThongBao;
                return;
            }

            LoiCa = string.Empty;
            MaCaDangSua = null;
            if (await TaiCaAsync()) HienThongBao(kq.ThongBao, LoaiThongBao.ThanhCong);
        }
        finally
        {
            DangLuu = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CoTheBatDauSuaCaMuc))]
    private async Task DoiTrangThaiCa(CaLamViecMucViewModel? muc)
    {
        if (muc is null) return;
        var ngung = muc.DangSuDung;

        var noiDung = ngung
            ? $"Ngừng sử dụng {muc.TenCa}? Ca ngừng sử dụng không được chọn khi xếp lịch làm việc và lịch hẹn."
            : $"Dùng lại {muc.TenCa} ({muc.GioHienThi})?";
        if (!_hopThoai.XacNhan(noiDung, "Cấu hình hệ thống")) return;

        DangLuu = true;
        try
        {
            var kq = await _cauHinh.DoiTrangThaiCaAsync(muc.MaCa, !ngung);
            if (!kq.ThanhCong)
            {
                HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
                return;
            }
            if (await TaiCaAsync()) HienThongBao(kq.ThongBao, LoaiThongBao.ThanhCong);
        }
        finally
        {
            DangLuu = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CoTheBatDauSuaCaMuc))]
    private async Task XoaCa(CaLamViecMucViewModel? muc)
    {
        if (muc is null) return;
        if (!_hopThoai.XacNhan($"Xoá {muc.TenCa} ({muc.GioHienThi})? Chỉ xoá được ca chưa có lịch làm việc nào.", "Cấu hình hệ thống")) return;

        DangLuu = true;
        try
        {
            var kq = await _cauHinh.XoaCaAsync(muc.MaCa);
            if (!kq.ThanhCong)
            {
                HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
                return;
            }
            if (await TaiCaAsync()) HienThongBao(kq.ThongBao, LoaiThongBao.ThanhCong);
        }
        finally
        {
            DangLuu = false;
        }
    }

    private string? DocFormCa(int maCa, out CaLamViecDto ca)
    {
        ca = null!;
        var bd = QuyTacCauHinh.DocGio(GioBatDau);
        if (bd is null) return "Giờ bắt đầu nhập dạng HH:mm, ví dụ 08:00.";
        var kt = QuyTacCauHinh.DocGio(GioKetThuc);
        if (kt is null) return "Giờ kết thúc nhập dạng HH:mm, ví dụ 14:30.";
        if (!DocSoNguyen(PhutVuot, out var lo)) return "Số phút được lố phải là số nguyên.";
        if (!DocSoNguyen(SoLeTan, out var leTan)) return "Số Lễ tân phải là số nguyên.";
        if (!DocSoNguyen(SoGroomer, out var groomer)) return "Số Groomer phải là số nguyên.";

        var dangSuDung = maCa == 0 || DanhSachCa.FirstOrDefault(x => x.MaCa == maCa)?.DangSuDung != false;
        ca = new CaLamViecDto(maCa, (TenCa ?? string.Empty).Trim(), bd.Value, kt.Value, lo, leTan, groomer, dangSuDung);
        return null;
    }

    private static bool DocSoNguyen(string? s, out int v) =>
        int.TryParse((s ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v);

    private static KetQua ToKetQua(KetQua<int> kq) => kq.ThanhCong ? KetQua.Ok(kq.ThongBao) : KetQua.Loi(kq.ThongBao);

    // =====================================================================
    // Thẻ Thông số
    // =====================================================================

    public ObservableCollection<ThongSoMucViewModel> CacThongSo { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoiThuMuc))]
    [NotifyPropertyChangedFor(nameof(CoLoiThuMuc))]
    [NotifyPropertyChangedFor(nameof(ThuMucAnhDayDu))]
    private string _thuMucAnh = string.Empty;

    partial void OnThuMucAnhChanged(string value) => DanhDauSuaThongSo();

    public string? LoiThuMuc => QuyTacCauHinh.KiemTraThuMuc(ThuMucAnh);

    public bool CoLoiThuMuc => LoiThuMuc is not null;

    public string ThuMucAnhDayDu
    {
        get
        {
            if (CoLoiThuMuc) return string.Empty;
            try
            {
                return "Đường dẫn đầy đủ: " + QuyTacCauHinh.DuongDanDayDu(ThuMucAnh);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LuuThongSoCommand))]
    [NotifyCanExecuteChangedFor(nameof(HoanTacThongSoCommand))]
    private bool _thongSoDaSua;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LuuThongSoCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChonThuMucCommand))]
    private bool _daTaiThongSo;

    // Đang đổ dữ liệu từ CSDL vào ô: không tính là người dùng sửa.
    private bool _dangDoDuLieu;

    private void DanhDauSuaThongSo()
    {
        if (!_dangDoDuLieu) ThongSoDaSua = true;
    }

    private async Task<bool> TaiThongSoAsync()
    {
        var kq = await _cauHinh.LayThongSoAsync();
        if (!kq.ThanhCong)
        {
            DaTaiThongSo = false;
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return false;
        }

        var ts = kq.DuLieu!;
        _dangDoDuLieu = true;
        try
        {
            foreach (var cu in CacThongSo) cu.DaSua -= DanhDauSuaThongSo;
            CacThongSo.Clear();
            foreach (var dn in QuyTacCauHinh.ThongSoSo)
            {
                var muc = new ThongSoMucViewModel(dn, ts.GiaTriSo.TryGetValue(dn.Ma, out var v) ? v : dn.MacDinh);
                muc.DaSua += DanhDauSuaThongSo;
                CacThongSo.Add(muc);
            }
            ThuMucAnh = ts.ThuMucAnh;
        }
        finally
        {
            _dangDoDuLieu = false;
        }

        ThongSoDaSua = false;
        DaTaiThongSo = true;
        return true;
    }

    private bool CoTheLuuThongSo() => DaTaiThongSo && ThongSoDaSua && !DangLuu;

    [RelayCommand(CanExecute = nameof(CoTheLuuThongSo))]
    private async Task LuuThongSo()
    {
        var giaTri = new Dictionary<string, int>();
        foreach (var muc in CacThongSo)
        {
            if (!muc.DocGiaTri(out var v))
            {
                HienThongBao("Có ô nhập chưa đúng, xem dòng báo lỗi dưới ô.", LoaiThongBao.Loi);
                return;
            }
            giaTri[muc.DinhNghia.Ma] = v;
        }
        if (CoLoiThuMuc)
        {
            HienThongBao("Thư mục ảnh: " + LoiThuMuc, LoaiThongBao.Loi);
            return;
        }

        DangLuu = true;
        try
        {
            var kq = await _cauHinh.CapNhatAsync(new ThongSoDto(giaTri, ThuMucAnh.Trim()));
            if (!kq.ThanhCong)
            {
                HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
                return;
            }
            if (await TaiThongSoAsync()) HienThongBao(kq.ThongBao, LoaiThongBao.ThanhCong);
        }
        finally
        {
            DangLuu = false;
        }
    }

    private bool CoTheHoanTac() => ThongSoDaSua && !DangLuu;

    /// <summary>Bỏ các thay đổi chưa lưu, tải lại giá trị đang lưu.</summary>
    [RelayCommand(CanExecute = nameof(CoTheHoanTac))]
    private async Task HoanTacThongSo()
    {
        ThongBao = string.Empty;
        await TaiThongSoAsync();
    }

    private bool CoTheChonThuMuc() => DaTaiThongSo && !DangLuu;

    [RelayCommand(CanExecute = nameof(CoTheChonThuMuc))]
    private void ChonThuMuc()
    {
        string? batDau = null;
        try
        {
            if (!CoLoiThuMuc) batDau = QuyTacCauHinh.DuongDanDayDu(ThuMucAnh);
        }
        catch (Exception)
        {
            // Đường dẫn đang gõ không hợp lệ: mở hộp thoại ở vị trí mặc định.
        }

        var chon = _hopThoai.ChonThuMuc("Chọn thư mục lưu ảnh thú cưng", batDau);
        if (chon is not null) ThuMucAnh = chon;
    }
}
