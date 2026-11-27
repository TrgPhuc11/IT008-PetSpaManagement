using SpaThuCung.ViewModels;

namespace SpaThuCung.Views;

public partial class NhanThuCungWindow : Wpf.Ui.Controls.FluentWindow
{
    public NhanThuCungWindow(NhanThuCungViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.YeuCauDong += ketQua => DialogResult = ketQua;
    }
}
