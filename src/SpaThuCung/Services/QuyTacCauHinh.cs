using System.Globalization;
using System.IO;
using SpaThuCung.Models;

namespace SpaThuCung.Services;

/// <summary>Định nghĩa một thông số dạng số nguyên ở thẻ Thông số.</summary>
public sealed record DinhNghiaThongSo(string Ma, string Ten, string DonVi, int MacDinh, int ToiThieu, int ToiDa, string MoTa);

/// <summary>
/// Quy tắc cấu hình hệ thống (FR14, BR03, BR09). Hàm Kiem... trả null nếu hợp lệ, ngược lại trả thông báo lỗi.
/// </summary>
public static class QuyTacCauHinh
{
    public const int DoDaiTenCaToiDa = 30;
    public const int PhutVuotToiDa = 180;
    public const int SoNguoiToiDa = 10;

    /// <summary>Giới hạn cột gia_tri NVARCHAR(100).</summary>
    public const int DoDaiThuMucToiDa = 100;

    public const string ThuMucAnhMacDinh = "HinhAnh";

    /// <summary>Các thông số dạng số, theo thứ tự hiện trên màn hình. Giá trị mặc định trùng với script 03.</summary>
    public static readonly IReadOnlyList<DinhNghiaThongSo> ThongSoSo = new List<DinhNghiaThongSo>
    {
        new(MaCauHinh.ThoiGianChoKhongDen, "Thời gian chờ để tính Không đến", "phút", 30, 1, 240,
            "Quá giờ hẹn bấy nhiêu phút mà chưa nhận thú cưng thì phiếu tự chuyển Không đến."),
        new(MaCauHinh.NguongCanhBaoKhongDen, "Ngưỡng cảnh báo khách không đến", "lần", 3, 1, 20,
            "Khách có từ bấy nhiêu lần Không đến thì hệ thống cảnh báo khi đặt lịch."),
        new(MaCauHinh.ThoiHanDoiLich, "Thời hạn dời lịch", "giờ", 12, 0, 168,
            "Dời lịch phải trước giờ hẹn ít nhất bấy nhiêu giờ. Báo trễ và huỷ phiếu không áp dụng."),
        new(MaCauHinh.SoAnhToiDa, "Số ảnh tối đa mỗi loại", "ảnh", 5, 1, 20,
            "Mỗi phiếu có tối đa bấy nhiêu ảnh Lúc nhận và bấy nhiêu ảnh Sau chăm sóc."),
    };

    public static DinhNghiaThongSo? TimThongSo(string ma) => ThongSoSo.FirstOrDefault(x => x.Ma == ma);

    // ---------- Thông số ----------

    /// <summary>Đọc chuỗi nhập thành số nguyên trong khoảng của thông số.</summary>
    public static string? KiemTraSo(DinhNghiaThongSo ts, string? nhap, out int giaTri)
    {
        giaTri = 0;
        if (string.IsNullOrWhiteSpace(nhap)) return $"Nhập {ts.Ten.ToLowerInvariant()}.";
        if (!int.TryParse(nhap.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out giaTri))
            return "Phải là số nguyên.";
        return KiemTraSo(ts, giaTri);
    }

    public static string? KiemTraSo(DinhNghiaThongSo ts, int giaTri) =>
        giaTri < ts.ToiThieu || giaTri > ts.ToiDa ? $"Phải từ {ts.ToiThieu} đến {ts.ToiDa} {ts.DonVi}." : null;

