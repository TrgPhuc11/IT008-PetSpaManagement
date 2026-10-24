namespace SpaThuCung.ViewModels;

/// <summary>ViewModel màn hình có dữ liệu cần tải sau khi được hiển thị.</summary>
public interface ICoTaiDuLieu
{
    Task TaiDuLieuAsync();
}
