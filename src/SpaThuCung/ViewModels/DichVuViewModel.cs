using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Models;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>
/// Màn Dịch vụ và bảng giá (UC08, UC09). Mỗi dịch vụ là một thẻ, bấm mũi tên để xổ bảng giá.
/// Quản lý bấm Chỉnh sửa để sửa ngay trong thẻ, mỗi lần một dịch vụ; Nhân viên chỉ xem và tra giá.
/// </summary>
public partial class DichVuViewModel : ObservableObject, ICoTaiDuLieu
{
    private readonly IDichVuService _dichVu;
    private readonly IHopThoaiService _hopThoai;

    private List<DichVuDto> _tatCa = new();

    // Các dịch vụ đang xổ bảng giá, giữ lại khi tải lại hoặc lọc danh sách.
    private readonly HashSet<int> _maDangMo = new();

    public DichVuViewModel(IDichVuService dichVu, IPhienLamViec phien, IHopThoaiService hopThoai)
    {
        _dichVu = dichVu;
        _hopThoai = hopThoai;
        LaQuanLy = phien.LaQuanLy;
    }

    /// <summary>Chỉ Quản lý thấy nút Thêm dịch vụ, Chỉnh sửa (Service vẫn kiểm tra lại quyền).</summary>
    public bool LaQuanLy { get; }

    public string MoTaManHinh => LaQuanLy
        ? "Bấm mũi tên để xem giá và thời lượng theo loài, khoảng cân nặng. Bấm Chỉnh sửa để sửa một dịch vụ và bảng giá của nó."
        : "Bấm mũi tên để xem giá và thời lượng theo loài, khoảng cân nặng. Nhập loài và cân nặng để tra giá tư vấn cho khách.";

    public ObservableCollection<DichVuMucViewModel> DanhSach { get; } = new();

    public ObservableCollection<GiaTraCuu> KetQuaTra { get; } = new();

    public IReadOnlyList<string> DanhSachLoai => LoaiThuCung.TatCa;

    // ---------- Tìm kiếm ----------

    [ObservableProperty]
    private string _tuKhoa = string.Empty;

    partial void OnTuKhoaChanged(string value) => LocDanhSach();

    public bool KhongCoDichVu => DanhSach.Count == 0;

    // ---------- Dịch vụ đang chỉnh sửa (mỗi lần một) ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KhongDangSua))]
    [NotifyCanExecuteChangedFor(nameof(ThemDichVuCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChinhSuaCommand))]
    [NotifyCanExecuteChangedFor(nameof(HuySuaCommand))]
    [NotifyCanExecuteChangedFor(nameof(LuuCommand))]
    [NotifyCanExecuteChangedFor(nameof(ThemMucGiaCommand))]
    [NotifyCanExecuteChangedFor(nameof(XoaMucGiaCommand))]
    private DichVuMucViewModel? _mucDangSua;

