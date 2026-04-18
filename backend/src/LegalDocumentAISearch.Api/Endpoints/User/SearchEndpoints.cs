using LegalDocumentAISearch.Application.Interfaces;
using LegalDocumentAISearch.Application.Search;
using System.Text.Json;

namespace LegalDocumentAISearch.Api.Endpoints.User;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/search/keyword", KeywordSearch).WithName("KeywordSearch");
        group.MapGet("/search/semantic", SemanticSearch).WithName("SemanticSearch");
        group.MapGet("/search/rag", RagSearch).WithName("RagSearch");

        return group;
    }

    private static async Task<IResult> KeywordSearch(
        string q, int limit = 10, ISearchService searchService = default!, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Results.BadRequest("Query parameter 'q' is required.");

        var response = await searchService.KeywordSearchAsync(q, Math.Clamp(limit, 1, 50), ct);
        return Results.Ok(response);
    }

    private static async Task<IResult> SemanticSearch(
        string q, int limit = 10, ISearchService searchService = default!, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Results.BadRequest("Query parameter 'q' is required.");

        var response = await searchService.SemanticSearchAsync(q, Math.Clamp(limit, 1, 50), ct);
        return Results.Ok(response);
    }

    private static async Task RagSearch(
        string q,
        HttpContext context,
        ISearchService searchService,
        IRagChatService ragChatService)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            context.Response.StatusCode = 400;
            return;
        }

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        var ct = context.RequestAborted;

        try
        {
            var ragContext = await searchService.GetRagContextAsync(q, ct);

            if (ragContext.Chunks.Count == 0)
            {
                await WriteSseEvent(context.Response, JsonSerializer.Serialize(new { done = true, sources = Array.Empty<string>() }));
                return;
            }

            await foreach (var token in ragChatService.StreamAnswerAsync(q, ragContext, ct))
            {
                await WriteSseEvent(context.Response, JsonSerializer.Serialize(new { token }));
            }

            var sources = ragContext.Chunks
                .Where(c => c.ArticleNumber != null)
                .Select(c => $"Article {c.ArticleNumber}")
                .Distinct()
                .ToArray();

            await WriteSseEvent(context.Response, JsonSerializer.Serialize(new { done = true, sources }));
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — normal
        }
        catch (Exception ex)
        {
            await WriteSseEvent(context.Response, JsonSerializer.Serialize(new { error = ex.Message }));
        }
    }

    private static async Task WriteSseEvent(HttpResponse response, string data)
    {
        await response.WriteAsync($"data: {data}\n\n");
        await response.Body.FlushAsync();
    }
}
