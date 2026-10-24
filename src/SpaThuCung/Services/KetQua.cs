namespace SpaThuCung.Services;

/// <summary>Kết quả của một thao tác không trả dữ liệu.</summary>
public sealed record KetQua(bool ThanhCong, string ThongBao)
{
    public static KetQua Ok(string thongBao = "") => new(true, thongBao);

    public static KetQua Loi(string thongBao) => new(false, thongBao);
}

/// <summary>Kết quả của một thao tác có trả dữ liệu.</summary>
public sealed record KetQua<T>(bool ThanhCong, string ThongBao, T? DuLieu)
{
    public static KetQua<T> Ok(T duLieu, string thongBao = "") => new(true, thongBao, duLieu);

    public static KetQua<T> Loi(string thongBao) => new(false, thongBao, default);
}
