using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Models;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>
/// Màn Dịch vụ và bảng giá (UC08, UC09). Quản lý thêm, sửa dịch vụ và chỉnh bảng giá;
/// Nhân viên chỉ xem và tra giá theo loài, cân nặng.
/// </summary>
public partial class DichVuViewModel : ObservableObject, ICoTaiDuLieu
{
    private readonly IDichVuService _dichVu;
    private readonly IHopThoaiService _hopThoai;

    private List<DichVuDto> _tatCa = new();

    // Dịch vụ đang sửa trong form (giữ lại để đổi dòng chọn không ảnh hưởng form).
    private int _maDangThaoTac;

    // Dịch vụ của bảng giá đang hiển thị, và số lần tải để bỏ kết quả của lần tải cũ về muộn.
    private int? _maBangGiaDangHien;
    private int _lanTaiBangGia;

    // Mã các dòng đã có trong CSDL mà người dùng bấm Xoá (chỉ xoá thật khi Lưu bảng giá).
    private readonly HashSet<int> _maDaXoa = new();

    // Đang lọc lại danh sách: DataGrid tạm đẩy dòng chọn về null, không tải lại bảng giá theo.
    private bool _dangLoc;

    public DichVuViewModel(IDichVuService dichVu, IPhienLamViec phien, IHopThoaiService hopThoai)
    {
        _dichVu = dichVu;
        _hopThoai = hopThoai;
        LaQuanLy = phien.LaQuanLy;
    }

    /// <summary>Chỉ Quản lý thấy các nút thêm, sửa và sửa được bảng giá (Service vẫn kiểm tra lại quyền).</summary>
    public bool LaQuanLy { get; }

    public string MoTaManHinh => LaQuanLy
        ? "Giá và thời lượng theo loài và khoảng cân nặng. Quản lý được thêm, sửa dịch vụ và chỉnh bảng giá."
        : "Giá và thời lượng theo loài và khoảng cân nặng. Nhân viên chỉ xem; nhập loài và cân nặng để tra giá tư vấn cho khách.";

    public ObservableCollection<DichVuDto> DanhSach { get; } = new();

    public ObservableCollection<DongGiaViewModel> DongGia { get; } = new();

    public ObservableCollection<GiaTraCuu> KetQuaTra { get; } = new();

    public IReadOnlyList<string> DanhSachLoai => LoaiThuCung.TatCa;

    // ---------- Tìm kiếm, chọn dòng ----------

