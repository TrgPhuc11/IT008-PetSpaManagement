namespace SpaThuCung.Services;

/// <summary>Quy tắc kiểm tra dữ liệu khách hàng và thú cưng. Mỗi hàm trả null nếu hợp lệ, ngược lại là câu báo lỗi.</summary>
public static class QuyTacKhachHang
{
    public static string? KiemTraHoTen(string? hoTen)
    {
        if (string.IsNullOrWhiteSpace(hoTen)) return "Nhập họ tên khách.";
        if (hoTen.Trim().Length > 100) return "Họ tên tối đa 100 ký tự.";
        return null;
    }

    /// <summary>SĐT khách bắt buộc (là khoá tìm khách, BR04): 9 đến 15 chữ số.</summary>
    public static string? KiemTraSdt(string? sdt)
    {
        if (string.IsNullOrWhiteSpace(sdt)) return "Nhập số điện thoại.";
        return QuyTacNhanVien.KiemTraSdt(sdt);
    }

    public static string? KiemTraGhiChu(string? ghiChu) =>
        ghiChu is not null && ghiChu.Trim().Length > 255 ? "Ghi chú tối đa 255 ký tự." : null;

    public static string? KiemTraTenThuCung(string? ten)
    {
        if (string.IsNullOrWhiteSpace(ten)) return "Nhập tên thú cưng.";
        if (ten.Trim().Length > 50) return "Tên thú cưng tối đa 50 ký tự.";
        return null;
    }

    public static string? KiemTraLoai(string? loai) =>
        loai is not null && Models.LoaiThuCung.TatCa.Contains(loai) ? null : "Chọn loài Chó hoặc Mèo.";

    /// <summary>UC07: cân nặng nhỏ hơn hoặc bằng 0 thì báo lỗi; tối đa theo cột DECIMAL(5,2).</summary>
    public static string? KiemTraCanNang(decimal canNang)
    {
        if (canNang <= 0) return "Cân nặng phải lớn hơn 0.";
        if (canNang > QuyTacDichVu.CanNangToiDa) return $"Cân nặng tối đa {QuyTacDichVu.KgHienThi(QuyTacDichVu.CanNangToiDa)} kg.";
        if (decimal.Round(canNang, 2) != canNang) return "Cân nặng lấy tối đa 2 chữ số thập phân.";
        return null;
    }

    /// <summary>Chuỗi rỗng thành null, bỏ khoảng trắng hai đầu.</summary>
    public static string? ChuanHoa(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
