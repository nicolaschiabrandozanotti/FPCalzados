using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FPCalzados.Infrastructure;

internal static partial class DataNormalizer
{
    public static string Text(string? value)
    {
        var collapsed = Whitespace().Replace((value ?? "").Trim(), " ");
        var decomposed = collapsed.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string Phone(string? value) => new((value ?? "").Where(char.IsDigit).ToArray());

    public static string? Document(string? value)
    {
        var normalized = new string((value ?? "").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        return normalized.Length == 0 ? null : normalized;
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex Whitespace();
}
