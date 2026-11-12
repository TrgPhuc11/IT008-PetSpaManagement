using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpaThuCung.Services;

namespace SpaThuCung.ViewModels;

/// <summary>Một dòng trong danh sách bản sao lưu.</summary>
public sealed record BanSaoLuuMucViewModel(BanSaoLuuDto Ban)
{
    private static readonly CultureInfo VanHoa = CultureInfo.GetCultureInfo("vi-VN");

    public string ThoiGianHienThi => Ban.ThoiGian.ToString("dd/MM/yyyy HH:mm", VanHoa);

    public string NguoiHienThi => string.IsNullOrWhiteSpace(Ban.NguoiSaoLuu) ? "Không rõ" : Ban.NguoiSaoLuu;

    public string SoAnhHienThi => Ban.SoAnh is int n ? n.ToString("N0", VanHoa) : "Không có";

    public string DungLuongHienThi => DinhDangDungLuong(Ban.DungLuong);

    public string TenThuMuc => System.IO.Path.GetFileName(Ban.ThuMuc.TrimEnd('\\', '/'));

    public static string DinhDangDungLuong(long b) => b switch
    {
        >= 1L << 30 => (b / (double)(1L << 30)).ToString("0.0", VanHoa) + " GB",
        >= 1L << 20 => (b / (double)(1L << 20)).ToString("0.0", VanHoa) + " MB",
        >= 1L << 10 => (b / (double)(1L << 10)).ToString("0", VanHoa) + " KB",
        _ => b.ToString(VanHoa) + " B",
    };
}

/// <summary>
/// Màn Sao lưu và phục hồi dữ liệu (UC20, NFR07, chỉ Quản lý). Sao lưu: chọn nơi lưu (nên là ổ đĩa ngoài) rồi bấm
/// Sao lưu ngay. Phục hồi: chọn một bản trong danh sách hoặc một tệp .bak, đọc cảnh báo ghi đè, xác nhận; xong thì
/// đăng nhập lại.
/// </summary>
public partial class SaoLuuViewModel : ObservableObject, ICoTaiDuLieu
{
    private static readonly CultureInfo VanHoa = CultureInfo.GetCultureInfo("vi-VN");

    private readonly ISaoLuuService _saoLuu;
    private readonly IHopThoaiService _hopThoai;
    private readonly IPhienLamViec _phien;
    private readonly IDieuHuongCuaSo _dieuHuong;

    public SaoLuuViewModel(ISaoLuuService saoLuu, IHopThoaiService hopThoai, IPhienLamViec phien, IDieuHuongCuaSo dieuHuong)
    {
        _saoLuu = saoLuu;
        _hopThoai = hopThoai;
        _phien = phien;
        _dieuHuong = dieuHuong;
    }

    public async Task TaiDuLieuAsync()
    {
        var caiDat = _saoLuu.DocCaiDat();
        NoiLuu = caiDat.NoiLuu ?? string.Empty;
        CapNhatLanGanNhat(caiDat);
        await TaiDanhSachAsync();
    }

    // =====================================================================
    // Thông báo, trạng thái
    // =====================================================================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoThongBao))]
    private string _thongBao = string.Empty;

    [ObservableProperty]
    private LoaiThongBao _loaiThongBao;

    public bool CoThongBao => !string.IsNullOrEmpty(ThongBao);

    private void HienThongBao(string noiDung, LoaiThongBao loai)
    {
        LoaiThongBao = loai;
        ThongBao = noiDung;
    }

    /// <summary>Đang sao lưu hoặc phục hồi: khoá mọi nút cho tới khi xong.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KhongDangXuLy))]
    [NotifyCanExecuteChangedFor(nameof(ChonNoiLuuCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaoLuuCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChonTepPhucHoiCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChonBanCommand))]
    [NotifyCanExecuteChangedFor(nameof(PhucHoiCommand))]
    [NotifyCanExecuteChangedFor(nameof(TaiLaiDanhSachCommand))]
    private bool _dangXuLy;

