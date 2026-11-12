using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using SpaThuCung.Data;

namespace SpaThuCung.Services;

public class SaoLuuService : ISaoLuuService
{
    public const string TenTepCsdl = "SpaThuCung.bak";
    public const string TenThuMucAnh = "HinhAnh";
    public const string TenTepThongTin = "SaoLuu.json";
    public const string TienToThuMuc = "SpaThuCung_SaoLuu_";

    private const string TenTepTruocPhucHoi = "TruocPhucHoi.bak";

    private const string LoiQuyen = "Chỉ Quản lý được sao lưu và phục hồi dữ liệu.";
    private const string LoiKetNoi = "Không kết nối được SQL Server. Kiểm tra SQL Server đang chạy rồi thử lại.";
    private const string LoiTepKhongHopLe =
        "Tệp đã chọn không phải bản sao lưu hợp lệ (hỏng hoặc không phải tệp sao lưu SQL Server). Dữ liệu hiện tại không thay đổi.";

    private static readonly JsonSerializerOptions TuyChonJson = new() { WriteIndented = true };

    private readonly KhoiTaoCsdl _csdl;
    private readonly ICauHinhService _cauHinh;
    private readonly IPhienLamViec _phien;

    public SaoLuuService(KhoiTaoCsdl csdl, ICauHinhService cauHinh, IPhienLamViec phien)
    {
        _csdl = csdl;
        _cauHinh = cauHinh;
        _phien = phien;
    }

    /// <summary>Tên CSDL trong chuỗi kết nối (mặc định SpaThuCung).</summary>
    private string TenCsdl
    {
        get
        {
            var ten = new SqlConnectionStringBuilder(_csdl.ConnectionString).InitialCatalog;
            return string.IsNullOrWhiteSpace(ten) ? "SpaThuCung" : ten;
        }
    }

    /// <summary>Kết nối vào master (không vào CSDL đang phục hồi), không dùng pool để không giữ kết nối thừa.</summary>
    private string ChuoiKetNoiMaster => new SqlConnectionStringBuilder(_csdl.ConnectionString)
    {
        InitialCatalog = "master",
        ConnectTimeout = 10,
        Pooling = false,
    }.ConnectionString;

    private static string TepCaiDat => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpaThuCung", "SaoLuu.json");

    /// <summary>
    /// Thư mục trung gian mà cả ứng dụng lẫn dịch vụ SQL Server đều đọc ghi được. SQL Server chạy bằng tài khoản dịch vụ
    /// riêng nên thường không ghi thẳng được vào USB hay thư mục của người dùng: sao lưu ra đây rồi ứng dụng chép đi.
    /// </summary>
    private static string ThuMucTrungGian => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SpaThuCung", "SaoLuuTam");

    private string NguoiDangDung => _phien.NhanVienHienTai?.HoTen ?? string.Empty;

    // =====================================================================
    // Ghi nhớ trên máy
    // =====================================================================

    public CaiDatSaoLuuDto DocCaiDat()
    {
        try
        {
            if (File.Exists(TepCaiDat))
                return JsonSerializer.Deserialize<CaiDatSaoLuuDto>(File.ReadAllText(TepCaiDat))
                       ?? new CaiDatSaoLuuDto(null, null, null);
        }
        catch (Exception)
        {
            // Tệp ghi nhớ hỏng thì coi như chưa có.
        }
        return new CaiDatSaoLuuDto(null, null, null);
    }

    public void GhiNhoNoiLuu(string noiLuu)
    {
        if (string.IsNullOrWhiteSpace(noiLuu)) return;
        LuuCaiDat(DocCaiDat() with { NoiLuu = noiLuu.Trim() });
    }