    public static string? KiemTraThuMuc(string? thuMuc)
    {
        if (string.IsNullOrWhiteSpace(thuMuc)) return "Chọn thư mục lưu ảnh.";
        var t = thuMuc.Trim();
        if (t.Length > DoDaiThuMucToiDa) return $"Đường dẫn tối đa {DoDaiThuMucToiDa} ký tự.";
        if (t.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return "Đường dẫn có ký tự không hợp lệ.";
        return null;
    }

    /// <summary>Đường dẫn đầy đủ của thư mục ảnh: tương đối thì tính từ thư mục chương trình.</summary>
    public static string DuongDanDayDu(string thuMuc)
    {
        var t = string.IsNullOrWhiteSpace(thuMuc) ? ThuMucAnhMacDinh : thuMuc.Trim();
        return Path.GetFullPath(Path.IsPathRooted(t) ? t : Path.Combine(AppContext.BaseDirectory, t));
    }

    // ---------- Ca làm việc ----------

    /// <summary>Đọc giờ dạng H:mm hoặc HH:mm (ví dụ 8:00, 14:30).</summary>
    public static TimeSpan? DocGio(string? nhap)
    {
        if (string.IsNullOrWhiteSpace(nhap)) return null;
        var t = nhap.Trim().Replace('h', ':').Replace('H', ':').Replace('.', ':');
        if (t.EndsWith(':')) t += "00";
        string[] mau = { @"h\:mm", @"hh\:mm" };
        if (TimeSpan.TryParseExact(t, mau, CultureInfo.InvariantCulture, out var gio) && gio < TimeSpan.FromDays(1))
            return gio;
        return null;
    }

    public static string GioHienThi(TimeSpan gio) =>
        gio >= TimeSpan.FromDays(1)
            ? $"{gio.Days * 24 + gio.Hours:00}:{gio.Minutes:00}"
            : gio.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// Kiểm tra một ca trước khi thêm/sửa. Ca đang sử dụng không được chồng giờ với ca đang sử dụng khác
    /// (ca này kết thúc 14:30, ca kia bắt đầu 14:30 là hợp lệ).
    /// </summary>
    public static string? KiemTraCa(CaLamViecDto ca, IEnumerable<CaLamViecDto> cacCa)
    {
        var ten = (ca.TenCa ?? string.Empty).Trim();
        if (ten.Length == 0) return "Nhập tên ca.";
        if (ten.Length > DoDaiTenCaToiDa) return $"Tên ca tối đa {DoDaiTenCaToiDa} ký tự.";

        var loiGio = KiemTraGio(ca.GioBatDau, ca.GioKetThuc);
        if (loiGio is not null) return loiGio;

        if (ca.PhutVuotChoPhep < 0 || ca.PhutVuotChoPhep > PhutVuotToiDa)
            return $"Số phút được lố phải từ 0 đến {PhutVuotToiDa}.";
        if (ca.SoLeTan < 0 || ca.SoLeTan > SoNguoiToiDa) return $"Số Lễ tân phải từ 0 đến {SoNguoiToiDa}.";
        if (ca.SoGroomer < 0 || ca.SoGroomer > SoNguoiToiDa) return $"Số Groomer phải từ 0 đến {SoNguoiToiDa}.";

        var khac = cacCa.Where(x => x.MaCa != ca.MaCa).ToList();
        if (khac.Any(x => string.Equals(x.TenCa.Trim(), ten, StringComparison.CurrentCultureIgnoreCase)))
            return $"Đã có ca tên \"{ten}\".";

        return ca.DangSuDung ? KiemTraChongGio(ca, khac) : null;
    }

    public static string? KiemTraGio(TimeSpan batDau, TimeSpan ketThuc)
    {
        if (batDau < TimeSpan.Zero || batDau >= TimeSpan.FromDays(1) || ketThuc < TimeSpan.Zero || ketThuc >= TimeSpan.FromDays(1))
            return "Giờ phải trong khoảng 00:00 đến 23:59.";
        if (batDau.Seconds != 0 || ketThuc.Seconds != 0) return "Giờ chỉ tính đến phút.";
        if (ketThuc <= batDau) return "Giờ kết thúc phải sau giờ bắt đầu.";
        return null;
    }

    /// <summary>Lỗi nếu ca chồng giờ với một ca đang sử dụng khác.</summary>
    public static string? KiemTraChongGio(CaLamViecDto ca, IEnumerable<CaLamViecDto> cacCa)
    {
        var trung = cacCa.FirstOrDefault(x => x.MaCa != ca.MaCa && x.DangSuDung
            && ca.GioBatDau < x.GioKetThuc && x.GioBatDau < ca.GioKetThuc);
        return trung is null
            ? null
            : $"Ca chồng giờ với {trung.TenCa} ({GioHienThi(trung.GioBatDau)}–{GioHienThi(trung.GioKetThuc)}).";
    }

    /// <summary>Ca đang sử dụng chứa giờ hẹn (giờ bắt đầu ≤ giờ &lt; giờ kết thúc), null nếu ngoài mọi ca.</summary>
    public static CaLamViecDto? XacDinhCa(IEnumerable<CaLamViecDto> cacCa, TimeSpan gio) =>
        cacCa.Where(x => x.DangSuDung).OrderBy(x => x.GioBatDau)
            .FirstOrDefault(x => x.GioBatDau <= gio && gio < x.GioKetThuc);

    /// <summary>Giờ mở cửa = giờ bắt đầu của ca đang sử dụng sớm nhất.</summary>
    public static TimeSpan? GioMoCua(IEnumerable<CaLamViecDto> cacCa) =>
        cacCa.Where(x => x.DangSuDung).Select(x => (TimeSpan?)x.GioBatDau).Min();

    /// <summary>Giờ đóng cửa = giờ kết thúc của ca đang sử dụng muộn nhất.</summary>
    public static TimeSpan? GioDongCua(IEnumerable<CaLamViecDto> cacCa) =>
        cacCa.Where(x => x.DangSuDung).Select(x => (TimeSpan?)x.GioKetThuc).Max();
}
