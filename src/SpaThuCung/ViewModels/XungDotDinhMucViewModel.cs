using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Cách xử lý một ô vướng định mức. MaNvThayThe = null nghĩa là không đổi.</summary>
public sealed record LuaChonXuLy(int? MaNvThayThe, string HienThi);

/// <summary>Một ô vướng định mức trong hộp thoại xác nhận đổi người.</summary>
public sealed partial class OXungDotViewModel : ObservableObject
{
    private readonly Action _khiDoiLuaChon;

    public OXungDotViewModel(OVuongDinhMuc o, string tenNguoiMoi, Action khiDoiLuaChon)
    {
        O = o;
        _khiDoiLuaChon = khiDoiLuaChon;

        var giu = o.NguoiGiu.Select(x => x.HoTen).ToList();
        TieuDe = $"{o.TenCa} · {QuyTacLichLamViec.ThuDayDu(o.Ngay)} {QuyTacLichLamViec.NgayNgan(o.Ngay)} · {o.ViTri} " +
                 $"(đủ {o.NguoiGiu.Count}/{o.DinhMuc})";
        NoiDung = giu.Count == 0
            ? $"{o.TenCa} không nhận {o.ViTri} (định mức {o.DinhMuc} người) nên không xếp {tenNguoiMoi} được."
            : $"{o.TenCa} này đã do {string.Join(" và ", giu)} nhận. " +
              $"Bạn có chắc muốn đổi lịch để {tenNguoiMoi} làm thay?";

        CacLuaChon = new[] { new LuaChonXuLy(null, "Không đổi, giữ nguyên") }
            .Concat(o.NguoiGiu.Select(x => new LuaChonXuLy(x.MaNv, $"Đổi: {x.HoTen} → {tenNguoiMoi}")))
            .ToList();
        _luaChon = CacLuaChon[0];
    }

    public OVuongDinhMuc O { get; }
    public string TieuDe { get; }
    public string NoiDung { get; }
    public IReadOnlyList<LuaChonXuLy> CacLuaChon { get; }

    [ObservableProperty]
    private LuaChonXuLy _luaChon;

    public bool CoDoi => LuaChon.MaNvThayThe is not null;

    partial void OnLuaChonChanged(LuaChonXuLy value)
    {
        OnPropertyChanged(nameof(CoDoi));
        _khiDoiLuaChon();
    }
}

/// <summary>
/// Hộp thoại khi phân công vướng định mức: liệt kê từng ô đã đủ người để Quản lý chọn đổi người hoặc giữ nguyên.
/// Huỷ thì không lưu gì cả.
/// </summary>
public sealed partial class XungDotDinhMucViewModel : HopThoaiViewModel
{
    private readonly int _soOHopLe;

    public XungDotDinhMucViewModel(IReadOnlyList<OVuongDinhMuc> cacO, string tenNguoiMoi, int soOHopLe)
    {
        _soOHopLe = soOHopLe;
        CacO = cacO.Select(o => new OXungDotViewModel(o, tenNguoiMoi, CapNhatNut)).ToList();

        if (cacO.Count == 1)
        {
            var o = cacO[0];
            TieuDe = $"{o.TenCa} {QuyTacLichLamViec.NgayNgan(o.Ngay)} đã đủ {o.ViTri}";
            LoiDan = "Ca đã nhận đủ người cho vị trí này nên không nhận thêm. Bạn có thể đổi người nếu muốn.";
        }
        else
        {
            TieuDe = $"{cacO.Count} ô vướng định mức ca";
            LoiDan = "Các ca dưới đây đã nhận đủ người. Chọn đổi người cho từng ô, hoặc giữ nguyên.";
        }
    }

    public IReadOnlyList<OXungDotViewModel> CacO { get; }
    public string TieuDe { get; }
    public string LoiDan { get; }

    public bool CoOHopLe => _soOHopLe > 0;

    public string ChuOHopLe => $"{_soOHopLe} ô khác hợp lệ sẽ được phân công bình thường.";

    public string ChuXacNhan
    {
        get
        {
            var soDoi = CacO.Count(x => x.CoDoi);
            if (soDoi > 0) return $"Đổi lịch ({soDoi} ô)" + (_soOHopLe > 0 ? " và phân công" : string.Empty);
            return _soOHopLe > 0 ? $"Chỉ phân công {_soOHopLe} ô hợp lệ" : "Đóng";
        }
    }

    private void CapNhatNut() => OnPropertyChanged(nameof(ChuXacNhan));

    /// <summary>Các thay đổi đổi người Quản lý đã chọn: xoá lịch người bị thay rồi xếp người mới.</summary>
    public List<ThayDoiLich> LayCacThayDoiDoi()
    {
        var ds = new List<ThayDoiLich>();
        foreach (var o in CacO)
        {
            if (o.LuaChon.MaNvThayThe is not int maNguoiBiThay) continue;
            ds.Add(ThayDoiLich.Xoa(maNguoiBiThay, o.O.Ngay, o.O.MaCa, o.O.ViTri));
            ds.Add(o.O.ThayDoiChoNguoiMoi);
        }

        return ds;
    }

    [RelayCommand]
    private void XacNhan() => Dong(true);

    [RelayCommand]
    private void Huy() => Dong(false);
}
