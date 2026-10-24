namespace SpaThuCung.Services;

/// <summary>Chuyển giữa các cửa sổ mà ViewModel không phải biết Window cụ thể.</summary>
public interface IDieuHuongCuaSo
{
    void MoManHinhChinh();
    void VeDangNhap();
}
