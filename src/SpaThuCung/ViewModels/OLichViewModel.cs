using CommunityToolkit.Mvvm.ComponentModel;
using SpaThuCung.Models;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Tiêu đề một cột ngày của lưới tuần.</summary>
public sealed class CotNgayViewModel
{
    public CotNgayViewModel(DateOnly ngay)
    {
        Ngay = ngay;
    }

    public DateOnly Ngay { get; }
    public string Thu => QuyTacLichLamViec.Thu(Ngay);
    public string NgayNgan => QuyTacLichLamViec.NgayNgan(Ngay);
    public bool LaHomNay => Ngay == QuyTacLichLamViec.HomNay;
    public bool DaQua => QuyTacLichLamViec.DaQua(Ngay);
}

/// <summary>Một hàng của lưới tuần: một ca, bảy ô ngày.</summary>
public sealed class HangCaViewModel
{
    public HangCaViewModel(CaLamViecDto ca, IReadOnlyList<OLichViewModel> cacO)
    {
        Ca = ca;
        CacO = cacO;
    }

    public CaLamViecDto Ca { get; }
    public IReadOnlyList<OLichViewModel> CacO { get; }

    public string TenCa => Ca.TenCa;
    public string GioHienThi => $"{QuyTacCauHinh.GioHienThi(Ca.GioBatDau)} – {QuyTacCauHinh.GioHienThi(Ca.GioKetThuc)}";
    public bool NgungSuDung => !Ca.DangSuDung;
}

/// <summary>Một ô của lưới: một ca trong một ngày, gồm các nhân viên được xếp và dòng định mức.</summary>
public sealed class OLichViewModel
{
    public OLichViewModel(CaLamViecDto ca, DateOnly ngay, IReadOnlyList<TheLichViewModel> cacThe)
    {
        Ngay = ngay;
        CacThe = cacThe;
        SoLeTan = cacThe.Count(x => x.LaLeTan);
        SoGroomer = cacThe.Count(x => !x.LaLeTan);
        DinhMucLeTan = ca.SoLeTan;
        DinhMucGroomer = ca.SoGroomer;
        CaDangSuDung = ca.DangSuDung;
    }

    public DateOnly Ngay { get; }
    public IReadOnlyList<TheLichViewModel> CacThe { get; }

    public int SoLeTan { get; }
    public int SoGroomer { get; }
    public int DinhMucLeTan { get; }
    public int DinhMucGroomer { get; }
    public bool CaDangSuDung { get; }

    public bool LaHomNay => Ngay == QuyTacLichLamViec.HomNay;
    public bool DaQua => QuyTacLichLamViec.DaQua(Ngay);

    /// <summary>Số người khác số cấu hình của ca (thiếu hoặc thừa). Chỉ để Quản lý cân nhắc, không chặn.</summary>
    public bool LechDinhMuc => SoLeTan != DinhMucLeTan || SoGroomer != DinhMucGroomer;

    /// <summary>Tô vàng: lệch định mức ở ca đang dùng, ngày chưa qua.</summary>
    public bool CanCanhBao => LechDinhMuc && CaDangSuDung && !DaQua;

    public string DinhMucHienThi => $"LT {SoLeTan}/{DinhMucLeTan} · GR {SoGroomer}/{DinhMucGroomer}";

    public string DinhMucChiTiet =>
        $"Lễ tân: {SoLeTan}/{DinhMucLeTan} người, Groomer: {SoGroomer}/{DinhMucGroomer} người (theo cấu hình ca)";
}

/// <summary>Thẻ tên một nhân viên trong ô lịch.</summary>
public sealed class TheLichViewModel
{
    public TheLichViewModel(DongLichDto dong)
    {
        Dong = dong;
    }

    public DongLichDto Dong { get; }

    public int MaLich => Dong.MaLich;
    public string HoTen => Dong.HoTen;
    public string ViTri => Dong.ViTri;
    public bool LaLeTan => Dong.ViTri == Models.ViTri.LeTan;
    public bool DaNghiViec => Dong.DaNghiViec;
    public bool DaQua => QuyTacLichLamViec.DaQua(Dong.Ngay);
    public bool XoaDuoc => !DaQua;

    /// <summary>Tên hiển thị trên thẻ: họ tên đầy đủ, người đã nghỉ việc có ghi chú.</summary>
    public string TenHienThi => DaNghiViec ? $"{HoTen} (đã nghỉ)" : HoTen;

    public string ChuThich => $"{HoTen} · {ViTri} · {QuyTacLichLamViec.ThuVaNgay(Dong.Ngay)}";

    public string NhanNutXoa => $"Xoá {HoTen} khỏi lịch {QuyTacLichLamViec.ThuVaNgay(Dong.Ngay)}";
}

/// <summary>Ô tick chọn một ngày trong khung "Xếp lịch nhiều ngày".</summary>
public sealed partial class ChonNgayViewModel : ObservableObject
{
    public ChonNgayViewModel(DateOnly ngay, bool duocChon)
    {
        Ngay = ngay;
        _duocChon = duocChon && CoTheChon;
    }

    public DateOnly Ngay { get; }
    public string Thu => QuyTacLichLamViec.Thu(Ngay);
    public string ChuThich => QuyTacLichLamViec.DaQua(Ngay)
        ? $"{QuyTacLichLamViec.ThuVaNgay(Ngay)} đã qua, không xếp được"
        : QuyTacLichLamViec.ThuVaNgay(Ngay);

    /// <summary>Ngày đã qua không chọn được.</summary>
    public bool CoTheChon => !QuyTacLichLamViec.DaQua(Ngay);

    [ObservableProperty]
    private bool _duocChon;
}
