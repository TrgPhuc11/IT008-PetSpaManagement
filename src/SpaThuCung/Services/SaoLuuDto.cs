namespace SpaThuCung.Services;

/// <summary>
/// Một bản sao lưu: thư mục chứa tệp cơ sở dữ liệu (SpaThuCung.bak), thư mục ảnh (HinhAnh) và tệp thông tin (SaoLuu.json).
/// </summary>
/// <param name="ThuMuc">Thư mục của bản sao lưu.</param>
/// <param name="TepCsdl">Đường dẫn tệp .bak.</param>
/// <param name="ThoiGian">Lúc sao lưu (lấy từ SaoLuu.json; không có thì lấy giờ sửa tệp).</param>
/// <param name="NguoiSaoLuu">Họ tên người sao lưu; rỗng nếu không rõ.</param>
/// <param name="SoAnh">Số tệp ảnh trong bản sao lưu; null nếu bản không có thư mục ảnh.</param>
/// <param name="DungLuong">Dung lượng tệp .bak (byte).</param>
public sealed record BanSaoLuuDto(
    string ThuMuc,
    string TepCsdl,
    DateTime ThoiGian,
    string NguoiSaoLuu,
    int? SoAnh,
    long DungLuong);

/// <summary>Ghi nhớ trên máy này (không nằm trong CSDL nên phục hồi không làm mất): nơi lưu và lần sao lưu gần nhất.</summary>
public sealed record CaiDatSaoLuuDto(string? NoiLuu, DateTime? LanGanNhat, string? ThuMucGanNhat);