    public bool KhongDangXuLy => !DangXuLy;

    [ObservableProperty]
    private string _chuDangXuLy = string.Empty;

    private bool CoTheThaoTac() => !DangXuLy;

    // =====================================================================
    // Sao lưu
    // =====================================================================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoNoiLuu))]
    private string _noiLuu = string.Empty;

    public bool CoNoiLuu => !string.IsNullOrWhiteSpace(NoiLuu);

    [ObservableProperty]
    private string _lanSaoLuuGanNhat = string.Empty;

    private void CapNhatLanGanNhat(CaiDatSaoLuuDto caiDat)
    {
        LanSaoLuuGanNhat = caiDat.LanGanNhat is DateTime luc
            ? $"Lần sao lưu gần nhất trên máy này: {luc.ToString("dd/MM/yyyy HH:mm", VanHoa)} ({SoNgayTruoc(luc)}), tại {caiDat.ThuMucGanNhat}."
            : "Chưa sao lưu lần nào trên máy này.";
    }

    private static string SoNgayTruoc(DateTime luc)
    {
        var soNgay = (DateTime.Today - luc.Date).Days;
        return soNgay switch
        {
            <= 0 => "hôm nay",
            1 => "hôm qua",
            _ => $"{soNgay} ngày trước",
        };
    }

    [RelayCommand(CanExecute = nameof(CoTheThaoTac))]
    private async Task ChonNoiLuu()
    {
        var chon = _hopThoai.ChonThuMuc("Chọn thư mục hoặc ổ đĩa ngoài để lưu bản sao lưu", CoNoiLuu ? NoiLuu : null);
        if (chon is null) return;

        NoiLuu = chon;
        _saoLuu.GhiNhoNoiLuu(chon);
        await TaiDanhSachAsync();
    }

    [RelayCommand(CanExecute = nameof(CoTheThaoTac))]
    private async Task SaoLuu()
    {
        if (!CoNoiLuu)
        {
            HienThongBao("Chọn nơi lưu trước (nên là ổ đĩa ngoài hoặc USB, để máy hỏng vẫn còn dữ liệu).", LoaiThongBao.Loi);
            return;
        }

        DangXuLy = true;
        ChuDangXuLy = "Đang sao lưu cơ sở dữ liệu và thư mục ảnh…";
        ThongBao = string.Empty;
        try
        {
            var kq = await _saoLuu.SaoLuuAsync(NoiLuu);
            if (!kq.ThanhCong)
            {
                HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
                return;
            }

            CapNhatLanGanNhat(_saoLuu.DocCaiDat());
            await TaiDanhSachAsync();
            HienThongBao(kq.ThongBao, LoaiThongBao.ThanhCong);
        }
        finally
        {
            DangXuLy = false;
        }
    }

    // =====================================================================
    // Danh sách bản sao lưu ở nơi lưu
    // =====================================================================

    public ObservableCollection<BanSaoLuuMucViewModel> DanhSachBan { get; } = new();

    [ObservableProperty]
    private string _tomTatDanhSach = string.Empty;

    [ObservableProperty]
    private bool _coBanNao;

    [RelayCommand(CanExecute = nameof(CoTheThaoTac))]
    private Task TaiLaiDanhSach() => TaiDanhSachAsync();

    private async Task TaiDanhSachAsync()
    {
        DanhSachBan.Clear();
        if (!CoNoiLuu)
        {
            CoBanNao = false;
            TomTatDanhSach = "Chọn nơi lưu để xem các bản sao lưu có ở đó.";
            return;
        }

        var kq = await _saoLuu.LayDanhSachBanSaoLuuAsync(NoiLuu);
        if (!kq.ThanhCong)
        {
            CoBanNao = false;
            TomTatDanhSach = kq.ThongBao;
            return;
        }

        foreach (var ban in kq.DuLieu!)
            DanhSachBan.Add(new BanSaoLuuMucViewModel(ban));

        CoBanNao = DanhSachBan.Count > 0;
        TomTatDanhSach = CoBanNao
            ? $"{DanhSachBan.Count} bản sao lưu trong {NoiLuu}, mới nhất ở trên."
            : $"Chưa có bản sao lưu nào trong {NoiLuu}.";
    }

