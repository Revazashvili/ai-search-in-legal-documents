const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5081";

async function apiFetch(path: string, init?: RequestInit): Promise<Response> {
  return fetch(`${API_BASE}${path}`, { ...init, credentials: "include" });
}

// ── Auth ──────────────────────────────────────────────────────────────────────

export async function checkAuth(): Promise<boolean> {
  try {
    const res = await apiFetch("/api/admin/manage/info");
    return res.ok;
  } catch {
    return false;
  }
}

// ── Types ─────────────────────────────────────────────────────────────────────

export interface DocumentListItem {
  id: string;
  title: string;
  documentType: string;
  chunkingStrategy: string;
  status: string;
  chunkCount: number;
  uploadedAt: string;
}

export interface ChunkSummary {
  id: string;
  chunkType: string;
  articleNumber: string | null;
  chunkIndex: number;
  tokenCount: number;
  hasEmbedding: boolean;
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

export interface UploadResponse {
  documentId: string;
  title: string;
  status: string;
}

// ── Documents ─────────────────────────────────────────────────────────────────

export async function getDocuments(): Promise<DocumentListItem[]> {
  const res = await apiFetch("/api/admin/documents");
  if (!res.ok) throw new Error("Failed to fetch documents");
  return res.json();
}

export async function getDocument(id: string): Promise<DocumentDetail> {
  const res = await apiFetch(`/api/admin/documents/${id}`);
  if (!res.ok) throw new Error("Failed to fetch document");
  return res.json();
}

export async function uploadDocument(data: FormData): Promise<UploadResponse> {
  const res = await apiFetch("/api/admin/documents", { method: "POST", body: data });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || "Upload failed");
  }
  return res.json();
}

export async function deleteDocument(id: string): Promise<void> {
  const res = await apiFetch(`/api/admin/documents/${id}`, { method: "DELETE" });
  if (!res.ok) throw new Error("Failed to delete document");
}
