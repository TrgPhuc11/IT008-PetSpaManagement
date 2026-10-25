using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpaThuCung.Data;
using SpaThuCung.Services;
using SpaThuCung.ViewModels;
using SpaThuCung.Views;
using Wpf.Ui.Appearance;

namespace SpaThuCung;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Lỗi không bắt được thì hiện thông báo, không để app chạy ngầm mà không có cửa sổ.
        DispatcherUnhandledException += KhiCoLoiChuaXuLy;

        // Màu nhấn cam đất cho viền focus, nút Primary... của WPF-UI.
        ApplicationAccentColorManager.Apply(Color.FromRgb(0xA9, 0x48, 0x1F), ApplicationTheme.Light);

        string connectionString;
        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            connectionString = config.GetConnectionString("SpaThuCung")
                ?? throw new InvalidOperationException("Thiếu ConnectionStrings:SpaThuCung.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không đọc được cấu hình trong appsettings.json.\n\n" + ex.Message,
                "Spa thú cưng", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        _services = DungServiceProvider(connectionString);

        // Hiện màn Đăng nhập ngay; việc kết nối/tạo CSDL chạy sau khi cửa sổ đã hiện.
        var dangNhap = _services.GetRequiredService<LoginWindow>();
        MainWindow = dangNhap;
        dangNhap.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }

    private void KhiCoLoiChuaXuLy(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            "Đã xảy ra lỗi không mong muốn:\n\n" + e.Exception,
            "Spa thú cưng", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;

        // Lỗi xảy ra trước khi có cửa sổ nào thì thoát hẳn, không chạy ngầm.
        if (Windows.Count == 0)
            Shutdown(1);
    }

    private static ServiceProvider DungServiceProvider(string connectionString)
    {
        var sc = new ServiceCollection();

        sc.AddDbContextFactory<SpaDbContext>(o => o.UseSqlServer(connectionString));
        sc.AddSingleton(new KhoiTaoCsdl(connectionString));

        sc.AddSingleton<IPhienLamViec, PhienLamViec>();
        sc.AddSingleton<IDieuHuongCuaSo, DieuHuongCuaSo>();
        sc.AddSingleton<IHopThoaiService, HopThoaiService>();
        sc.AddTransient<IAuthService, AuthService>();
        sc.AddTransient<INhanVienService, NhanVienService>();

        // Cửa sổ và ViewModel là Transient: mỗi lần về đăng nhập là form mới.
        sc.AddTransient<LoginViewModel>();
        sc.AddTransient<MainViewModel>();
        sc.AddTransient<TrangChuViewModel>();
        sc.AddTransient<NhanVienViewModel>();
        sc.AddTransient<DoiMatKhauViewModel>();
        sc.AddTransient<LoginWindow>();
        sc.AddTransient<MainWindow>();
        sc.AddTransient<DoiMatKhauWindow>();

        return sc.BuildServiceProvider();
    }
}
