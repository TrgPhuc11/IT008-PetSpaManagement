using SpaThuCung.Models;

namespace SpaThuCung.Services;

/// <summary>
/// Dịch vụ để hiển thị trên giao diện.
/// Là class (không phải record) để so sánh theo tham chiếu, giống NhanVienDto.
/// </summary>
public sealed class DichVuDto
{
    public DichVuDto(int maDv, string tenDv, string? moTa, string trangThai)
    {
        MaDv = maDv;
        TenDv = tenDv;
        MoTa = moTa;
        TrangThai = trangThai;
    }

    public int MaDv { get; }
    public string TenDv { get; }
    public string? MoTa { get; }
    public string TrangThai { get; }

    public bool NgungCungCap => TrangThai == TrangThaiDichVu.NgungCungCap;
}

/// <summary>Một dòng bảng giá đã lưu.</summary>
public sealed record DongGiaDto(int MaGia, string Loai, decimal CanNangTu, decimal CanNangDen, decimal Gia, int ThoiLuong);

/// <summary>Một dòng bảng giá gửi lên để lưu. MaGia = 0 là dòng mới.</summary>
public sealed record DongGiaNhap(int MaGia, string Loai, decimal CanNangTu, decimal CanNangDen, decimal Gia, int ThoiLuong);

/// <summary>Kết quả tra giá một dịch vụ theo loài và cân nặng. Gia/ThoiLuong null: không có khoảng phù hợp, cần báo giá riêng.</summary>
public sealed record GiaTraCuu(int MaDv, string TenDv, decimal? CanNangTu, decimal? CanNangDen, decimal? Gia, int? ThoiLuong)
{
    public bool CoGia => Gia is not null;

    public string KhoangHienThi => CanNangTu is null || CanNangDen is null
        ? "—"
        : QuyTacDichVu.Khoang(CanNangTu.Value, CanNangDen.Value);

    public string GiaHienThi => Gia is null ? "Cần báo giá riêng" : QuyTacDichVu.TienHienThi(Gia.Value);

    public string ThoiLuongHienThi => ThoiLuong is null ? "—" : $"{ThoiLuong} phút";
}
