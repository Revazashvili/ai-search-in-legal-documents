using LegalDocumentAISearch.Application.Search;

namespace LegalDocumentAISearch.Application.Interfaces;

public interface IRagChatService
{
    IAsyncEnumerable<string> StreamAnswerAsync(string question, RagContext context, CancellationToken ct = default);
}
