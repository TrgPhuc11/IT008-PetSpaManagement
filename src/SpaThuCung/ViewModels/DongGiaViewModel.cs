using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SpaThuCung.Models;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Một dòng đang sửa trong bảng giá. Các ô là chuỗi để giữ nguyên những gì người dùng gõ.</summary>
public partial class DongGiaViewModel : ObservableObject
{
    // Gán sau khi nạp dữ liệu ban đầu để việc nạp không bị tính là "có thay đổi".
    private readonly Action? _khiThayDoi;

    public DongGiaViewModel(DongGiaDto? dto, Action khiThayDoi)
    {
        if (dto is not null)
        {
            MaGia = dto.MaGia;
            Loai = dto.Loai;
            Tu = QuyTacDichVu.KgHienThi(dto.CanNangTu);
            Den = QuyTacDichVu.KgHienThi(dto.CanNangDen);
            Gia = QuyTacDichVu.TienHienThi(dto.Gia);
            ThoiLuong = dto.ThoiLuong.ToString(CultureInfo.InvariantCulture);
        }
        _khiThayDoi = khiThayDoi;
    }

    /// <summary>0 là dòng mới thêm.</summary>
    public int MaGia { get; }

    [ObservableProperty]
    private string _loai = LoaiThuCung.Cho;

    [ObservableProperty]
    private string _tu = string.Empty;

    [ObservableProperty]
    private string _den = string.Empty;

    [ObservableProperty]
    private string _gia = string.Empty;

    [ObservableProperty]
    private string _thoiLuong = string.Empty;

    partial void OnLoaiChanged(string value) => _khiThayDoi?.Invoke();
    partial void OnTuChanged(string value) => _khiThayDoi?.Invoke();
    partial void OnDenChanged(string value) => _khiThayDoi?.Invoke();
    partial void OnGiaChanged(string value) => _khiThayDoi?.Invoke();
    partial void OnThoiLuongChanged(string value) => _khiThayDoi?.Invoke();

    /// <summary>Đổi các ô thành số; trả thông báo lỗi nếu ô nào không đọc được.</summary>
    public string? ThuChuyen(out DongGiaNhap? dong)
    {
        dong = null;
        if (!DocKg(Tu, out var tu)) return "cân nặng \"Từ\" không phải là số.";
        if (!DocKg(Den, out var den)) return "cân nặng \"Đến\" không phải là số.";
        if (!DocSoNguyen(Gia, out var gia)) return "giá phải là số nguyên (VNĐ).";
        if (!DocSoNguyen(ThoiLuong, out var phut) || phut > int.MaxValue) return "thời lượng phải là số phút.";

        dong = new DongGiaNhap(MaGia, Loai, tu, den, gia, (int)phut);
        return null;
    }

    /// <summary>Nhận cả dấu phẩy và dấu chấm thập phân (2,5 hoặc 2.5).</summary>
    public static bool DocKg(string? s, out decimal kg) =>
        decimal.TryParse((s ?? string.Empty).Trim().Replace(',', '.'),
            NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out kg);

    /// <summary>Bỏ dấu chấm, dấu phẩy, khoảng trắng phân cách hàng nghìn (150.000 = 150000).</summary>
    private static bool DocSoNguyen(string? s, out decimal so)
    {
        var chuoi = new string((s ?? string.Empty).Where(c => c is not ('.' or ',' or ' ' or ' ')).ToArray());
        so = 0;
        return chuoi.Length > 0 && chuoi.All(c => c is >= '0' and <= '9')
            && decimal.TryParse(chuoi, NumberStyles.None, CultureInfo.InvariantCulture, out so);
    }
}
