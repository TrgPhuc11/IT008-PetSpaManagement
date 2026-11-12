using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>
/// Một dịch vụ trong form Lập phiếu: giá, thời lượng tra theo loài và cân nặng (BR08).
/// Không có khoảng cân nặng phù hợp thì bắt buộc nhập tay; có giá vẫn được nhập tay cho trường hợp đặc biệt.
/// </summary>
public partial class DichVuPhieuViewModel : ObservableObject
{
    private readonly Action _khiDoi;

    public DichVuPhieuViewModel(GiaTraCuu tra, Action khiDoi)
    {
        _khiDoi = khiDoi;
        Tra = tra;
        _nhapTay = !tra.CoGia;
    }

    public GiaTraCuu Tra { get; }

    public int MaDv => Tra.MaDv;
    public string TenDv => Tra.TenDv;
    public bool CoGia => Tra.CoGia;

    /// <summary>Không có giá trong bảng giá: luôn phải nhập tay.</summary>
    public bool BatBuocNhapTay => !Tra.CoGia;

    public bool DuocBoNhapTay => Tra.CoGia;

    /// <summary>Ví dụ "5–10 kg · 150.000 đ · 60 phút" hoặc "Không có khoảng cân phù hợp".</summary>
    public string MoTaGia => Tra.CoGia
        ? $"{Tra.KhoangHienThi} · {QuyTacDichVu.TienHienThi(Tra.Gia!.Value)} đ · {Tra.ThoiLuong} phút"
        : "Không có khoảng cân phù hợp, cần nhập tay";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HienONhapTay))]
    private bool _chon;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HienONhapTay))]
    private bool _nhapTay;

    [ObservableProperty]
    private string _giaNhap = string.Empty;

    [ObservableProperty]
    private string _thoiLuongNhap = string.Empty;

    [ObservableProperty]
    private string _ghiChu = string.Empty;

    public bool HienONhapTay => Chon && NhapTay;

    partial void OnChonChanged(bool value) => _khiDoi();

    partial void OnNhapTayChanged(bool value)
    {
        // Không có giá thì không bỏ nhập tay được.
        if (!value && BatBuocNhapTay)
        {
            NhapTay = true;
            return;
        }
        if (value && Tra.CoGia && GiaNhap.Length == 0 && ThoiLuongNhap.Length == 0)
        {
            // Gợi ý sẵn giá trong bảng giá để sửa.
            GiaNhap = QuyTacDichVu.TienHienThi(Tra.Gia!.Value);
            ThoiLuongNhap = Tra.ThoiLuong!.Value.ToString(CultureInfo.InvariantCulture);
        }
        _khiDoi();
    }

    partial void OnGiaNhapChanged(string value) => _khiDoi();
    partial void OnThoiLuongNhapChanged(string value) => _khiDoi();

    /// <summary>Giá đang dùng (tra hoặc nhập tay); null nếu ô nhập tay chưa hợp lệ.</summary>
    public decimal? DonGia => NhapTay ? DocSoNguyen(GiaNhap) : Tra.Gia;

    /// <summary>Thời lượng đang dùng (tra hoặc nhập tay); null nếu ô nhập tay chưa hợp lệ.</summary>
    public int? ThoiLuong
    {
        get
        {
            if (!NhapTay) return Tra.ThoiLuong;
            var so = DocSoNguyen(ThoiLuongNhap);
            return so is null || so > int.MaxValue ? null : (int)so.Value;
        }
    }

    /// <summary>Chép trạng thái người dùng đã nhập từ dòng cũ (khi tra lại giá theo cân nặng mới).</summary>
    public void ChepTu(DichVuPhieuViewModel cu)
    {
        Chon = cu.Chon;
        // Dòng cũ bị buộc nhập tay (không có giá) mà nay đã có giá thì dùng giá tra được.
        if (cu.NhapTay && (cu.DuocBoNhapTay || BatBuocNhapTay))
        {
            GiaNhap = cu.GiaNhap;
            ThoiLuongNhap = cu.ThoiLuongNhap;
            NhapTay = true;
        }
        GhiChu = cu.GhiChu;
    }

    /// <summary>Kiểm tra và tạo dòng gửi lên Service; trả thông báo lỗi nếu chưa hợp lệ.</summary>
    public string? ThuChuyen(out DongDichVuNhap? dong)
    {
        dong = null;
        if (!NhapTay)
        {
            dong = new DongDichVuNhap(MaDv, false, null, null, null);
            return null;
        }

        var loi = QuyTacPhieu.KiemTraNhapTay(DonGia, ThoiLuong, GhiChu);
        if (loi is not null) return $"{TenDv}: {loi}";
        dong = new DongDichVuNhap(MaDv, true, DonGia, ThoiLuong, GhiChu.Trim());
        return null;
    }

    /// <summary>Bỏ dấu chấm, dấu phẩy, khoảng trắng phân cách hàng nghìn (150.000 = 150000).</summary>
    private static decimal? DocSoNguyen(string? s)
    {
        var chuoi = new string((s ?? string.Empty).Where(c => c is not ('.' or ',' or ' ' or ' ')).ToArray());
        return chuoi.Length > 0 && chuoi.All(c => c is >= '0' and <= '9')
               && decimal.TryParse(chuoi, NumberStyles.None, CultureInfo.InvariantCulture, out var so)
            ? so
            : null;
    }
}
