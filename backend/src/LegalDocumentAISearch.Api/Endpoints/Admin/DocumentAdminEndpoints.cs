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

        group.MapGet("/documents/{id:guid}/file", GetDocumentFile)
            .RequireAuthorization()
            .WithName("GetAdminDocumentFile");

        return group;
    }

    private static async Task<IResult> ListDocuments(int? page, int? pageSize, IDocumentService documentService, CancellationToken ct) =>
        Results.Ok(await documentService.ListDocumentsAsync(page ?? 1, pageSize ?? 20, ct));

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

        const long maxFileSize = 50 * 1024 * 1024; // 50 MB

        var file = form.Files["file"];
        if (file is not null && file.Length > maxFileSize)
            return Results.BadRequest($"File exceeds maximum size of 50 MB.");

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

        // Save PDF to disk
        var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var savedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var savedFilePath = Path.Combine(uploadsDir, savedFileName);
        await using (var fs = new FileStream(savedFilePath, FileMode.Create))
        {
            await file.CopyToAsync(fs, ct);
        }

        await using var stream = File.OpenRead(savedFilePath);
        var command = new UploadDocumentCommand(stream, file.FileName, title, sourceLawName,
            documentType, chunkingStrategy, dateEnacted, lastAmended, sourceUrl, savedFilePath);

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

    private static async Task<IResult> GetDocumentFile(Guid id, IDocumentService documentService, CancellationToken ct)
    {
        var document = await documentService.GetDocumentAsync(id, ct);
        if (document is null)
            return Results.NotFound();

        if (string.IsNullOrEmpty(document.FilePath) || !File.Exists(document.FilePath))
            return Results.NotFound();

        var stream = File.OpenRead(document.FilePath);
        return Results.File(stream, "application/pdf");
    }
}
