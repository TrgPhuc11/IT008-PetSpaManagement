using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SpaThuCung.Models;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>
/// Thẻ của một dịch vụ: dòng tiêu đề (mũi tên xổ, tên, trạng thái, nút Chỉnh sửa) và phần xổ ra là bảng giá.
/// Ở chế độ Chỉnh sửa, phần xổ ra cho sửa tên, mô tả, trạng thái và từng mức giá.
/// </summary>
public partial class DichVuMucViewModel : ObservableObject
{
    // Gán sau khi đặt trạng thái xổ ban đầu để việc khởi tạo không báo ngược lên màn hình.
    private readonly Action<DichVuMucViewModel>? _khiDoiMo;

    /// <param name="dto">null là dịch vụ mới chưa lưu.</param>
    public DichVuMucViewModel(DichVuDto? dto, bool dangMo, Action<DichVuMucViewModel> khiDoiMo)
    {
        Dto = dto;
        DangMo = dangMo;
        NapBangGia(false);
        _khiDoiMo = khiDoiMo;
    }

    /// <summary>Dữ liệu đã lưu; null với dịch vụ mới.</summary>
    public DichVuDto? Dto { get; }

    public int MaDv => Dto?.MaDv ?? 0;

    public bool LaMoi => Dto is null;

    public string TenDv => Dto?.TenDv ?? "Dịch vụ mới";

    public string? MoTa => Dto?.MoTa;

    public bool CoMoTa => !string.IsNullOrWhiteSpace(MoTa);

    public string TrangThai => Dto?.TrangThai ?? TrangThaiDichVu.DangCungCap;

    public bool NgungCungCap => TrangThai == TrangThaiDichVu.NgungCungCap;

    public string TomTatGia => Dto is null || Dto.BangGia.Count == 0
        ? "Chưa có mức giá"
        : $"{Dto.BangGia.Count} mức giá";

    public ObservableCollection<DongGiaViewModel> DongGia { get; } = new();

    /// <summary>Mã các dòng đã lưu mà người dùng bấm Xoá (chỉ xoá thật khi Lưu).</summary>
    public HashSet<int> MaDaXoa { get; } = new();

    public bool BangGiaTrong => DongGia.Count == 0;

    /// <summary>Phần bảng giá đang xổ ra.</summary>
    [ObservableProperty]
    private bool _dangMo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChiXem))]
    private bool _dangSua;

    public bool ChiXem => !DangSua;

    // ---------- Ô nhập khi Chỉnh sửa ----------

    [ObservableProperty]
    private string _formTen = string.Empty;

    [ObservableProperty]
    private string _formMoTa = string.Empty;

    [ObservableProperty]
    private string _formTrangThai = TrangThaiDichVu.DangCungCap;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoLoi))]
    private string _loi = string.Empty;

    public bool CoLoi => !string.IsNullOrEmpty(Loi);

    partial void OnDangMoChanged(bool value) => _khiDoiMo?.Invoke(this);

    /// <summary>Vào chế độ Chỉnh sửa: chép dữ liệu đã lưu vào ô nhập, các dòng giá thành dòng sửa được.</summary>
    public void BatDauSua()
    {
        FormTen = Dto?.TenDv ?? string.Empty;
        FormMoTa = Dto?.MoTa ?? string.Empty;
        FormTrangThai = TrangThai;
        Loi = string.Empty;
        NapBangGia(true);
        DangSua = true;
        DangMo = true;
    }

    /// <summary>Bỏ mọi thay đổi, về lại dữ liệu đã lưu.</summary>
    public void HuySua()
    {
        Loi = string.Empty;
        NapBangGia(false);
        DangSua = false;
    }

    public void ThemDong(DongGiaViewModel dong)
    {
        DongGia.Add(dong);
        OnPropertyChanged(nameof(BangGiaTrong));
    }

    public void XoaDong(DongGiaViewModel dong)
    {
        if (!DongGia.Remove(dong)) return;
        if (dong.MaGia != 0) MaDaXoa.Add(dong.MaGia);
        OnPropertyChanged(nameof(BangGiaTrong));
    }

    private void NapBangGia(bool coTheSua)
    {
        DongGia.Clear();
        MaDaXoa.Clear();
        if (Dto is not null)
        {
            foreach (var d in Dto.BangGia)
                DongGia.Add(new DongGiaViewModel(d, coTheSua));
        }
        OnPropertyChanged(nameof(BangGiaTrong));
    }
}
