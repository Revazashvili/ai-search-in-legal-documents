"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Plus, Trash2, RefreshCw, ChevronLeft, ChevronRight } from "lucide-react";
import {
  getDocuments,
  deleteDocument,
  type DocumentListItem,
  type PagedResult,
} from "@/lib/api";
import { Button, buttonVariants } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { StatusBadge } from "@/components/status-badge";
import { cn } from "@/lib/utils";

export default function DashboardPage() {
  const router = useRouter();
  const [result, setResult] = useState<PagedResult<DocumentListItem> | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const documents = result?.items ?? [];
  const hasActive = documents.some(
    (d) => d.status === "Pending" || d.status === "Processing"
  );

  async function fetchDocuments(p = page) {
    try {
      setResult(await getDocuments(p));
    } catch {
      setError("Failed to load documents.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    fetchDocuments(page);
  }, [page]);

  // Auto-poll while any document is being ingested
  useEffect(() => {
    if (!hasActive) return;
    const id = setInterval(() => fetchDocuments(page), 3000);
    return () => clearInterval(id);
  }, [hasActive, page]);

  async function handleDelete(docId: string, e: React.MouseEvent) {
    e.stopPropagation();
    if (!confirm("Delete this document and all its chunks?")) return;
    try {
      await deleteDocument(docId);
      fetchDocuments(page);
    } catch {
      setError("Failed to delete document.");
    }
  }

  return (
    <div className="space-y-6">
      {/* Page header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold">Documents</h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            {result?.totalCount ?? 0} document{(result?.totalCount ?? 0) !== 1 ? "s" : ""}
          </p>
        </div>
        <div className="flex items-center gap-3">
          {hasActive && (
            <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
              <RefreshCw size={12} className="animate-spin" />
              Ingesting…
            </span>
          )}
          <Link
            href="/dashboard/upload"
            className={cn(buttonVariants({ size: "sm" }))}
          >
            <Plus size={14} />
            Upload
          </Link>
        </div>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      <Card>
        <CardContent className="p-0">
          {loading ? (
            <div className="py-12 text-center text-sm text-muted-foreground">
              Loading…
            </div>
          ) : documents.length === 0 ? (
            <div className="py-12 text-center">
              <p className="text-sm text-muted-foreground">No documents yet.</p>
              <Link
                href="/dashboard/upload"
                className={cn(
                  buttonVariants({ variant: "outline", size: "sm" }),
                  "mt-3"
                )}
              >
                Upload your first document
              </Link>
            </div>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border">
                  {["Title", "Type", "Strategy", "Status", "Chunks", "Uploaded", ""].map(
                    (h) => (
                      <th
                        key={h}
                        className="px-4 py-3 text-left text-xs font-medium text-muted-foreground"
                      >
                        {h}
                      </th>
                    )
                  )}
                </tr>
              </thead>
              <tbody>
                {documents.map((doc) => (
                  <tr
                    key={doc.id}
                    onClick={() => router.push(`/dashboard/documents/${doc.id}`)}
                    className="cursor-pointer border-b border-border transition-colors last:border-0 hover:bg-accent/50"
                  >
                    <td className="px-4 py-3 font-medium">{doc.title}</td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {doc.documentType}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {doc.chunkingStrategy}
                    </td>
                    <td className="px-4 py-3">
                      <StatusBadge status={doc.status} />
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {doc.chunkCount}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {new Date(doc.uploadedAt).toLocaleDateString()}
                    </td>
                    <td className="px-4 py-3">
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        onClick={(e) => handleDelete(doc.id, e)}
                        className="text-muted-foreground hover:text-destructive"
                      >
                        <Trash2 size={14} />
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>

      {/* Pagination controls */}
      {result && result.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-xs text-muted-foreground">
            Page {result.page} of {result.totalPages}
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={!result.hasPreviousPage}
              onClick={() => setPage((p) => p - 1)}
            >
              <ChevronLeft size={14} />
              Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={!result.hasNextPage}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
              <ChevronRight size={14} />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
