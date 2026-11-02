namespace SpaThuCung.Models;

/// <summary>Mã các thông số trong bảng CauHinh, để các Service đọc giá trị không phải gõ chuỗi tay.</summary>
public static class MaCauHinh
{
    /// <summary>Quá giờ hẹn bao nhiêu phút chưa nhận thì tự chuyển Không đến.</summary>
    public const string ThoiGianChoKhongDen = "THOI_GIAN_CHO_KHONG_DEN";

    /// <summary>Khách có từ bấy nhiêu lần Không đến thì cảnh báo.</summary>
    public const string NguongCanhBaoKhongDen = "NGUONG_CANH_BAO_KHONG_DEN";

    /// <summary>Dời lịch phải trước giờ hẹn ít nhất bấy nhiêu giờ.</summary>
    public const string ThoiHanDoiLich = "THOI_HAN_DOI_LICH";

    /// <summary>Số ảnh tối đa mỗi loại của một phiếu.</summary>
    public const string SoAnhToiDa = "SO_ANH_TOI_DA";

    /// <summary>Thư mục lưu ảnh thú cưng (tương đối thì tính từ thư mục chương trình).</summary>
    public const string ThuMucAnh = "THU_MUC_ANH";
}
