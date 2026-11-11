namespace SpaThuCung.Services;

/// <summary>
/// Khách hàng để hiển thị. Là class để DataGrid so sánh theo tham chiếu (giống NhanVienDto).
/// </summary>
public sealed class KhachHangDto
{
    public KhachHangDto(int maKh, string hoTen, string sdt, string? ghiChu, int soThuCung, int soLanKhongDen, bool coCanhBao)
    {
        MaKh = maKh;
        HoTen = hoTen;
        Sdt = sdt;
        GhiChu = ghiChu;
        SoThuCung = soThuCung;
        SoLanKhongDen = soLanKhongDen;
        CoCanhBao = coCanhBao;
    }

    public int MaKh { get; }
    public string HoTen { get; }
    public string Sdt { get; }
    public string? GhiChu { get; }
    public int SoThuCung { get; }

    /// <summary>Số phiếu "Không đến" của khách (BR05).</summary>
    public int SoLanKhongDen { get; }

    /// <summary>Số lần không đến ≥ ngưỡng cảnh báo trong Cấu hình (mặc định 3).</summary>
    public bool CoCanhBao { get; }

    /// <summary>Mã dạng KH001.</summary>
    public string MaHienThi => $"KH{MaKh:000}";

    /// <summary>Dùng trong ComboBox chọn chủ thú cưng.</summary>
    public string TenVaSdt => $"{HoTen} · {Sdt}";
}

/// <summary>Thú cưng để hiển thị, kèm tên và SĐT chủ.</summary>
public sealed class ThuCungDto
{
    public ThuCungDto(int maThuCung, int maKh, string tenChu, string sdtChu, string ten, string loai, decimal canNang,
        bool daTiemPhong, string? ghiChu)
    {
        MaThuCung = maThuCung;
        MaKh = maKh;
        TenChu = tenChu;
        SdtChu = sdtChu;
        Ten = ten;
        Loai = loai;
        CanNang = canNang;
        DaTiemPhong = daTiemPhong;
        GhiChu = ghiChu;
    }

    public int MaThuCung { get; }
    public int MaKh { get; }
    public string TenChu { get; }
    public string SdtChu { get; }
    public string Ten { get; }
    public string Loai { get; }
    public decimal CanNang { get; }
    public bool DaTiemPhong { get; }
    public string? GhiChu { get; }

    /// <summary>Mã dạng TC001.</summary>
    public string MaHienThi => $"TC{MaThuCung:000}";

    public string CanNangHienThi => QuyTacDichVu.KgHienThi(CanNang);

    public string TiemPhongHienThi => DaTiemPhong ? "Đã tiêm" : "Chưa xác nhận";
}

/// <summary>Dữ liệu khách hàng nhập ở form.</summary>
public sealed record KhachHangNhap(string HoTen, string Sdt, string? GhiChu);

/// <summary>Dữ liệu thú cưng nhập ở form. Khi sửa, MaKh bị bỏ qua (không đổi chủ).</summary>
public sealed record ThuCungNhap(int MaKh, string Ten, string Loai, decimal CanNang, bool DaTiemPhong, string? GhiChu);

/// <summary>
/// Kết quả thêm khách. DaCoHoSo = true khi SĐT đã có hồ sơ: không tạo mới mà trả về mã hồ sơ cũ để mở (FR03).
/// </summary>
public sealed record KetQuaThemKhach(int MaKh, bool DaCoHoSo, string HoTenHoSoCu);
