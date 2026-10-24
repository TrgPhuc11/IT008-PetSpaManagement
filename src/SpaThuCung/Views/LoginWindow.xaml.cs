using SpaThuCung.ViewModels;

namespace SpaThuCung.Views;

public partial class LoginWindow : Wpf.Ui.Controls.FluentWindow
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Cửa sổ hiện xong mới kết nối CSDL.
        Loaded += async (_, _) => await viewModel.KhoiTaoCsdlAsync();
    }
}
