using CommunityToolkit.Mvvm.ComponentModel;

namespace SpaThuCung.ViewModels;

/// <summary>Cơ sở cho ViewModel của hộp thoại mở qua IHopThoaiService.MoHopThoai. Window lắng nghe YeuCauDong để đóng.</summary>
public abstract class HopThoaiViewModel : ObservableObject
{
    /// <summary>true = người dùng xác nhận (DialogResult true), false = huỷ.</summary>
    public event Action<bool>? YeuCauDong;

    protected void Dong(bool ketQua) => YeuCauDong?.Invoke(ketQua);
}
