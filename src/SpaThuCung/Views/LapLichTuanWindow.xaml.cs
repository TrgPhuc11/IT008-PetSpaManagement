using SpaThuCung.ViewModels;

namespace SpaThuCung.Views;

public partial class LapLichTuanWindow : Wpf.Ui.Controls.FluentWindow
{
    public LapLichTuanWindow(LapLichTuanViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.YeuCauDong += ketQua => DialogResult = ketQua;
    }
}