    private static void LuuCaiDat(CaiDatSaoLuuDto caiDat)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TepCaiDat)!);
            File.WriteAllText(TepCaiDat, JsonSerializer.Serialize(caiDat, TuyChonJson));
        }
        catch (Exception)
        {
            // Chỉ là ghi nhớ cho tiện, lỗi thì bỏ qua.
        }
    }

    // =====================================================================
    // Sao lưu
    // =====================================================================

    public async Task<KetQua<BanSaoLuuDto>> SaoLuuAsync(string noiLuu)
    {
        if (!_phien.LaQuanLy) return KetQua<BanSaoLuuDto>.Loi(LoiQuyen);
        if (string.IsNullOrWhiteSpace(noiLuu))
            return KetQua<BanSaoLuuDto>.Loi("Chọn thư mục hoặc ổ đĩa để lưu bản sao lưu.");

        noiLuu = noiLuu.Trim();
        if (!Directory.Exists(noiLuu))
            return KetQua<BanSaoLuuDto>.Loi("Không tìm thấy nơi lưu. Kiểm tra ổ đĩa ngoài đã cắm vào máy chưa.");

        var kqAnh = await _cauHinh.LayThuMucAnhAsync();
        if (!kqAnh.ThanhCong) return KetQua<BanSaoLuuDto>.Loi(kqAnh.ThongBao);
        var thuMucAnh = kqAnh.DuLieu!;

        var thoiGian = DateTime.Now;
        var thuMucBan = Path.Combine(noiLuu, TienToThuMuc + thoiGian.ToString("yyyyMMdd_HHmmss"));
        for (var i = 2; Directory.Exists(thuMucBan); i++)
            thuMucBan = Path.Combine(noiLuu, TienToThuMuc + thoiGian.ToString("yyyyMMdd_HHmmss") + "_" + i);

        // Ảnh nằm ngay trong nơi lưu thì chép sẽ lặp vô tận.
        if (LaThuMucCon(thuMucBan, thuMucAnh))
            return KetQua<BanSaoLuuDto>.Loi("Nơi lưu đang nằm trong thư mục ảnh. Chọn thư mục khác, tốt nhất là ổ đĩa ngoài.");

        try
        {
            Directory.CreateDirectory(thuMucBan);
        }
        catch (Exception ex)
        {
            return KetQua<BanSaoLuuDto>.Loi("Không ghi được vào nơi lưu: " + ex.Message);
        }

        string? tepTam = null;
        try
        {
            tepTam = Path.Combine(ChuanBiThuMucTrungGian(), $"SaoLuu_{Guid.NewGuid():N}.bak");

            await using (var conn = new SqlConnection(ChuoiKetNoiMaster))
            {
                await conn.OpenAsync();
                await SaoLuuCsdlAsync(conn, TenCsdl, tepTam, "Spa thu cung - sao luu " + thoiGian.ToString("dd/MM/yyyy HH:mm"));
            }

            var tepDich = Path.Combine(thuMucBan, TenTepCsdl);
            var thuMucAnhDich = Path.Combine(thuMucBan, TenThuMucAnh);
            var soAnh = await Task.Run(() =>
            {
                File.Copy(tepTam, tepDich);
                // Luôn tạo thư mục ảnh (kể cả rỗng) để lúc phục hồi biết bản này có ảnh đi kèm.
                Directory.CreateDirectory(thuMucAnhDich);
                return Directory.Exists(thuMucAnh) ? SaoChepThuMuc(thuMucAnh, thuMucAnhDich) : 0;
            });

            var thongTin = new ThongTinTep(thoiGian, NguoiDangDung, TenCsdl, soAnh);
            await File.WriteAllTextAsync(Path.Combine(thuMucBan, TenTepThongTin), JsonSerializer.Serialize(thongTin, TuyChonJson));

            LuuCaiDat(new CaiDatSaoLuuDto(noiLuu, thoiGian, thuMucBan));

            var ban = new BanSaoLuuDto(thuMucBan, tepDich, thoiGian, NguoiDangDung, soAnh, new FileInfo(tepDich).Length);
            return KetQua<BanSaoLuuDto>.Ok(ban,
                $"Đã sao lưu dữ liệu và {soAnh} ảnh vào {thuMucBan}.");
        }
        catch (SqlException ex)
        {
            XoaThuMuc(thuMucBan);
            return KetQua<BanSaoLuuDto>.Loi(MoTaLoiSql(ex, "sao lưu"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            XoaThuMuc(thuMucBan);
            return KetQua<BanSaoLuuDto>.Loi("Không ghi được vào nơi lưu (ổ đầy, bị rút ra hoặc không có quyền ghi): " + ex.Message);
        }
        finally
        {
            XoaTep(tepTam);
        }
    }

    // =====================================================================
    // Danh sách bản sao lưu
    // =====================================================================

    public Task<KetQua<List<BanSaoLuuDto>>> LayDanhSachBanSaoLuuAsync(string noiLuu)
    {
        if (!_phien.LaQuanLy) return Task.FromResult(KetQua<List<BanSaoLuuDto>>.Loi(LoiQuyen));
        if (string.IsNullOrWhiteSpace(noiLuu) || !Directory.Exists(noiLuu))
            return Task.FromResult(KetQua<List<BanSaoLuuDto>>.Ok(new List<BanSaoLuuDto>()));

        return Task.Run(() =>
        {
            try
            {
                var ds = new List<BanSaoLuuDto>();
                var cacThuMuc = new List<string> { noiLuu.Trim() };
                cacThuMuc.AddRange(Directory.EnumerateDirectories(noiLuu.Trim()));

                foreach (var thuMuc in cacThuMuc)
                {
                    var tep = Path.Combine(thuMuc, TenTepCsdl);
                    if (File.Exists(tep)) ds.Add(DocThongTinBan(tep));
                }

                return KetQua<List<BanSaoLuuDto>>.Ok(ds.OrderByDescending(x => x.ThoiGian).ToList());
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return KetQua<List<BanSaoLuuDto>>.Loi("Không đọc được nơi lưu: " + ex.Message);
            }
        });
    }

    public KetQua<BanSaoLuuDto> DocBanSaoLuu(string tepCsdl)
    {
        if (!_phien.LaQuanLy) return KetQua<BanSaoLuuDto>.Loi(LoiQuyen);
        if (string.IsNullOrWhiteSpace(tepCsdl) || !File.Exists(tepCsdl))
            return KetQua<BanSaoLuuDto>.Loi("Không tìm thấy tệp sao lưu.");

        try
        {
            return KetQua<BanSaoLuuDto>.Ok(DocThongTinBan(tepCsdl));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return KetQua<BanSaoLuuDto>.Loi("Không đọc được tệp sao lưu: " + ex.Message);
        }
    }

    /// <summary>Đọc SaoLuu.json cạnh tệp .bak; không có (bản chép tay, bản cũ) thì lấy giờ sửa tệp.</summary>
    private static BanSaoLuuDto DocThongTinBan(string tepCsdl)
    {
        var fi = new FileInfo(tepCsdl);
        var thuMuc = fi.DirectoryName!;
        var thuMucAnh = Path.Combine(thuMuc, TenThuMucAnh);
        int? soAnh = Directory.Exists(thuMucAnh)
            ? Directory.EnumerateFiles(thuMucAnh, "*", SearchOption.AllDirectories).Count()
            : null;

        ThongTinTep? thongTin = null;
        try
        {
            var tepJson = Path.Combine(thuMuc, TenTepThongTin);
            if (File.Exists(tepJson))
                thongTin = JsonSerializer.Deserialize<ThongTinTep>(File.ReadAllText(tepJson));
        }
        catch (Exception)
        {
            // Tệp thông tin hỏng thì bỏ qua, tệp .bak vẫn dùng được.
        }

        return new BanSaoLuuDto(thuMuc, fi.FullName, thongTin?.ThoiGian ?? fi.LastWriteTime,
            thongTin?.NguoiSaoLuu ?? string.Empty, soAnh, fi.Length);
    }

    // =====================================================================
    // Phục hồi
    // =====================================================================

    public async Task<KetQua> PhucHoiAsync(string tepCsdl)
    {
        if (!_phien.LaQuanLy) return KetQua.Loi(LoiQuyen);
        if (string.IsNullOrWhiteSpace(tepCsdl) || !File.Exists(tepCsdl))
            return KetQua.Loi("Không tìm thấy tệp sao lưu.");

        var tenCsdl = TenCsdl;
        var thuMucBan = Path.GetDirectoryName(Path.GetFullPath(tepCsdl))!;

        // Chép tệp sao lưu sang thư mục trung gian để SQL Server đọc được (tệp có thể nằm trên USB).
        string thuMucTam;
        string tepTam;
        try
        {
            thuMucTam = ChuanBiThuMucTrungGian();
            tepTam = Path.Combine(thuMucTam, $"PhucHoi_{Guid.NewGuid():N}.bak");
            await Task.Run(() => File.Copy(tepCsdl, tepTam));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return KetQua.Loi("Không đọc được tệp sao lưu: " + ex.Message);
        }

        var tepTruoc = Path.Combine(thuMucTam, TenTepTruocPhucHoi);
        DateTime? lucSaoLuu = null;
        var coBanTruoc = false;
        try
        {
            await using var conn = new SqlConnection(ChuoiKetNoiMaster);
            await conn.OpenAsync();

            // 1. Kiểm tra tệp: đọc được, nguyên vẹn, đúng là CSDL của hệ thống.
            string tenTrongBan;
            List<TepCsdl> tepTrongBan;
            try
            {
                (tenTrongBan, lucSaoLuu) = await DocDauBanAsync(conn, tepTam);
                await ChayAsync(conn, "RESTORE VERIFYONLY FROM DISK = @tep", ("@tep", tepTam));
                tepTrongBan = await DocDanhSachTepTrongBanAsync(conn, tepTam);
            }
            catch (Exception ex) when (ex is SqlException or InvalidDataException)
            {
                if (ex is SqlException sqlEx)
                {
                    if (CoMaLoi(sqlEx, 3169))
                        return KetQua.Loi("Bản sao lưu được tạo từ phiên bản SQL Server mới hơn máy này nên không phục hồi được. Dữ liệu hiện tại không thay đổi.");
                    if (CoMaLoi(sqlEx, 3201) || LaLoiKetNoi(sqlEx))
                        return KetQua.Loi(MoTaLoiSql(sqlEx, "phục hồi"));
                }
                return KetQua.Loi(LoiTepKhongHopLe);
            }

            if (!string.Equals(tenTrongBan, tenCsdl, StringComparison.OrdinalIgnoreCase))
                return KetQua.Loi($"Tệp này là bản sao lưu của cơ sở dữ liệu \"{tenTrongBan}\", không phải của hệ thống spa. Dữ liệu hiện tại không thay đổi.");

            var tepHienTai = await DocTepHienTaiAsync(conn, tenCsdl);
            var coCsdl = tepHienTai.Count > 0;
            var dsMove = await LapDanhSachMoveAsync(conn, tenCsdl, tepTrongBan, tepHienTai);

            // 2. Giữ lại dữ liệu hiện tại để quay lại nếu phục hồi lỗi.
            if (coCsdl)
            {
                XoaTep(tepTruoc);
                await SaoLuuCsdlAsync(conn, tenCsdl, tepTruoc, "Spa thu cung - truoc khi phuc hoi");
                coBanTruoc = true;
            }

            // 3. Ngắt mọi kết nối vào CSDL (kể cả của chính ứng dụng) rồi ghi đè.
            SqlConnection.ClearAllPools();
            var daGhiDe = false;
            try
            {
                if (coCsdl) await DatCheDoAsync(conn, tenCsdl, "SINGLE_USER WITH ROLLBACK IMMEDIATE");
                daGhiDe = true;
                await PhucHoiCsdlAsync(conn, tenCsdl, tepTam, dsMove);
            }
            catch (SqlException ex)
            {
                var lyDo = MoTaLoiSql(ex, "phục hồi");
                if (!coCsdl) return KetQua.Loi(lyDo);
                if (!daGhiDe) return KetQua.Loi(lyDo + " Dữ liệu hiện tại không thay đổi.");

                try
                {
                    var dsMoveCu = tepHienTai.Select(t => (t.TenLogic, t.DuongDan)).ToList();
                    await PhucHoiCsdlAsync(conn, tenCsdl, tepTruoc, dsMoveCu);
                    return KetQua.Loi(lyDo + " Dữ liệu hiện tại đã được giữ nguyên.");
                }
                catch (SqlException)
                {
                    return KetQua.Loi(lyDo + $" Không tự quay lại được; dữ liệu ngay trước khi phục hồi nằm ở {tepTruoc}, hãy giữ tệp này và nhờ hỗ trợ.");
                }
            }
            finally
            {
                try
                {
                    await DatCheDoAsync(conn, tenCsdl, "MULTI_USER");
                }
                catch (SqlException)
                {
                    // CSDL vừa phục hồi đã ở chế độ nhiều người dùng.
                }
                SqlConnection.ClearAllPools();
            }
        }
        catch (SqlException ex)
        {
            return KetQua.Loi(MoTaLoiSql(ex, "phục hồi"));
        }
        finally
        {
            XoaTep(tepTam);
        }

        // Bản sao lưu cũ có thể thiếu bảng mới: lần đăng nhập sau chạy lại script tạo bảng.
        _csdl.DatLai();

        // 4. Thư mục ảnh: đọc lại đường dẫn từ cấu hình vừa phục hồi.
        var thongBaoAnh = await PhucHoiAnhAsync(thuMucBan);

        var luc = lucSaoLuu.HasValue ? $" lúc {lucSaoLuu.Value.ToLocalTime():dd/MM/yyyy HH:mm}" : string.Empty;
        var giuLai = coBanTruoc
            ? $" Dữ liệu ngay trước khi phục hồi được giữ ở {tepTruoc} (bị thay ở lần phục hồi sau)."
            : string.Empty;
        return KetQua.Ok($"Đã phục hồi dữ liệu từ bản sao lưu{luc}. {thongBaoAnh}{giuLai}");
    }

    private async Task<string> PhucHoiAnhAsync(string thuMucBan)
    {
        var nguon = Path.Combine(thuMucBan, TenThuMucAnh);
        if (!Directory.Exists(nguon))
            return "Bản sao lưu không có thư mục ảnh nên ảnh hiện tại được giữ nguyên.";

        var kq = await _cauHinh.LayThuMucAnhAsync();
        if (!kq.ThanhCong) return "Chưa chép được ảnh: " + kq.ThongBao;

        try
        {
            var soAnh = await Task.Run(() => ThayThuMucAnh(nguon, kq.DuLieu!));
            return $"Đã chép {soAnh} ảnh vào {kq.DuLieu}.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "Chưa chép được ảnh (ảnh hiện tại giữ nguyên): " + ex.Message;
        }
    }

    /// <summary>Chép ảnh sang thư mục tạm cạnh thư mục ảnh rồi mới đổi tên, để lỗi giữa chừng không làm mất ảnh hiện tại.</summary>
    private static int ThayThuMucAnh(string nguon, string dich)
    {
        var dichDayDu = Path.GetFullPath(dich).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var tam = dichDayDu + ".dangphuchoi";
        var cu = dichDayDu + ".cu";
        XoaThuMuc(tam);
        XoaThuMuc(cu);

        int soAnh;
        try
        {
            Directory.CreateDirectory(tam);
            soAnh = SaoChepThuMuc(nguon, tam);
        }
        catch
        {
            XoaThuMuc(tam);
            throw;
        }

        if (Directory.Exists(dichDayDu)) Directory.Move(dichDayDu, cu);
        Directory.Move(tam, dichDayDu);
        XoaThuMuc(cu);
        return soAnh;
    }

    // =====================================================================
    // Lệnh SQL Server
    // =====================================================================

    private sealed record TepCsdl(string TenLogic, string DuongDan, char Loai);

    private static Task SaoLuuCsdlAsync(SqlConnection conn, string tenCsdl, string tep, string nhan) =>
        ChayAsync(conn,
            "BACKUP DATABASE @ten TO DISK = @tep WITH COPY_ONLY, INIT, FORMAT, CHECKSUM, NAME = @nhan",
            ("@ten", tenCsdl), ("@tep", tep), ("@nhan", nhan));

    private static Task PhucHoiCsdlAsync(SqlConnection conn, string tenCsdl, string tep,
        IReadOnlyList<(string TenLogic, string DuongDan)> dsMove)
    {
        var lenh = "RESTORE DATABASE @ten FROM DISK = @tep WITH REPLACE, RECOVERY";
        var thamSo = new List<(string, object)> { ("@ten", tenCsdl), ("@tep", tep) };
        for (var i = 0; i < dsMove.Count; i++)
        {
            lenh += $", MOVE @l{i} TO @p{i}";
            thamSo.Add(($"@l{i}", dsMove[i].TenLogic));
            thamSo.Add(($"@p{i}", dsMove[i].DuongDan));
        }
        return ChayAsync(conn, lenh, thamSo.ToArray());
    }

    /// <summary>ALTER DATABASE không nhận tham số nên ghép tên qua QUOTENAME; chế độ là hằng trong code.</summary>
    private static Task DatCheDoAsync(SqlConnection conn, string tenCsdl, string cheDo) =>
        ChayAsync(conn,
            $"IF DB_ID(@ten) IS NOT NULL BEGIN DECLARE @sql NVARCHAR(MAX) = N'ALTER DATABASE ' + QUOTENAME(@ten) + N' SET {cheDo}'; EXEC (@sql); END",
            ("@ten", tenCsdl));

    private static async Task<(string TenCsdl, DateTime? LucSaoLuu)> DocDauBanAsync(SqlConnection conn, string tep)
    {
        await using var cmd = TaoLenh(conn, "RESTORE HEADERONLY FROM DISK = @tep", ("@tep", tep));
        await using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync()) throw new InvalidDataException();

        var ten = rd.GetString(rd.GetOrdinal("DatabaseName"));
        var cot = rd.GetOrdinal("BackupFinishDate");
        DateTime? luc = rd.IsDBNull(cot) ? null : DateTime.SpecifyKind(rd.GetDateTime(cot), DateTimeKind.Local);
        return (ten, luc);
    }

    private static async Task<List<TepCsdl>> DocDanhSachTepTrongBanAsync(SqlConnection conn, string tep)
    {
        var ds = new List<TepCsdl>();
        await using var cmd = TaoLenh(conn, "RESTORE FILELISTONLY FROM DISK = @tep", ("@tep", tep));
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            var loai = rd.GetString(rd.GetOrdinal("Type"));
            ds.Add(new TepCsdl(
                rd.GetString(rd.GetOrdinal("LogicalName")),
                rd.GetString(rd.GetOrdinal("PhysicalName")),
                string.IsNullOrEmpty(loai) ? 'D' : char.ToUpperInvariant(loai[0])));
        }
        return ds;
    }

    private static async Task<List<TepCsdl>> DocTepHienTaiAsync(SqlConnection conn, string tenCsdl)
    {
        var ds = new List<TepCsdl>();
        await using var cmd = TaoLenh(conn,
            "SELECT name, physical_name, type FROM sys.master_files WHERE database_id = DB_ID(@ten) ORDER BY file_id",
            ("@ten", tenCsdl));
        await using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
            ds.Add(new TepCsdl(rd.GetString(0), rd.GetString(1), rd.GetByte(2) == 1 ? 'L' : 'D'));
        return ds;
    }

    /// <summary>
    /// Mỗi tệp trong bản sao lưu phục hồi vào đúng tệp hiện tại cùng loại (ưu tiên cùng tên logic), để bản sao lưu
    /// lấy từ máy khác (đường dẫn khác) vẫn phục hồi được. Không có tệp tương ứng thì đặt cạnh tệp dữ liệu.
    /// </summary>
    private static async Task<List<(string TenLogic, string DuongDan)>> LapDanhSachMoveAsync(
        SqlConnection conn, string tenCsdl, List<TepCsdl> tepTrongBan, List<TepCsdl> tepHienTai)
    {
        string? thuMucMacDinh = null;
        async Task<string> LayThuMucMacDinhAsync()
        {
            if (thuMucMacDinh is not null) return thuMucMacDinh;
            await using var cmd = TaoLenh(conn,
                "SELECT COALESCE(CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(260)), " +
                "(SELECT physical_name FROM sys.master_files WHERE database_id = 1 AND file_id = 1))");
            var kq = (string)(await cmd.ExecuteScalarAsync())!;
            thuMucMacDinh = kq.EndsWith(".mdf", StringComparison.OrdinalIgnoreCase) ? Path.GetDirectoryName(kq)! : kq;
            return thuMucMacDinh;
        }

        var daDung = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ds = new List<(string, string)>();
        foreach (var b in tepTrongBan)
        {
            var hienTai = tepHienTai.FirstOrDefault(t => t.Loai == b.Loai && !daDung.Contains(t.DuongDan)
                                                         && string.Equals(t.TenLogic, b.TenLogic, StringComparison.OrdinalIgnoreCase))
                          ?? tepHienTai.FirstOrDefault(t => t.Loai == b.Loai && !daDung.Contains(t.DuongDan));

            string duongDan;
            if (hienTai is not null)
            {
                duongDan = hienTai.DuongDan;
            }
            else
            {
                var canh = tepHienTai.FirstOrDefault(t => t.Loai == b.Loai) ?? tepHienTai.FirstOrDefault();
                var thuMuc = canh is not null ? Path.GetDirectoryName(canh.DuongDan)! : await LayThuMucMacDinhAsync();
                var duoi = b.Loai == 'L' ? ".ldf" : (ds.Count == 0 ? ".mdf" : ".ndf");
                duongDan = Path.Combine(thuMuc, $"{tenCsdl}_{b.TenLogic}{duoi}");
            }

            daDung.Add(duongDan);
            ds.Add((b.TenLogic, duongDan));
        }
        return ds;
    }

    private static SqlCommand TaoLenh(SqlConnection conn, string lenh, params (string Ten, object GiaTri)[] thamSo)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = lenh;
        cmd.CommandTimeout = 0; // sao lưu/phục hồi CSDL lớn có thể lâu
        foreach (var (ten, giaTri) in thamSo)
            cmd.Parameters.AddWithValue(ten, giaTri);
        return cmd;
    }

    private static async Task ChayAsync(SqlConnection conn, string lenh, params (string Ten, object GiaTri)[] thamSo)
    {
        await using var cmd = TaoLenh(conn, lenh, thamSo);
        await cmd.ExecuteNonQueryAsync();
    }

    private static bool CoMaLoi(SqlException ex, params int[] cacMa) =>
        ex.Errors.Cast<SqlError>().Any(e => cacMa.Contains(e.Number));

    private static bool LaLoiKetNoi(SqlException ex) => CoMaLoi(ex, -2, -1, 2, 53, 4060, 18456);

    private static string MoTaLoiSql(SqlException ex, string viec)
    {
        if (LaLoiKetNoi(ex)) return LoiKetNoi;
        if (CoMaLoi(ex, 262, 229, 15247, 3110))
            return $"Tài khoản kết nối SQL Server không có quyền {viec} (cần quyền sysadmin hoặc dbcreator/db_backupoperator).";
        if (CoMaLoi(ex, 3201))
            return $"SQL Server không đọc/ghi được tệp tạm khi {viec}. Kiểm tra quyền thư mục {ThuMucTrungGian}.";
        if (CoMaLoi(ex, 3101))
            return "Cơ sở dữ liệu đang được mở ở nơi khác (ví dụ SSMS). Đóng lại rồi thử lại.";
        if (CoMaLoi(ex, 3257, 3202))
            return $"Ổ đĩa không đủ chỗ trống để {viec}.";
        return $"Không {viec} được: {ex.Message}";
    }

    // =====================================================================
    // Tệp và thư mục
    // =====================================================================

    /// <summary>Tạo thư mục trung gian và cho mọi tài khoản (gồm tài khoản dịch vụ SQL Server) đọc ghi được.</summary>
    private static string ChuanBiThuMucTrungGian()
    {
        var thuMuc = ThuMucTrungGian;
        var di = Directory.CreateDirectory(thuMuc);
        try
        {
            var acl = di.GetAccessControl();
            var moiNguoi = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            acl.AddAccessRule(new FileSystemAccessRule(moiNguoi, FileSystemRights.Modify,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None, AccessControlType.Allow));
            di.SetAccessControl(acl);
        }
        catch (Exception)
        {
            // Không đặt được quyền (ví dụ thư mục do tài khoản khác tạo): vẫn thử, lỗi sẽ báo ở bước SQL Server.
        }

        // Dọn tệp tạm còn sót từ lần trước (giữ TruocPhucHoi.bak).
        foreach (var tep in di.EnumerateFiles("*.bak"))
        {
            if (!tep.Name.Equals(TenTepTruocPhucHoi, StringComparison.OrdinalIgnoreCase))
                XoaTep(tep.FullName);
        }
        return thuMuc;
    }

    private static int SaoChepThuMuc(string nguon, string dich)
    {
        var soTep = 0;
        foreach (var thuMuc in Directory.EnumerateDirectories(nguon, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(dich, Path.GetRelativePath(nguon, thuMuc)));

        foreach (var tep in Directory.EnumerateFiles(nguon, "*", SearchOption.AllDirectories))
        {
            File.Copy(tep, Path.Combine(dich, Path.GetRelativePath(nguon, tep)), overwrite: true);
            soTep++;
        }
        return soTep;
    }

    private static bool LaThuMucCon(string con, string cha)
    {
        var a = Path.GetFullPath(con).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        var b = Path.GetFullPath(cha).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        return a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
    }

    private static void XoaTep(string? tep)
    {
        if (tep is null) return;
        try
        {
            if (File.Exists(tep)) File.Delete(tep);
        }
        catch (Exception)
        {
            // Tệp tạm, lần sau dọn tiếp.
        }
    }

    private static void XoaThuMuc(string thuMuc)
    {
        try
        {
            if (Directory.Exists(thuMuc)) Directory.Delete(thuMuc, recursive: true);
        }
        catch (Exception)
        {
            // Bỏ qua, không ảnh hưởng dữ liệu.
        }
    }

    /// <summary>Nội dung SaoLuu.json trong mỗi bản sao lưu.</summary>
    private sealed record ThongTinTep(DateTime ThoiGian, string NguoiSaoLuu, string TenCsdl, int SoAnh);
}
