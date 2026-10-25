using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SpaThuCung.Data;

/// <summary>
/// Tự tạo CSDL + bảng + dữ liệu mẫu bằng script SQL nhúng trong assembly.
/// Script idempotent nên chạy mỗi lần khởi động đều an toàn.
/// </summary>
public class KhoiTaoCsdl
{
    private const string TenResource = "SpaThuCung.Sql.TaoCsdl";

    private readonly string _connectionString;

    public KhoiTaoCsdl(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>Đã tạo/kiểm tra CSDL thành công trong lần chạy này chưa.</summary>
    public bool DaXong { get; private set; }

    public async Task ChayAsync()
    {
        if (DaXong) return;

        // Kết nối vào master; lệnh USE trong script sẽ chuyển sang CSDL SpaThuCung.
        // Timeout ngắn để sai chuỗi kết nối thì báo lỗi nhanh, không treo lâu.
        var builder = new SqlConnectionStringBuilder(_connectionString)
        {
            InitialCatalog = "master",
            ConnectTimeout = 5
        };

        var cacLo = DocCacLo();

        await using var conn = new SqlConnection(builder.ConnectionString);
        await conn.OpenAsync();

        foreach (var lo in cacLo)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = lo;
            cmd.CommandTimeout = 120;
            await cmd.ExecuteNonQueryAsync();
        }

        DaXong = true;
    }

    /// <summary>Đọc script nhúng và tách thành các lô theo dòng GO.</summary>
    private static List<string> DocCacLo()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TenResource)
            ?? throw new InvalidOperationException($"Không tìm thấy script nhúng '{TenResource}'.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var noiDung = reader.ReadToEnd();

        return Regex.Split(noiDung, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
    }
}
