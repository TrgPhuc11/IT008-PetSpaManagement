using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Models;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Màu của lưới lịch theo mẫu giao diện (Lễ tân xanh dương, Groomer xanh lá, ngày đã qua be đậm).</summary>
internal static class MauLich
{
    private static SolidColorBrush Dong(string ma)
    {
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ma));
        b.Freeze();
        return b;
    }

    public static readonly Brush TrongSuot = Brushes.Transparent;
    public static readonly Brush NgayDaQua = Dong("#F3EDE3");
    public static readonly Brush GoiY = Dong("#FFF7EE");
    public static readonly Brush LeTanNen = Dong("#E8EEF6");
    public static readonly Brush LeTanVien = Dong("#4A6FA0");
    public static readonly Brush LeTanChu = Dong("#27466B");
    public static readonly Brush GroomerNen = Dong("#E6F0DA");
    public static readonly Brush GroomerVien = Dong("#5A8A3A");
    public static readonly Brush GroomerChu = Dong("#33561E");
    public static readonly Brush MacDinhChu = Dong("#2B2520");
}

public enum CheDoLich
{
    Tuan,
    Thang,
    KhoangNgay,
}

/// <summary>Một mục của ComboBox Ca.</summary>
public sealed record LuaChonCa(int MaCa, string TenCa, string HienThi);

/// <summary>Một mục của ComboBox Vị trí. ViTri = null nghĩa là "Theo vị trí thường làm".</summary>
public sealed record LuaChonViTri(string? ViTri, string HienThi);

/// <summary>Một mục trong ngăn xếp Hoàn tác (chỉ trong phiên).</summary>
public sealed record MucHoanTac(string Nhan, IReadOnlyList<ThayDoiLich> NghichDao);

/// <summary>Tiêu đề một cột ngày. Không bấm được.</summary>
public sealed class CotNgayViewModel
{
    public CotNgayViewModel(DateOnly ngay, bool rong)
    {
        Ngay = ngay;
        Thu = QuyTacLichLamViec.Thu(ngay);
        NgayText = rong ? QuyTacLichLamViec.NgayNgan(ngay) : ngay.Day.ToString();
        LaHomNay = ngay == QuyTacLichLamViec.HomNay;
        DaQua = QuyTacLichLamViec.DaQua(ngay);
        Rong = rong;
        ChieuRongCot = rong ? LichLamViecViewModel.ChieuRongCotRong : LichLamViecViewModel.ChieuRongCotGon;
    }

    public DateOnly Ngay { get; }
    public string Thu { get; }
    public string NgayText { get; }
    public bool LaHomNay { get; }
    public bool DaQua { get; }
    public bool Rong { get; }
    public double ChieuRongCot { get; }
}

public enum LoaiChipCa
{
    Du,
    Thieu,
    Thua,
}

/// <summary>Chip trạng thái một ca trong một ngày ở hàng "Ca còn thiếu". Chip thiếu bấm được để gợi ý người rảnh.</summary>
public sealed partial class ChipCaViewModel : ObservableObject
{
    private readonly Action<ChipCaViewModel> _khiBam;

    public ChipCaViewModel(DateOnly ngay, int maCa, string text, string goiY, LoaiChipCa loai, string? viTriThieu,
        bool rong, Action<ChipCaViewModel> khiBam)
    {
        Ngay = ngay;
        MaCa = maCa;
        Text = text;
        GoiY = goiY;
        Loai = loai;
        ViTriThieu = viTriThieu;
        Rong = rong;
        _khiBam = khiBam;
    }

    public DateOnly Ngay { get; }
    public int MaCa { get; }
    public string Text { get; }
    public string GoiY { get; }
    public LoaiChipCa Loai { get; }

    /// <summary>Vị trí đang thiếu (Lễ tân trước, rồi Groomer); null khi không thiếu.</summary>
    public string? ViTriThieu { get; }

    public bool Rong { get; }

    public bool CoTheBam => Loai == LoaiChipCa.Thieu;

    [ObservableProperty]
    private bool _dangChon;

    [RelayCommand(CanExecute = nameof(CoTheBam))]
    private void Chon() => _khiBam(this);
}

/// <summary>Các chip của một ngày (một chip cho mỗi ca đang sử dụng). Ngày đã qua không có chip.</summary>
public sealed class CotThieuViewModel
{
    public CotThieuViewModel(DateOnly ngay, bool rong, IReadOnlyList<ChipCaViewModel> cacChip)
    {
        Ngay = ngay;
        DaQua = QuyTacLichLamViec.DaQua(ngay);
        CacChip = cacChip;
        ChieuRongCot = rong ? LichLamViecViewModel.ChieuRongCotRong : LichLamViecViewModel.ChieuRongCotGon;
    }

    public DateOnly Ngay { get; }
    public bool DaQua { get; }
    public IReadOnlyList<ChipCaViewModel> CacChip { get; }
    public double ChieuRongCot { get; }
}