    [ObservableProperty]
    private string _tuKhoa = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoChonDichVu))]
    [NotifyPropertyChangedFor(nameof(TieuDeBangGia))]
    [NotifyPropertyChangedFor(nameof(NhanDoiTrangThai))]
    [NotifyCanExecuteChangedFor(nameof(SuaCommand))]
    [NotifyCanExecuteChangedFor(nameof(DoiTrangThaiCommand))]
    [NotifyCanExecuteChangedFor(nameof(ThemMucGiaCommand))]
    private DichVuDto? _dichVuDuocChon;

    public bool CoChonDichVu => DichVuDuocChon is not null;

    public string TieuDeBangGia => DichVuDuocChon is null ? "Bảng giá" : $"Bảng giá: {DichVuDuocChon.TenDv}";

    public string NhanDoiTrangThai => DichVuDuocChon?.NgungCungCap == true ? "Mở lại dịch vụ" : "Ngừng cung cấp";

    partial void OnTuKhoaChanged(string value) => LocDanhSach();

    partial void OnDichVuDuocChonChanged(DichVuDto? value)
    {
        if (!_dangLoc)
            _ = TaiBangGiaAsync(value?.MaDv, false);
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

    // ---------- Form thêm / sửa dịch vụ ----------

    [ObservableProperty]
    private CheDoForm _cheDo = CheDoForm.Dong;

    [ObservableProperty]
    private string _formTen = string.Empty;

    [ObservableProperty]
    private string _formMoTa = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoFormLoi))]
    private string _formLoi = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LuuDichVuCommand))]
    private bool _dangLuu;

    public bool CoFormLoi => !string.IsNullOrEmpty(FormLoi);

    public bool DangMoForm => CheDo != CheDoForm.Dong;

    public string NutLuuForm => CheDo == CheDoForm.Them ? "Thêm" : "Lưu";

    partial void OnCheDoChanged(CheDoForm value)
    {
        OnPropertyChanged(nameof(DangMoForm));
        OnPropertyChanged(nameof(NutLuuForm));
    }

    // ---------- Bảng giá ----------

    /// <summary>Bảng giá có chỉnh sửa chưa lưu. Khi đó khoá danh sách dịch vụ để không mất dữ liệu đang sửa.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChonDuocDichVu))]
    [NotifyCanExecuteChangedFor(nameof(LuuDichVuCommand))]
    [NotifyCanExecuteChangedFor(nameof(ThemCommand))]
    [NotifyCanExecuteChangedFor(nameof(SuaCommand))]
    [NotifyCanExecuteChangedFor(nameof(DoiTrangThaiCommand))]
    [NotifyCanExecuteChangedFor(nameof(HoanTacCommand))]
    [NotifyCanExecuteChangedFor(nameof(LuuBangGiaCommand))]
    private bool _coThayDoi;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoLoiBangGia))]
    private string _loiBangGia = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BangGiaChiDoc))]
    [NotifyPropertyChangedFor(nameof(SuaDuocBangGia))]
    [NotifyCanExecuteChangedFor(nameof(LuuBangGiaCommand))]
    [NotifyCanExecuteChangedFor(nameof(HoanTacCommand))]
    [NotifyCanExecuteChangedFor(nameof(ThemMucGiaCommand))]
    private bool _dangLuuBangGia;

    /// <summary>Bảng giá của dịch vụ đang chọn đã tải thành công. Tải lỗi thì không cho sửa, tránh lưu đè lên dữ liệu chưa thấy.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BangGiaChiDoc))]
    [NotifyPropertyChangedFor(nameof(SuaDuocBangGia))]
    [NotifyCanExecuteChangedFor(nameof(ThemMucGiaCommand))]
    [NotifyCanExecuteChangedFor(nameof(LuuBangGiaCommand))]
    private bool _bangGiaDaTai;

    /// <summary>Bảng giá không sửa được: Nhân viên, đang lưu hoặc chưa tải được.</summary>
    public bool BangGiaChiDoc => !LaQuanLy || DangLuuBangGia || !BangGiaDaTai;

    public bool SuaDuocBangGia => !BangGiaChiDoc;

    public bool CoLoiBangGia => !string.IsNullOrEmpty(LoiBangGia);

    public bool ChonDuocDichVu => !CoThayDoi;

    public bool BangGiaTrong => CoChonDichVu && DongGia.Count == 0;

    // ---------- Tra giá (UC09) ----------

    [ObservableProperty]
    private string _traLoai = LoaiThuCung.Cho;

    [ObservableProperty]
    private string _traCanNang = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoKetQuaTra))]
    private string _tieuDeKetQuaTra = string.Empty;

    public bool CoKetQuaTra => !string.IsNullOrEmpty(TieuDeKetQuaTra);

    // ---------- Tải dữ liệu ----------

    public async Task TaiDuLieuAsync() => await TaiLaiAsync(null);

    /// <summary>Tải lại danh sách dịch vụ; chonMa là dịch vụ cần chọn lại (null: giữ dòng đang chọn).</summary>
    private async Task<bool> TaiLaiAsync(int? chonMa)
    {
        var kq = await _dichVu.LayDanhSachAsync();
        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return false;
        }

        _tatCa = kq.DuLieu ?? new List<DichVuDto>();
        LocDanhSach(chonMa);

        // Vào màn hình lần đầu thì chọn sẵn dịch vụ đầu tiên để thấy bảng giá.
        if (DichVuDuocChon is null && DanhSach.Count > 0 && string.IsNullOrWhiteSpace(TuKhoa))
            DichVuDuocChon = DanhSach[0];
        return true;
    }

    /// <summary>Lọc trên danh sách đã tải, không phân biệt hoa thường và dấu.</summary>
    private void LocDanhSach(int? chonMa = null)
    {
        // Xoá danh sách làm DataGrid đẩy DichVuDuocChon = null, nên nhớ mã trước.
        var ma = chonMa ?? DichVuDuocChon?.MaDv;
        var tuKhoa = BoDau(TuKhoa.Trim());

        IEnumerable<DichVuDto> kq = _tatCa;
        if (tuKhoa.Length > 0)
            kq = kq.Where(x => BoDau(x.TenDv).Contains(tuKhoa) || BoDau(x.MoTa ?? string.Empty).Contains(tuKhoa));

        _dangLoc = true;
        try
        {
            DanhSach.Clear();
            foreach (var dv in kq)
                DanhSach.Add(dv);

            DichVuDuocChon = ma is null ? null : DanhSach.FirstOrDefault(x => x.MaDv == ma.Value);
        }
        finally
        {
            _dangLoc = false;
        }

        // Chỉ tải lại khi dịch vụ được chọn thực sự đổi.
        _ = TaiBangGiaAsync(DichVuDuocChon?.MaDv, false);
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

    /// <summary>Tải bảng giá của dịch vụ; batBuoc = false thì bỏ qua khi bảng đang hiện đúng dịch vụ đó.</summary>
    private async Task TaiBangGiaAsync(int? maDv, bool batBuoc)
    {
        if (!batBuoc && maDv == _maBangGiaDangHien) return;

        var lan = ++_lanTaiBangGia;
        _maBangGiaDangHien = maDv;
        BangGiaDaTai = false;
        LoiBangGia = string.Empty;
        DatBangGia(Array.Empty<DongGiaDto>());

        if (maDv is null) return;

        var kq = await _dichVu.LayBangGiaAsync(maDv.Value);
        if (lan != _lanTaiBangGia) return; // Người dùng đã chọn dịch vụ khác trong lúc chờ.

        if (!kq.ThanhCong)
        {
            // Cho phép chọn lại dòng này để tải lại.
            _maBangGiaDangHien = null;
            LoiBangGia = kq.ThongBao;
            return;
        }
        DatBangGia(kq.DuLieu ?? new List<DongGiaDto>());
        BangGiaDaTai = true;
    }

    private void DatBangGia(IEnumerable<DongGiaDto> ds)
    {
        DongGia.Clear();
        _maDaXoa.Clear();
        foreach (var d in ds)
            DongGia.Add(new DongGiaViewModel(d, KhiBangGiaThayDoi));
        CoThayDoi = false;
        OnPropertyChanged(nameof(BangGiaTrong));
    }

    private void KhiBangGiaThayDoi()
    {
        CoThayDoi = true;
        LoiBangGia = string.Empty;
        OnPropertyChanged(nameof(BangGiaTrong));
    }

    // ---------- Lệnh trên thanh công cụ (Quản lý) ----------

    private bool CoTheThem() => LaQuanLy && !CoThayDoi;

    private bool CoTheSuaDichVu() => LaQuanLy && !CoThayDoi && DichVuDuocChon is not null;

    [RelayCommand(CanExecute = nameof(CoTheThem))]
    private void Them()
    {
        ThongBao = string.Empty;
        DatLaiForm();
        CheDo = CheDoForm.Them;
    }

    [RelayCommand(CanExecute = nameof(CoTheSuaDichVu))]
    private void Sua()
    {
        var dv = DichVuDuocChon;
        if (dv is null) return;

        ThongBao = string.Empty;
        DatLaiForm();
        _maDangThaoTac = dv.MaDv;
        FormTen = dv.TenDv;
        FormMoTa = dv.MoTa ?? string.Empty;
        CheDo = CheDoForm.Sua;
    }

    [RelayCommand(CanExecute = nameof(CoTheSuaDichVu))]
    private async Task DoiTrangThaiAsync()
    {
        var dv = DichVuDuocChon;
        if (dv is null) return;

        // Ngừng cung cấp thì hỏi lại; mở lại thì làm luôn.
        if (!dv.NgungCungCap)
        {
            var dongY = _hopThoai.XacNhan(
                $"Ngừng cung cấp {dv.TenDv}? Dịch vụ sẽ không chọn được khi lập phiếu mới; phiếu đã lập không bị ảnh hưởng.",
                "Đổi trạng thái dịch vụ");
            if (!dongY) return;
        }

        var kq = await _dichVu.DoiTrangThaiAsync(dv.MaDv);
        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return;
        }

        if (await TaiLaiAsync(dv.MaDv))
            HienThongBao(kq.ThongBao, LoaiThongBao.ThanhCong);
    }

    // ---------- Lệnh trong form ----------

    [RelayCommand]
    private void HuyForm()
    {
        CheDo = CheDoForm.Dong;
        DatLaiForm();
    }

    [RelayCommand(CanExecute = nameof(CoTheLuuDichVu))]
    private async Task LuuDichVuAsync()
    {
        FormLoi = string.Empty;

        // Kiểm tra nhanh trong ViewModel; quy tắc đầy đủ do Service kiểm tra lại.
        if (string.IsNullOrWhiteSpace(FormTen))
        {
            FormLoi = "Nhập tên dịch vụ.";
            return;
        }

        DangLuu = true;
        try
        {
            if (CheDo == CheDoForm.Them)
            {
                var kq = await _dichVu.ThemAsync(FormTen, FormMoTa);
                await XuLyKetQuaLuuAsync(kq.ThanhCong, kq.ThongBao, kq.DuLieu?.MaDv);
            }
            else if (CheDo == CheDoForm.Sua)
            {
                var kq = await _dichVu.CapNhatAsync(_maDangThaoTac, FormTen, FormMoTa);
                await XuLyKetQuaLuuAsync(kq.ThanhCong, kq.ThongBao, _maDangThaoTac);
            }
        }
        finally
        {
            DangLuu = false;
        }
    }

    // Không lưu form khi bảng giá đang sửa dở: lưu xong danh sách được chọn lại và bảng giá sẽ bị tải lại.
    private bool CoTheLuuDichVu() => !DangLuu && !CoThayDoi;

    /// <summary>Lỗi thì giữ form và hiện lỗi trong form; thành công thì đóng form, tải lại, chọn lại dòng, báo ở hộp trên.</summary>
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
        FormTen = string.Empty;
        FormMoTa = string.Empty;
        FormLoi = string.Empty;
    }

    // ---------- Lệnh của bảng giá (Quản lý) ----------

    private bool CoTheThemMucGia() => LaQuanLy && DichVuDuocChon is not null && BangGiaDaTai && !DangLuuBangGia;

    [RelayCommand(CanExecute = nameof(CoTheThemMucGia))]
    private void ThemMucGia()
    {
        // Dòng mới nối tiếp khoảng cân nặng lớn nhất của cùng loài cho đỡ phải gõ.
        var dong = new DongGiaViewModel(null, KhiBangGiaThayDoi);
        var denLonNhat = DongGia
            .Where(x => x.Loai == dong.Loai)
            .Select(x => DongGiaViewModel.DocKg(x.Den, out var kg) ? kg : (decimal?)null)
            .Max();
        dong.Tu = denLonNhat is null ? "0" : QuyTacDichVu.KgHienThi(denLonNhat.Value);

        DongGia.Add(dong);
        KhiBangGiaThayDoi();
    }

    [RelayCommand]
    private void XoaMucGia(DongGiaViewModel? dong)
    {
        if (BangGiaChiDoc || dong is null) return;
        if (DongGia.Remove(dong))
        {
            if (dong.MaGia != 0) _maDaXoa.Add(dong.MaGia);
            KhiBangGiaThayDoi();
        }
    }

    private bool CoTheHoanTac() => LaQuanLy && CoThayDoi && !DangLuuBangGia;

    [RelayCommand(CanExecute = nameof(CoTheHoanTac))]
    private async Task HoanTacAsync()
    {
        ThongBao = string.Empty;
        await TaiBangGiaAsync(DichVuDuocChon?.MaDv, true);
    }

    private bool CoTheLuuBangGia() => LaQuanLy && CoThayDoi && BangGiaDaTai && !DangLuuBangGia;

    [RelayCommand(CanExecute = nameof(CoTheLuuBangGia))]
    private async Task LuuBangGiaAsync()
    {
        var dv = DichVuDuocChon;
        if (dv is null) return;

        LoiBangGia = string.Empty;

        var ds = new List<DongGiaNhap>();
        for (var i = 0; i < DongGia.Count; i++)
        {
            var loi = DongGia[i].ThuChuyen(out var dong);
            if (loi is not null)
            {
                LoiBangGia = $"Dòng {i + 1}: {loi}";
                return;
            }
            ds.Add(dong!);
        }

        DangLuuBangGia = true;
        try
        {
            var kq = await _dichVu.LuuBangGiaAsync(dv.MaDv, ds, _maDaXoa.ToList());
            if (!kq.ThanhCong)
            {
                LoiBangGia = kq.ThongBao;
                return;
            }

            await TaiBangGiaAsync(dv.MaDv, true);
            HienThongBao(kq.ThongBao, LoaiThongBao.ThanhCong);
        }
        finally
        {
            DangLuuBangGia = false;
        }
    }

    // ---------- Tra giá ----------

    [RelayCommand]
    private async Task TraGiaAsync()
    {
        if (!DongGiaViewModel.DocKg(TraCanNang, out var canNang) || canNang <= 0)
        {
            HienThongBao("Nhập cân nặng của thú cưng (kg), ví dụ 4,5.", LoaiThongBao.CanhBao);
            return;
        }

        var kq = await _dichVu.TraGiaAsync(TraLoai, canNang);
        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return;
        }

        ThongBao = string.Empty;
        KetQuaTra.Clear();
        foreach (var g in kq.DuLieu ?? new List<GiaTraCuu>())
            KetQuaTra.Add(g);

        TieuDeKetQuaTra = $"Giá cho {TraLoai.ToLowerInvariant()} {QuyTacDichVu.KgHienThi(canNang)} kg";
        if (KetQuaTra.Any(x => !x.CoGia))
            HienThongBao("Dịch vụ ghi \"Cần báo giá riêng\" không có khoảng cân nặng phù hợp: nhập tay giá và thời lượng khi lập phiếu.",
                LoaiThongBao.CanhBao);
    }

    [RelayCommand]
    private void DongTraGia()
    {
        KetQuaTra.Clear();
        TieuDeKetQuaTra = string.Empty;
    }
}
