using System.Globalization;
using System.Text;

namespace FPCalzados.Infrastructure;

internal static class SimplePdfWriter
{
    public static void Write(string path, string title, IEnumerable<string> lines)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var pages = BuildPages(title, lines);
        var objects = new List<string>();
        var pageObjectNumbers = new List<int>();

        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        objects.Add("");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        foreach (var pageLines in pages)
        {
            var content = BuildContent(pageLines);
            var contentObjectNumber = objects.Count + 2;
            var pageObjectNumber = objects.Count + 1;
            pageObjectNumbers.Add(pageObjectNumber);
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }

        objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectNumbers.Select(item => $"{item} 0 R"))}] /Count {pageObjectNumbers.Count} >>";

        using var stream = File.Create(path);
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };
        writer.WriteLine("%PDF-1.4");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            writer.Flush();
            offsets.Add(stream.Position);
            writer.WriteLine($"{index + 1} 0 obj");
            writer.WriteLine(objects[index]);
            writer.WriteLine("endobj");
        }

        writer.Flush();
        var xrefOffset = stream.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Count + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
        {
            writer.WriteLine($"{offset.ToString("0000000000", CultureInfo.InvariantCulture)} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
        writer.WriteLine("%%EOF");
    }

    private static List<List<string>> BuildPages(string title, IEnumerable<string> lines)
    {
        const int maxLines = 48;
        var all = new List<string> { title, new string('-', Math.Min(90, title.Length + 8)) };
        all.AddRange(lines);
        return all
            .Select((line, index) => new { line, index })
            .GroupBy(item => item.index / maxLines)
            .Select(group => group.Select(item => item.line).ToList())
            .ToList();
    }

    private static string BuildContent(IReadOnlyList<string> lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("BT");
        builder.AppendLine("/F1 10 Tf");
        builder.AppendLine("14 TL");
        builder.AppendLine("50 800 Td");
        foreach (var line in lines)
        {
            builder.AppendLine($"({Escape(TrimToFit(line))}) Tj");
            builder.AppendLine("T*");
        }

        builder.AppendLine("ET");
        return builder.ToString();
    }

    private static string TrimToFit(string value) => value.Length <= 115 ? value : value[..112] + "...";

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
            .Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U")
            .Replace("ñ", "n").Replace("Ñ", "N");
    }
}