/// <summary>Một ô (nhân viên, ngày) trên lưới. Có ca thì tô theo vị trí; ngày đã qua không chọn được.</summary>
public sealed partial class OLichViewModel : ObservableObject
{
    private readonly Action<OLichViewModel> _khiBam;

    public OLichViewModel(int maNv, string hoTen, DateOnly ngay, DongLichDto? dong, CaLamViecDto? ca, string kyHieuCa,
        bool rong, Action<OLichViewModel> khiBam)
    {
        MaNv = maNv;
        HoTen = hoTen;
        Ngay = ngay;
        Dong = dong;
        Ca = ca;
        KyHieuCa = kyHieuCa;
        Rong = rong;
        DaQua = QuyTacLichLamViec.DaQua(ngay);
        _khiBam = khiBam;
    }

    public int MaNv { get; }
    public string HoTen { get; }
    public DateOnly Ngay { get; }

    /// <summary>Dòng lịch hiện có (null = ô trống).</summary>
    public DongLichDto? Dong { get; }

    public CaLamViecDto? Ca { get; }
    public string KyHieuCa { get; }
    public bool Rong { get; }
    public bool DaQua { get; }

    public bool CoCa => Dong is not null;
    public bool Trong => Dong is null;
    public bool HienCong => Dong is null && !DaQua;

    public double ChieuRongO => Rong ? LichLamViecViewModel.ChieuRongORong : LichLamViecViewModel.ChieuRongOGon;
    public double ChieuCaoO => Rong ? 48 : 30;

    /// <summary>Dòng 1: tên ca (rộng) hoặc ký hiệu ca (thu gọn).</summary>
    public string Dong1 => Dong is null ? string.Empty : Rong ? (Ca?.TenCa ?? "?") : KyHieuCa;

    /// <summary>Dòng 2: "08:00–14:30 · Groomer", chỉ khi ô rộng.</summary>
    public string Dong2 => Dong is null || !Rong || Ca is null
        ? string.Empty
        : $"{Ca.GioBatDau:hh\\:mm}–{Ca.GioKetThuc:hh\\:mm} · {Dong.ViTri}";

    public Brush Nen => Dong is not null
        ? (Dong.ViTri == ViTri.LeTan ? MauLich.LeTanNen : MauLich.GroomerNen)
        : DaQua ? MauLich.NgayDaQua
        : GoiY ? MauLich.GoiY : MauLich.TrongSuot;

    public Brush VienTrai => Dong is null ? MauLich.TrongSuot
        : Dong.ViTri == ViTri.LeTan ? MauLich.LeTanVien : MauLich.GroomerVien;

    public Brush Chu => Dong is null ? MauLich.MacDinhChu
        : Dong.ViTri == ViTri.LeTan ? MauLich.LeTanChu : MauLich.GroomerChu;

    public string ChuCong => DaChon ? "✓" : "+";

    public string MoTaTruyCap =>
        $"{HoTen} {QuyTacLichLamViec.NgayNgan(Ngay)}: " +
        (Dong is not null ? $"{Ca?.TenCa} · {Dong.ViTri}" : DaQua ? "đã qua" : "trống");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChuCong))]
    private bool _daChon;

    /// <summary>Ô trống được gợi ý (người rảnh cho ca thiếu).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nen))]
    private bool _goiY;

    private bool CoTheChon => !DaQua;

    [RelayCommand(CanExecute = nameof(CoTheChon))]
    private void Chon() => _khiBam(this);
}

/// <summary>Một hàng của lưới: tiêu đề nhóm hoặc nhân viên.</summary>
public abstract class HangLichViewModel : ObservableObject
{
}

public sealed class TieuDeNhomLichViewModel : HangLichViewModel
{
    public TieuDeNhomLichViewModel(string ten) => Ten = ten;

    public string Ten { get; }
}

public sealed partial class HangNhanVienViewModel : HangLichViewModel
{
    private readonly Action<HangNhanVienViewModel> _khiBamTen;

    public HangNhanVienViewModel(int maNv, string hoTen, string phuDe, IReadOnlyList<OLichViewModel> cacO,
        Action<HangNhanVienViewModel> khiBamTen)
    {
        MaNv = maNv;
        HoTen = hoTen;
        PhuDe = phuDe;
        CacO = cacO;
        _khiBamTen = khiBamTen;
    }

    public int MaNv { get; }
    public string HoTen { get; }

    /// <summary>"Quản lý · 5 ca · 32,5 giờ".</summary>
    public string PhuDe { get; }

    public IReadOnlyList<OLichViewModel> CacO { get; }

    public string MoTaTruyCap => $"Chọn cả hàng {HoTen}";

    [RelayCommand]
    private void ChonCaHang() => _khiBamTen(this);
}
