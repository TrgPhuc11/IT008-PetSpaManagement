namespace SpaThuCung.Services;

/// <summary>
/// Sao lưu và phục hồi dữ liệu (UC20, NFR07, chỉ Quản lý): sao lưu cơ sở dữ liệu (SQL Server BACKUP) kèm thư mục ảnh
/// thú cưng ra thư mục hoặc ổ đĩa ngoài; phục hồi từ một bản sao lưu (ghi đè dữ liệu hiện tại).
/// Lỗi thì dữ liệu hiện tại không bị thay đổi.
/// </summary>
public interface ISaoLuuService
{
    /// <summary>Nơi lưu và lần sao lưu gần nhất đã ghi nhớ trên máy này.</summary>
    CaiDatSaoLuuDto DocCaiDat();

    /// <summary>Ghi nhớ nơi lưu vừa chọn để lần sau mở màn hình là có sẵn.</summary>
    void GhiNhoNoiLuu(string noiLuu);

    /// <summary>
    /// Tạo một bản sao lưu mới trong <paramref name="noiLuu"/>: thư mục SpaThuCung_SaoLuu_yyyyMMdd_HHmmss gồm
    /// SpaThuCung.bak, HinhAnh và SaoLuu.json. Lỗi thì xoá phần đã ghi dở.
    /// </summary>
    Task<KetQua<BanSaoLuuDto>> SaoLuuAsync(string noiLuu);

    /// <summary>Các bản sao lưu có trong <paramref name="noiLuu"/> (kể cả khi chính thư mục này là một bản), mới nhất trước.</summary>
    Task<KetQua<List<BanSaoLuuDto>>> LayDanhSachBanSaoLuuAsync(string noiLuu);

    /// <summary>Thông tin bản sao lưu từ một tệp .bak người dùng tự chọn. Tệp có hợp lệ không thì kiểm khi phục hồi.</summary>
    KetQua<BanSaoLuuDto> DocBanSaoLuu(string tepCsdl);

    /// <summary>
    /// Phục hồi cơ sở dữ liệu và thư mục ảnh từ bản sao lưu. Trước khi ghi đè, tự sao lưu dữ liệu hiện tại ra tệp
    /// TruocPhucHoi.bak; phục hồi lỗi thì quay lại bản đó. Thành công thì người dùng phải đăng nhập lại.
    /// </summary>
    Task<KetQua> PhucHoiAsync(string tepCsdl);
}
