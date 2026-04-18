namespace LegalDocumentAISearch.Application.Documents;

public record UploadDocumentResult
{
    public bool IsSuccess { get; init; }
    public Guid? DocumentId { get; init; }
    public string? Title { get; init; }
    public string? Error { get; init; }

    public static UploadDocumentResult Success(Guid id, string title) =>
        new() { IsSuccess = true, DocumentId = id, Title = title };

    public static UploadDocumentResult Failure(string error) =>
        new() { IsSuccess = false, Error = error };
}
