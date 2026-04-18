using LegalDocumentAISearch.Application.Interfaces;
using UglyToad.PdfPig;

namespace LegalDocumentAISearch.Infrastructure.Services;

public class PdfTextExtractor : IPdfTextExtractor
{
    public string ExtractText(Stream stream, string fileName)
    {
        if (fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        // PDF extraction via PdfPig
        using var document = PdfDocument.Open(stream);
        var pages = document.GetPages().ToList();
        var sb = new System.Text.StringBuilder();

        foreach (var page in pages)
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0) continue;

            // Heuristic: skip header/footer lines (lines with < 5 words at very top/bottom)
            var pageText = page.Text;
            var lines = pageText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var filteredLines = FilterHeadersAndFooters(lines);
            sb.AppendLine(string.Join("\n", filteredLines));
        }

        return sb.ToString().Trim();
    }

    private static IEnumerable<string> FilterHeadersAndFooters(string[] lines)
    {
        if (lines.Length <= 6) return lines;

        // Skip first 2 and last 2 lines if they have < 5 words (likely header/footer)
        var result = lines.ToList();

        for (int i = 0; i < Math.Min(2, result.Count); i++)
        {
            if (result[i].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 5)
            {
                result[i] = string.Empty;
            }
        }

        for (int i = Math.Max(0, result.Count - 2); i < result.Count; i++)
        {
            if (result[i].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 5)
            {
                result[i] = string.Empty;
            }
        }

        return result.Where(l => !string.IsNullOrWhiteSpace(l));
    }
}
