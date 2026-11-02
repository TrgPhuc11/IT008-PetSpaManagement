namespace SpaThuCung.Services;

/// <summary>Hộp thoại hỏi người dùng, để ViewModel không gọi MessageBox trực tiếp.</summary>
public interface IHopThoaiService
{
    /// <summary>Hỏi Có/Không; true nếu người dùng chọn Có.</summary>
    bool XacNhan(string noiDung, string tieuDe);

    /// <summary>Hộp thoại chọn thư mục; null nếu người dùng huỷ.</summary>
    string? ChonThuMuc(string tieuDe, string? thuMucBanDau);
}
