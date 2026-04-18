"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Plus, Trash2, RefreshCw } from "lucide-react";
import {
  getDocuments,
  deleteDocument,
  type DocumentListItem,
} from "@/lib/api";
import { Button, buttonVariants } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { StatusBadge } from "@/components/status-badge";
import { cn } from "@/lib/utils";

export default function DashboardPage() {
  const router = useRouter();
  const [documents, setDocuments] = useState<DocumentListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const hasActive = documents.some(
    (d) => d.status === "Pending" || d.status === "Processing"
  );

  async function fetchDocuments() {
    try {
      setDocuments(await getDocuments());
    } catch {
      setError("Failed to load documents.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    fetchDocuments();
  }, []);

  // Auto-poll while any document is being ingested
  useEffect(() => {
    if (!hasActive) return;
    const id = setInterval(fetchDocuments, 3000);
    return () => clearInterval(id);
  }, [hasActive]);

  async function handleDelete(docId: string, e: React.MouseEvent) {
    e.stopPropagation();
    if (!confirm("Delete this document and all its chunks?")) return;
    try {
      await deleteDocument(docId);
      setDocuments((prev) => prev.filter((d) => d.id !== docId));
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
            {documents.length} document{documents.length !== 1 ? "s" : ""}
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
    </div>
  );
}
