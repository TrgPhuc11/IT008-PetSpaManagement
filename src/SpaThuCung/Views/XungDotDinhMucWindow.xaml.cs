using SpaThuCung.ViewModels;

namespace SpaThuCung.Views;

public partial class XungDotDinhMucWindow : Wpf.Ui.Controls.FluentWindow
{
    public XungDotDinhMucWindow(XungDotDinhMucViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.YeuCauDong += ketQua => DialogResult = ketQua;
    }
}
