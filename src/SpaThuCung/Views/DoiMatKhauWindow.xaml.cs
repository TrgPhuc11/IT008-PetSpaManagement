using SpaThuCung.ViewModels;

namespace SpaThuCung.Views;

public partial class DoiMatKhauWindow : Wpf.Ui.Controls.FluentWindow
{
    public DoiMatKhauWindow(DoiMatKhauViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.YeuCauDong += Close;
    }
}
