using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SpaThuCung.Data;

/// <summary>
/// Tự tạo CSDL + bảng + dữ liệu mẫu bằng các script SQL nhúng trong assembly.
/// Các script chạy theo thứ tự tên (01_, 02_...); script idempotent nên chạy mỗi lần khởi động đều an toàn.
/// </summary>
public class KhoiTaoCsdl
{
    private const string TienToResource = "SpaThuCung.Sql.";

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

    /// <summary>Đọc các script nhúng (theo thứ tự tên) và tách thành các lô theo dòng GO.</summary>
    private static List<string> DocCacLo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var tenCacScript = assembly.GetManifestResourceNames()
            .Where(x => x.StartsWith(TienToResource, StringComparison.Ordinal))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        if (tenCacScript.Count == 0)
            throw new InvalidOperationException($"Không tìm thấy script nhúng '{TienToResource}*'.");

        var cacLo = new List<string>();
        foreach (var ten in tenCacScript)
        {
            using var stream = assembly.GetManifestResourceStream(ten)!;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var noiDung = reader.ReadToEnd();

            cacLo.AddRange(Regex.Split(noiDung, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0));
        }
        return cacLo;
    }
}
