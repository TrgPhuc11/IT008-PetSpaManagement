using SpaThuCung.ViewModels;

namespace SpaThuCung.Views;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Huỷ đăng ký lắng nghe phiên làm việc khi cửa sổ đóng.
        Closed += (_, _) => viewModel.Dispose();
    }
}
