using LegalDocumentAISearch.Application.Documents;
using LegalDocumentAISearch.Domain.Entities;

namespace LegalDocumentAISearch.Api.Endpoints.Admin;

public static class DocumentAdminEndpoints
{
    public static IEndpointRouteBuilder MapDocumentAdminEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/documents", ListDocuments)
            .RequireAuthorization()
            .WithName("ListDocuments");

        group.MapGet("/documents/{id:guid}", GetDocument)
            .RequireAuthorization()
            .WithName("GetDocument");

        group.MapPost("/documents", UploadDocument)
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithName("UploadDocument");

        group.MapDelete("/documents/{id:guid}", DeleteDocument)
            .RequireAuthorization()
            .WithName("DeleteDocument");

        return group;
    }

    private static async Task<IResult> ListDocuments(IDocumentService documentService, CancellationToken ct) =>
        Results.Ok(await documentService.ListDocumentsAsync(ct));

    private static async Task<IResult> GetDocument(Guid id, IDocumentService documentService, CancellationToken ct)
    {
        var document = await documentService.GetDocumentAsync(id, ct);
        return document is null ? Results.NotFound() : Results.Ok(document);
    }

    private static async Task<IResult> UploadDocument(HttpRequest request, IDocumentService documentService, CancellationToken ct)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest("Expected multipart/form-data");

        var form = await request.ReadFormAsync(ct);

        var file = form.Files["file"];
        var title = form["title"].ToString();
        var sourceLawName = form["sourceLawName"].ToString();
        var documentType = form["documentType"].ToString();
        var chunkingStrategy = form["chunkingStrategy"].ToString();

        if (file is null || string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(sourceLawName) || string.IsNullOrWhiteSpace(documentType) ||
            string.IsNullOrWhiteSpace(chunkingStrategy))
        {
            return Results.BadRequest("Missing required fields: file, title, sourceLawName, documentType, chunkingStrategy");
        }

        var validDocumentTypes = new[] { "Law", "Code", "Regulation", "Other" };
        var validStrategies = new[] { ChunkingStrategy.FixedSize, ChunkingStrategy.ArticleLevel, ChunkingStrategy.Hierarchical };

        if (!validDocumentTypes.Contains(documentType))
            return Results.BadRequest($"Invalid documentType. Must be one of: {string.Join(", ", validDocumentTypes)}");

        if (!validStrategies.Contains(chunkingStrategy))
            return Results.BadRequest($"Invalid chunkingStrategy. Must be one of: {string.Join(", ", validStrategies)}");

        DateOnly? dateEnacted = DateOnly.TryParse(form["dateEnacted"], out var de) ? de : null;
        DateOnly? lastAmended = DateOnly.TryParse(form["lastAmended"], out var la) ? la : null;
        string? sourceUrl = string.IsNullOrWhiteSpace(form["sourceUrl"]) ? null : form["sourceUrl"].ToString();

        await using var stream = file.OpenReadStream();
        var command = new UploadDocumentCommand(stream, file.FileName, title, sourceLawName,
            documentType, chunkingStrategy, dateEnacted, lastAmended, sourceUrl);

        var result = await documentService.UploadDocumentAsync(command, ct);

        if (!result.IsSuccess)
            return Results.BadRequest(result.Error);

        return Results.Accepted($"/api/admin/documents/{result.DocumentId}", new
        {
            result.DocumentId,
            result.Title,
            Status = DocumentStatus.Pending,
            Message = "Document received. Ingestion pipeline started."
        });
    }

    private static async Task<IResult> DeleteDocument(Guid id, IDocumentService documentService, CancellationToken ct)
    {
        var deleted = await documentService.DeleteDocumentAsync(id, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
