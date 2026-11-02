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

    public string? ChonThuMuc(string tieuDe, string? thuMucBanDau)
    {
        var hop = new Microsoft.Win32.OpenFolderDialog { Title = tieuDe, Multiselect = false };
        if (!string.IsNullOrWhiteSpace(thuMucBanDau) && System.IO.Directory.Exists(thuMucBanDau))
            hop.InitialDirectory = thuMucBanDau;

        var chu = Application.Current?.MainWindow;
        var ok = chu is not null ? hop.ShowDialog(chu) : hop.ShowDialog();
        return ok == true ? hop.FolderName : null;
    }
}
