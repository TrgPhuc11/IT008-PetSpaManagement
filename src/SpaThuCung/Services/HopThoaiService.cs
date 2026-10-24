using System.Windows;

namespace SpaThuCung.Services;

public class HopThoaiService : IHopThoaiService
{
    public bool XacNhan(string noiDung, string tieuDe)
    {
        var chu = Application.Current?.MainWindow;
        var kq = chu is not null
            ? MessageBox.Show(chu, noiDung, tieuDe, MessageBoxButton.YesNo, MessageBoxImage.Question)
            : MessageBox.Show(noiDung, tieuDe, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return kq == MessageBoxResult.Yes;
    }
}
