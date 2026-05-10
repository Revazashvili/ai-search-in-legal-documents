"use client";

import { useState, useEffect, useCallback } from "react";
import { Eye, ChevronLeft, ChevronRight, X, Layers } from "lucide-react";
import { getDocuments, getDocument, type DocumentListItem, type DocumentDetail, type PagedResult } from "@/lib/api";

export default function DocumentsPage() {
  const [data, setData] = useState<PagedResult<DocumentListItem> | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [preview, setPreview] = useState<DocumentDetail | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);

  const fetchDocuments = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await getDocuments(page, 15);
      setData(result);
    } catch {
      setError("დოკუმენტების ჩატვირთვა ვერ მოხერხდა");
    } finally {
      setLoading(false);
    }
  }, [page]);

  useEffect(() => {
    fetchDocuments();
  }, [fetchDocuments]);

  const openPreview = async (id: string) => {
    setPreviewLoading(true);
    try {
      const doc = await getDocument(id);
      setPreview(doc);
    } catch {
      setError("დოკუმენტის ჩატვირთვა ვერ მოხერხდა");
    } finally {
      setPreviewLoading(false);
    }
  };

  return (
    <div>
      <h1 className="text-2xl font-semibold mb-6">დოკუმენტები</h1>

      {error && (
        <div className="mb-4 p-3 bg-red-50 text-red-700 rounded-lg text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <div className="space-y-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-14 bg-muted rounded-lg animate-pulse" />
          ))}
        </div>
      ) : data && data.items.length > 0 ? (
        <>
          <div className="bg-card border border-border rounded-lg overflow-hidden">
            <table className="w-full">
              <thead>
                <tr className="border-b border-border bg-muted/50">
                  <th className="text-left px-4 py-3 text-sm font-medium text-muted-foreground">სათაური</th>
                  <th className="text-left px-4 py-3 text-sm font-medium text-muted-foreground">ტიპი</th>
                  <th className="text-center px-4 py-3 text-sm font-medium text-muted-foreground">ნაწილები</th>
                  <th className="text-left px-4 py-3 text-sm font-medium text-muted-foreground">თარიღი</th>
                  <th className="text-center px-4 py-3 text-sm font-medium text-muted-foreground">მოქმედებები</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((doc) => (
                  <tr key={doc.id} className="border-b border-border last:border-0 hover:bg-muted/30 transition-colors">
                    <td className="px-4 py-3 text-sm font-medium">{doc.title}</td>
                    <td className="px-4 py-3 text-sm text-muted-foreground">{doc.documentType}</td>
                    <td className="px-4 py-3 text-sm text-center">
                      <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-primary/10 text-primary rounded-full text-xs font-medium">
                        <Layers className="w-3 h-3" />
                        {doc.chunkCount}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-sm text-muted-foreground">
                      {new Date(doc.uploadedAt).toLocaleDateString("ka-GE")}
                    </td>
                    <td className="px-4 py-3 text-center">
                      <button
                        onClick={() => openPreview(doc.id)}
                        className="inline-flex items-center justify-center w-8 h-8 rounded-lg hover:bg-accent transition-colors text-muted-foreground hover:text-foreground"
                        title="დეტალების ნახვა"
                      >
                        <Eye className="w-4 h-4" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {data.totalPages > 1 && (
            <div className="flex items-center justify-between mt-4">
              <span className="text-sm text-muted-foreground">
                სულ {data.totalCount} დოკუმენტი
              </span>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => setPage((p) => p - 1)}
                  disabled={!data.hasPreviousPage}
                  className="inline-flex items-center justify-center w-8 h-8 rounded-lg border border-border hover:bg-accent disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                >
                  <ChevronLeft className="w-4 h-4" />
                </button>
                <span className="text-sm text-muted-foreground px-2">
                  {data.page} / {data.totalPages}
                </span>
                <button
                  onClick={() => setPage((p) => p + 1)}
                  disabled={!data.hasNextPage}
                  className="inline-flex items-center justify-center w-8 h-8 rounded-lg border border-border hover:bg-accent disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                >
                  <ChevronRight className="w-4 h-4" />
                </button>
              </div>
            </div>
          )}
        </>
      ) : (
        <div className="text-center py-12 text-muted-foreground">
          დოკუმენტები ვერ მოიძებნა
        </div>
      )}

      {/* Preview Modal */}
      {(preview || previewLoading) && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50" onClick={() => setPreview(null)}>
          <div
            className="bg-card rounded-xl border border-border shadow-xl w-full max-w-lg mx-4 p-6"
            onClick={(e) => e.stopPropagation()}
          >
            {previewLoading ? (
              <div className="space-y-3">
                <div className="h-6 bg-muted rounded animate-pulse w-3/4" />
                <div className="h-4 bg-muted rounded animate-pulse w-1/2" />
                <div className="h-4 bg-muted rounded animate-pulse w-2/3" />
              </div>
            ) : preview ? (
              <>
                <div className="flex items-start justify-between mb-4">
                  <h2 className="text-lg font-semibold pr-4">{preview.title}</h2>
                  <button
                    onClick={() => setPreview(null)}
                    className="w-8 h-8 rounded-lg hover:bg-accent flex items-center justify-center flex-shrink-0 transition-colors"
                  >
                    <X className="w-4 h-4" />
                  </button>
                </div>
                <dl className="space-y-3 text-sm">
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">საკანონმდებლო აქტი</dt>
                    <dd className="font-medium">{preview.sourceLawName}</dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">ტიპი</dt>
                    <dd>{preview.documentType}</dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">დაყოფის სტრატეგია</dt>
                    <dd>{preview.chunkingStrategy}</dd>
                  </div>
                  {preview.dateEnacted && (
                    <div className="flex justify-between">
                      <dt className="text-muted-foreground">მიღების თარიღი</dt>
                      <dd>{preview.dateEnacted}</dd>
                    </div>
                  )}
                  {preview.lastAmended && (
                    <div className="flex justify-between">
                      <dt className="text-muted-foreground">ბოლო ცვლილება</dt>
                      <dd>{preview.lastAmended}</dd>
                    </div>
                  )}
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">ნაწილები</dt>
                    <dd>
                      <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-primary/10 text-primary rounded-full text-xs font-medium">
                        <Layers className="w-3 h-3" />
                        {preview.chunkCount}
                      </span>
                    </dd>
                  </div>
                  {preview.sourceUrl && (
                    <div className="flex justify-between">
                      <dt className="text-muted-foreground">წყარო</dt>
                      <dd>
                        <a
                          href={preview.sourceUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-primary hover:underline"
                        >
                          ბმული
                        </a>
                      </dd>
                    </div>
                  )}
                  <div className="flex justify-between">
                    <dt className="text-muted-foreground">ატვირთვის თარიღი</dt>
                    <dd>{new Date(preview.uploadedAt).toLocaleDateString("ka-GE")}</dd>
                  </div>
                </dl>
              </>
            ) : null}
          </div>
        </div>
      )}
    </div>
  );
}
