namespace LegalDocumentAISearch.Application.Search;

public record SearchResponse(
    string Query,
    string Mode,
    IReadOnlyList<SearchResultDto> Results,
    long LatencyMs);
