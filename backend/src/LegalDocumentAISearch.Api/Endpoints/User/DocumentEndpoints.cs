using LegalDocumentAISearch.Application.Documents;
using LegalDocumentAISearch.Domain.Entities;

namespace LegalDocumentAISearch.Api.Endpoints.User;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/documents", ListDocuments).WithName("ListPublicDocuments");
        group.MapGet("/documents/{id:guid}", GetDocument).WithName("GetPublicDocument");
        group.MapGet("/documents/{id:guid}/file", GetDocumentFile).WithName("GetPublicDocumentFile");

        return group;
    }

    private static async Task<IResult> ListDocuments(
        int page = 1, int pageSize = 20, IDocumentService documentService = default!, CancellationToken ct = default)
    {
        var result = await documentService.ListPublicDocumentsAsync(Math.Clamp(page, 1, int.MaxValue), Math.Clamp(pageSize, 1, 50), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetDocument(
        Guid id, IDocumentService documentService = default!, CancellationToken ct = default)
    {
        var document = await documentService.GetDocumentAsync(id, ct);
        if (document is null || document.Status != DocumentStatus.Ready)
            return Results.NotFound();

        return Results.Ok(document);
    }

    private static async Task<IResult> GetDocumentFile(
        Guid id, IDocumentService documentService = default!, CancellationToken ct = default)
    {
        var document = await documentService.GetDocumentAsync(id, ct);
        if (document is null || document.Status != DocumentStatus.Ready)
            return Results.NotFound();

        if (string.IsNullOrEmpty(document.FilePath) || !File.Exists(document.FilePath))
            return Results.NotFound();

        var stream = File.OpenRead(document.FilePath);
        return Results.File(stream, "application/pdf");
    }
}
