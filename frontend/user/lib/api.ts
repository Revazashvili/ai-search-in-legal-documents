const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5081";

export interface DocumentListItem {
  id: string;
  title: string;
  documentType: string;
  chunkingStrategy: string;
  status: string;
  chunkCount: number;
  uploadedAt: string;
}

export interface DocumentDetail {
  id: string;
  title: string;
  sourceLawName: string;
  documentType: string;
  chunkingStrategy: string;
  status: string;
  errorMessage: string | null;
  dateEnacted: string | null;
  lastAmended: string | null;
  sourceUrl: string | null;
  uploadedAt: string;
  chunkCount: number;
  chunks: ChunkSummary[];
}

export interface ChunkSummary {
  id: string;
  chunkType: string;
  articleNumber: string | null;
  chunkIndex: number;
  tokenCount: number;
  hasEmbedding: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface SearchResult {
  chunkId: string;
  documentId: string;
  documentTitle: string;
  articleNumber: string | null;
  chunkText: string;
  score: number;
  parentChunkId: string | null;
}

export interface SearchResponse {
  query: string;
  mode: string;
  results: SearchResult[];
  latencyMs: number;
}

async function fetchApi<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`);
  if (!res.ok) {
    throw new Error(`API error: ${res.status}`);
  }
  return res.json();
}

export function getDocuments(page = 1, pageSize = 20) {
  return fetchApi<PagedResult<DocumentListItem>>(
    `/api/documents?page=${page}&pageSize=${pageSize}`
  );
}

export function getDocument(id: string) {
  return fetchApi<DocumentDetail>(`/api/documents/${id}`);
}

export function keywordSearch(q: string, limit = 10) {
  return fetchApi<SearchResponse>(
    `/api/search/keyword?q=${encodeURIComponent(q)}&limit=${limit}`
  );
}

export function semanticSearch(q: string, limit = 10) {
  return fetchApi<SearchResponse>(
    `/api/search/semantic?q=${encodeURIComponent(q)}&limit=${limit}`
  );
}

export function createRagEventSource(q: string): EventSource {
  return new EventSource(
    `${API_BASE}/api/search/rag?q=${encodeURIComponent(q)}`
  );
}
