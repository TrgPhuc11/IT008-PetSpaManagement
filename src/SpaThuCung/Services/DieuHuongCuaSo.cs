using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SpaThuCung.Views;

namespace SpaThuCung.Services;

public class DieuHuongCuaSo : IDieuHuongCuaSo
{
    private readonly IServiceProvider _sp;

    public DieuHuongCuaSo(IServiceProvider sp)
    {
        _sp = sp;
    }

    public void MoManHinhChinh() => ChuyenSang<MainWindow, LoginWindow>();

    public void VeDangNhap() => ChuyenSang<LoginWindow, MainWindow>();

    /// <summary>Mở cửa sổ mới trước rồi mới đóng cửa sổ cũ để app không tự thoát.</summary>
    private void ChuyenSang<TMoi, TCu>() where TMoi : Window where TCu : Window
    {
        var app = Application.Current;
        var cuaSoCu = app.Windows.OfType<TCu>().ToList();

        var cuaSoMoi = _sp.GetRequiredService<TMoi>();
        app.MainWindow = cuaSoMoi;
        cuaSoMoi.Show();
        cuaSoMoi.Activate();

        foreach (var w in cuaSoCu)
            w.Close();
    }
}
