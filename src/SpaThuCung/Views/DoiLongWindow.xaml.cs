using SpaThuCung.ViewModels;

namespace SpaThuCung.Views;

public partial class DoiLongWindow : Wpf.Ui.Controls.FluentWindow
{
    public DoiLongWindow(DoiLongViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.YeuCauDong += ketQua => DialogResult = ketQua;
    }
}