    // =====================================================================
    // Phục hồi
    // =====================================================================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoBanDaChon))]
    [NotifyPropertyChangedFor(nameof(ThongTinBanDaChon))]
    private BanSaoLuuMucViewModel? _banDaChon;

    public bool CoBanDaChon => BanDaChon is not null;

    public string ThongTinBanDaChon => BanDaChon is null
        ? "Chưa chọn bản sao lưu. Chọn một bản trong danh sách bên dưới hoặc chọn tệp .bak."
        : $"Bản sao lưu lúc {BanDaChon.ThoiGianHienThi} · người sao lưu: {BanDaChon.NguoiHienThi} · " +
          $"ảnh: {BanDaChon.SoAnhHienThi} · {BanDaChon.DungLuongHienThi}\n{BanDaChon.Ban.TepCsdl}";

    [RelayCommand(CanExecute = nameof(CoTheThaoTac))]
    private void ChonBan(BanSaoLuuMucViewModel? ban)
    {
        if (ban is null) return;
        BanDaChon = ban;
        ThongBao = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CoTheThaoTac))]
    private void ChonTepPhucHoi()
    {
        var tep = _hopThoai.ChonTep("Chọn tệp sao lưu cần phục hồi", "Tệp sao lưu (*.bak)|*.bak",
            BanDaChon?.Ban.ThuMuc ?? (CoNoiLuu ? NoiLuu : null));
        if (tep is null) return;

        var kq = _saoLuu.DocBanSaoLuu(tep);
        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return;
        }

        BanDaChon = new BanSaoLuuMucViewModel(kq.DuLieu!);
        ThongBao = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CoTheThaoTac))]
    private async Task PhucHoi()
    {
        if (BanDaChon is null)
        {
            HienThongBao("Chọn bản sao lưu cần phục hồi.", LoaiThongBao.Loi);
            return;
        }

        var ban = BanDaChon;
        var xacNhan = _hopThoai.XacNhan(
            $"Phục hồi bản sao lưu lúc {ban.ThoiGianHienThi}?\n\n" +
            "Toàn bộ dữ liệu hiện tại (khách hàng, thú cưng, phiếu dịch vụ, lịch làm việc, nhân viên, bảng giá, cấu hình) " +
            "và thư mục ảnh sẽ bị GHI ĐÈ bằng dữ liệu trong bản sao lưu. Những gì nhập sau thời điểm sao lưu sẽ mất.\n\n" +
            "Hệ thống tự giữ lại một bản dữ liệu hiện tại trước khi ghi đè. Phục hồi xong, bạn cần đăng nhập lại.",
            "Xác nhận phục hồi dữ liệu");
        if (!xacNhan) return;

        DangXuLy = true;
        ChuDangXuLy = "Đang phục hồi dữ liệu, đừng tắt ứng dụng…";
        ThongBao = string.Empty;
        KetQua kq;
        try
        {
            kq = await _saoLuu.PhucHoiAsync(ban.Ban.TepCsdl);
        }
        finally
        {
            DangXuLy = false;
        }

        if (!kq.ThanhCong)
        {
            HienThongBao(kq.ThongBao, LoaiThongBao.Loi);
            return;
        }

        // Tài khoản, mật khẩu có thể đã khác theo bản sao lưu: bắt đăng nhập lại.
        _hopThoai.ThongBao(kq.ThongBao + "\n\nHãy đăng nhập lại.", "Phục hồi xong");
        _phien.DangXuat();
        _dieuHuong.VeDangNhap();
    }
}