    /// <summary>Không có dịch vụ nào đang sửa: mới cho tìm, thêm dịch vụ, bấm Chỉnh sửa dịch vụ khác.</summary>
    public bool KhongDangSua => MucDangSua is null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(HuySuaCommand))]
    [NotifyCanExecuteChangedFor(nameof(LuuCommand))]
    [NotifyCanExecuteChangedFor(nameof(ThemMucGiaCommand))]
    [NotifyCanExecuteChangedFor(nameof(XoaMucGiaCommand))]
    private bool _dangLuu;

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

    public async Task TaiDuLieuAsync() => await TaiLaiAsync();

    private async Task<bool> TaiLaiAsync()
    {
        var kq = await _dichVu.LayDanhSachAsync();
        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return false;
        }

        _tatCa = kq.DuLieu ?? new List<DichVuDto>();
        LocDanhSach();
        return true;
    }

    /// <summary>Dựng lại các thẻ từ danh sách đã tải, lọc theo từ khoá (không phân biệt hoa thường và dấu).</summary>
    private void LocDanhSach()
    {
        // Đang sửa thì ô tìm bị khoá; vẫn kiểm tra để không bao giờ xoá mất thẻ đang sửa.
        if (MucDangSua is not null) return;

        var tuKhoa = BoDau(TuKhoa.Trim());

        IEnumerable<DichVuDto> kq = _tatCa;
        if (tuKhoa.Length > 0)
            kq = kq.Where(x => BoDau(x.TenDv).Contains(tuKhoa) || BoDau(x.MoTa ?? string.Empty).Contains(tuKhoa));

        DanhSach.Clear();
        foreach (var dv in kq)
            DanhSach.Add(new DichVuMucViewModel(dv, _maDangMo.Contains(dv.MaDv), KhiDoiMo));
        OnPropertyChanged(nameof(KhongCoDichVu));
    }

    private void KhiDoiMo(DichVuMucViewModel muc)
    {
        if (muc.LaMoi) return;
        if (muc.DangMo) _maDangMo.Add(muc.MaDv);
        else _maDangMo.Remove(muc.MaDv);
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

    // ---------- Bắt đầu / thôi chỉnh sửa ----------

    private bool CoTheBatDauSua() => LaQuanLy && MucDangSua is null;

    [RelayCommand(CanExecute = nameof(CoTheBatDauSua))]
    private void ThemDichVu()
    {
        ThongBao = string.Empty;
        var muc = new DichVuMucViewModel(null, true, KhiDoiMo);
        DanhSach.Insert(0, muc);
        OnPropertyChanged(nameof(KhongCoDichVu));
        muc.BatDauSua();
        MucDangSua = muc;
    }

    [RelayCommand(CanExecute = nameof(CoTheBatDauSua))]
    private void ChinhSua(DichVuMucViewModel? muc)
    {
        if (muc is null || !CoTheBatDauSua()) return;

        ThongBao = string.Empty;
        muc.BatDauSua();
        MucDangSua = muc;
    }

    private bool DangSuaVaRanh() => MucDangSua is not null && !DangLuu;

    [RelayCommand(CanExecute = nameof(DangSuaVaRanh))]
    private void HuySua()
    {
        var muc = MucDangSua;
        if (muc is null) return;

        MucDangSua = null;
        if (muc.LaMoi)
        {
            DanhSach.Remove(muc);
            OnPropertyChanged(nameof(KhongCoDichVu));
        }
        else
        {
            muc.HuySua();
        }

        // Tải lại để thấy thay đổi người khác vừa lưu (nếu có).
        _ = TaiLaiAsync();
    }

    // ---------- Sửa bảng giá trong thẻ đang chỉnh sửa ----------

    [RelayCommand(CanExecute = nameof(DangSuaVaRanh))]
    private void ThemMucGia()
    {
        var muc = MucDangSua;
        if (muc is null) return;

        // Dòng mới nối tiếp khoảng cân nặng lớn nhất của cùng loài cho đỡ phải gõ.
        var dong = new DongGiaViewModel(null, true);
        var denLonNhat = muc.DongGia
            .Where(x => x.Loai == dong.Loai)
            .Select(x => DongGiaViewModel.DocKg(x.Den, out var kg) ? kg : (decimal?)null)
            .Max();
        dong.Tu = denLonNhat is null ? "0" : QuyTacDichVu.KgHienThi(denLonNhat.Value);

        muc.ThemDong(dong);
    }

    [RelayCommand(CanExecute = nameof(DangSuaVaRanh))]
    private void XoaMucGia(DongGiaViewModel? dong)
    {
        if (dong is null || !dong.CoTheSua) return;
        MucDangSua?.XoaDong(dong);
    }

    [RelayCommand(CanExecute = nameof(DangSuaVaRanh))]
    private async Task LuuAsync()
    {
        var muc = MucDangSua;
        if (muc is null) return;

        muc.Loi = string.Empty;

        // Kiểm tra nhanh trong ViewModel; quy tắc đầy đủ do Service kiểm tra lại.
        if (string.IsNullOrWhiteSpace(muc.FormTen))
        {
            muc.Loi = "Nhập tên dịch vụ.";
            return;
        }

        var ds = new List<DongGiaNhap>();
        for (var i = 0; i < muc.DongGia.Count; i++)
        {
            var loi = muc.DongGia[i].ThuChuyen(out var dong);
            if (loi is not null)
            {
                muc.Loi = $"Dòng {i + 1}: {loi}";
                return;
            }
            ds.Add(dong!);
        }

        // Chuyển sang Ngừng cung cấp thì hỏi lại.
        if (!muc.NgungCungCap && !muc.LaMoi && muc.FormTrangThai == TrangThaiDichVu.NgungCungCap)
        {
            var dongY = _hopThoai.XacNhan(
                $"Ngừng cung cấp {muc.TenDv}? Dịch vụ sẽ không chọn được khi lập phiếu mới; phiếu đã lập không bị ảnh hưởng.",
                "Ngừng cung cấp dịch vụ");
            if (!dongY) return;
        }

        DangLuu = true;
        try
        {
            var kq = await _dichVu.LuuAsync(new DichVuLuu(
                muc.MaDv, muc.FormTen, muc.FormMoTa, muc.FormTrangThai, ds, muc.MaDaXoa.ToList()));
            if (!kq.ThanhCong)
            {
                muc.Loi = kq.ThongBao;
                return;
            }

            // Lưu xong: thôi sửa, giữ thẻ vừa lưu đang xổ, tải lại danh sách.
            _maDangMo.Add(kq.DuLieu);
            MucDangSua = null;
            if (await TaiLaiAsync())
                HienThongBao(kq.ThongBao, LoaiThongBao.ThanhCong);
        }
        finally
        {
            DangLuu = false;
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
