using LegalDocumentAISearch.Domain.Entities;

namespace LegalDocumentAISearch.Application.Interfaces;

public interface IChunkingService
{
    List<DocumentChunk> Chunk(Guid documentId, string text, string strategy);
}
