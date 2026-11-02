using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Một dòng trong danh sách ca (chỉ hiển thị; sửa bằng form của CauHinhViewModel).</summary>
public sealed class CaLamViecMucViewModel
{
    public CaLamViecMucViewModel(CaLamViecDto ca)
    {
        Ca = ca;
    }

    public CaLamViecDto Ca { get; }

    public int MaCa => Ca.MaCa;
    public string TenCa => Ca.TenCa;
    public bool DangSuDung => Ca.DangSuDung;
    public bool NgungSuDung => !Ca.DangSuDung;

    public string GioHienThi => $"{QuyTacCauHinh.GioHienThi(Ca.GioBatDau)} – {QuyTacCauHinh.GioHienThi(Ca.GioKetThuc)}";

    /// <summary>Thời lượng ca = số phút tối đa của một Groomer trong ca.</summary>
    public string ThoiLuongHienThi => $"{Ca.ThoiLuongPhut} phút";

    public string LoHienThi => Ca.PhutVuotChoPhep == 0
        ? "Không được lố"
        : $"{Ca.PhutVuotChoPhep} phút (đến {QuyTacCauHinh.GioHienThi(Ca.GioLoToiDa)})";

    public string SoLeTanHienThi => Ca.SoLeTan.ToString();
    public string SoGroomerHienThi => Ca.SoGroomer.ToString();

    public string TrangThaiHienThi => Ca.DangSuDung ? "Đang sử dụng" : "Ngừng sử dụng";

    public string NutTrangThai => Ca.DangSuDung ? "Ngừng sử dụng" : "Dùng lại";
}
