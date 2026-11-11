using System.Globalization;
using System.Text;

namespace SpaThuCung.ViewModels;

/// <summary>So khớp chuỗi tìm kiếm không phân biệt hoa thường và dấu tiếng Việt.</summary>
public static class TimKiemChu
{
    public static string BoDau(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        var chuan = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(chuan.Length);
        foreach (var c in chuan)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(c == 'đ' ? 'd' : c);
        }
        return sb.ToString();
    }

    /// <summary>tuKhoaDaBoDau rỗng thì luôn khớp; ngược lại khớp khi một trong các trường chứa nó.</summary>
    public static bool Khop(string tuKhoaDaBoDau, params string?[] cacTruong) =>
        tuKhoaDaBoDau.Length == 0 || cacTruong.Any(x => BoDau(x).Contains(tuKhoaDaBoDau));
}
