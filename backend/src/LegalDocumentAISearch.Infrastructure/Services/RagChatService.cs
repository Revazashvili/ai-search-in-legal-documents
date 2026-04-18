using System.Runtime.CompilerServices;
using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Application.Search;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;

namespace LegalDocumentAISearch.Infrastructure.Services;

public class RagChatService(OpenAIClient openAiClient, IConfiguration configuration) : IRagChatService
{
    private const string SystemPrompt = """
        You are a legal document assistant. Answer the user's question using ONLY
        the provided legal text excerpts. Cite specific article numbers.
        If the answer is not in the provided text, say so clearly.
        Do not give legal advice — state that this is for informational purposes only.
        """;

    public async IAsyncEnumerable<string> StreamAnswerAsync(
        string question,
        RagContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var contextText = string.Join("\n\n", context.Chunks.Select(c =>
            $"[{c.DocumentTitle}{(c.ArticleNumber != null ? $", Article {c.ArticleNumber}" : "")}]\n{c.Text}"));

        var userPrompt = $"Context:\n{contextText}\n\nQuestion: {question}";

        var chatModel = configuration["OpenAI:ChatModel"] ?? "gpt-4o";
        var chatClient = openAiClient.GetChatClient(chatModel);

        List<ChatMessage> messages =
        [
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(userPrompt)
        ];

        await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, cancellationToken: ct))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                    yield return part.Text;
            }
        }
    }
}
