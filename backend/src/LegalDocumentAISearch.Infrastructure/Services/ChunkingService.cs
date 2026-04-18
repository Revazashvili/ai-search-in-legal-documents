using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Domain.Entities;
using SharpToken;
using System.Text.RegularExpressions;

namespace LegalDocumentAISearch.Infrastructure.Services;

public class ChunkingService : IChunkingService
{
    private const int FixedWindowTokens = 500;
    private const int FixedOverlapTokens = 50;
    private const int ArticleMaxTokens = 800;

    private static readonly GptEncoding Encoding = GptEncoding.GetEncoding("cl100k_base");

    // Article pattern: Georgian "მუხლი N" or English "Article N"
    private static readonly Regex ArticleRegex = new(@"(?:მუხლი\s+\d+|Article\s+\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ArticleNumberRegex = new(@"\d+", RegexOptions.Compiled);

    public List<DocumentChunk> Chunk(Guid documentId, string text, string strategy)
    {
        return strategy switch
        {
            Domain.Entities.ChunkingStrategy.FixedSize => ChunkFixedSize(documentId, text),
            Domain.Entities.ChunkingStrategy.ArticleLevel => ChunkArticleLevel(documentId, text),
            Domain.Entities.ChunkingStrategy.Hierarchical => ChunkHierarchical(documentId, text),
            _ => ChunkFixedSize(documentId, text)
        };
    }

    private static List<DocumentChunk> ChunkFixedSize(Guid documentId, string text)
    {
        var tokens = Encoding.Encode(text);
        var chunks = new List<DocumentChunk>();
        int index = 0;
        int start = 0;

        while (start < tokens.Count)
        {
            int end = Math.Min(start + FixedWindowTokens, tokens.Count);
            var windowTokens = tokens.Skip(start).Take(end - start).ToList();
            var chunkText = Encoding.Decode(windowTokens);

            chunks.Add(new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                ChunkType = "Chunk",
                ChunkIndex = index++,
                ChunkText = chunkText,
                TokenCount = windowTokens.Count,
                CreatedAt = DateTimeOffset.UtcNow
            });

            start += FixedWindowTokens - FixedOverlapTokens;
        }

        return chunks;
    }

    private static List<DocumentChunk> ChunkArticleLevel(Guid documentId, string text)
    {
        var sections = SplitByArticle(text);
        var chunks = new List<DocumentChunk>();
        int index = 0;

        foreach (var (header, body) in sections)
        {
            var fullText = string.IsNullOrEmpty(header) ? body : $"{header}\n{body}";
            var articleNumber = string.IsNullOrEmpty(header) ? null : ArticleNumberRegex.Match(header).Value;
            if (string.IsNullOrWhiteSpace(articleNumber)) articleNumber = null;

            var tokens = Encoding.Encode(fullText);

            if (tokens.Count <= ArticleMaxTokens)
            {
                chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    ChunkType = "Article",
                    ArticleNumber = articleNumber,
                    ChunkIndex = index++,
                    ChunkText = fullText,
                    TokenCount = tokens.Count,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                // Fall back to fixed-size for oversized articles
                var subChunks = ChunkFixedSize(documentId, fullText);
                foreach (var sub in subChunks)
                {
                    sub.ArticleNumber = articleNumber;
                    sub.ChunkIndex = index++;
                }
                chunks.AddRange(subChunks);
            }
        }

        return chunks;
    }

    private static List<DocumentChunk> ChunkHierarchical(Guid documentId, string text)
    {
        var sections = SplitByArticle(text);
        var chunks = new List<DocumentChunk>();
        int index = 0;

        foreach (var (header, body) in sections)
        {
            var fullText = string.IsNullOrEmpty(header) ? body : $"{header}\n{body}";
            var articleNumber = string.IsNullOrEmpty(header) ? null : ArticleNumberRegex.Match(header).Value;
            if (string.IsNullOrWhiteSpace(articleNumber)) articleNumber = null;

            var parentTokens = Encoding.Encode(fullText);

            // Parent article chunk (not embedded)
            var parent = new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                ChunkType = "Article",
                ArticleNumber = articleNumber,
                ChunkIndex = index++,
                ChunkText = fullText,
                TokenCount = parentTokens.Count,
                CreatedAt = DateTimeOffset.UtcNow
            };
            chunks.Add(parent);

            // Child paragraph chunks (these get embedded)
            var paragraphs = body.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);
            int paraIndex = 0;

            foreach (var para in paragraphs)
            {
                var trimmed = para.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                var paraTokens = Encoding.Encode(trimmed);
                chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    ParentChunkId = parent.Id,
                    ChunkType = "Paragraph",
                    ArticleNumber = articleNumber,
                    ChunkIndex = index++,
                    ChunkText = trimmed,
                    TokenCount = paraTokens.Count,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                paraIndex++;
            }

            // If no paragraphs found, make the article its own child for embedding
            if (paraIndex == 0)
            {
                chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    ParentChunkId = parent.Id,
                    ChunkType = "Paragraph",
                    ArticleNumber = articleNumber,
                    ChunkIndex = index++,
                    ChunkText = fullText,
                    TokenCount = parentTokens.Count,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        return chunks;
    }

    private static List<(string Header, string Body)> SplitByArticle(string text)
    {
        var matches = ArticleRegex.Matches(text);
        var sections = new List<(string, string)>();

        if (matches.Count == 0)
        {
            sections.Add((string.Empty, text));
            return sections;
        }

        // Text before first article
        if (matches[0].Index > 0)
        {
            var preamble = text[..matches[0].Index].Trim();
            if (!string.IsNullOrWhiteSpace(preamble))
                sections.Add((string.Empty, preamble));
        }

        for (int i = 0; i < matches.Count; i++)
        {
            int bodyStart = matches[i].Index + matches[i].Length;
            int bodyEnd = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;

            // Find end of header line
            int headerLineEnd = text.IndexOf('\n', matches[i].Index);
            if (headerLineEnd < 0 || headerLineEnd > bodyEnd) headerLineEnd = bodyEnd;

            var header = text[matches[i].Index..headerLineEnd].Trim();
            var body = text[headerLineEnd..bodyEnd].Trim();

            sections.Add((header, body));
        }

        return sections;
    }
}
