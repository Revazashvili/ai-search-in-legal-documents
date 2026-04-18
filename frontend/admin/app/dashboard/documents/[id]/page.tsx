"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Trash2, CheckCircle2, XCircle, RefreshCw } from "lucide-react";
import { getDocument, deleteDocument, type DocumentDetail } from "@/lib/api";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  Card,
  CardHeader,
  CardTitle,
  CardContent,
} from "@/components/ui/card";
import { StatusBadge } from "@/components/status-badge";
import { cn } from "@/lib/utils";

export default function DocumentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [doc, setDoc] = useState<DocumentDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const isActive =
    doc?.status === "Pending" || doc?.status === "Processing";

  async function fetchDoc() {
    try {
      setDoc(await getDocument(id));
    } catch {
      setError("Failed to load document.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    fetchDoc();
  }, [id]);

  // Auto-poll while ingesting
  useEffect(() => {
    if (!isActive) return;
    const interval = setInterval(fetchDoc, 3000);
    return () => clearInterval(interval);
  }, [isActive]);

  async function handleDelete() {
    if (!confirm("Delete this document and all its chunks?")) return;
    try {
      await deleteDocument(id);
      router.push("/dashboard");
    } catch {
      setError("Failed to delete document.");
    }
  }

  if (loading) {
    return (
      <div className="py-12 text-center text-sm text-muted-foreground">
        Loading…
      </div>
    );
  }

  if (!doc) {
    return (
      <div className="py-12 text-center">
        <p className="text-sm text-destructive">
          {error || "Document not found."}
        </p>
        <Link
          href="/dashboard"
          className={cn(buttonVariants({ variant: "outline", size: "sm" }), "mt-4")}
        >
          Back to Documents
        </Link>
      </div>
    );
  }

  return (
    <div className="max-w-3xl space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <Link
            href="/dashboard"
            className={cn(
              buttonVariants({ variant: "ghost", size: "icon-sm" })
            )}
          >
            <ArrowLeft size={15} />
          </Link>
          <div>
            <h1 className="text-xl font-semibold">{doc.title}</h1>
            <p className="mt-0.5 text-sm text-muted-foreground">
              {doc.sourceLawName}
            </p>
          </div>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={handleDelete}
          className="text-destructive hover:text-destructive"
        >
          <Trash2 size={14} />
          Delete
        </Button>
      </div>

      {error && <p className="text-sm text-destructive">{error}</p>}

      {/* Metadata */}
      <Card>
        <CardHeader>
          <CardTitle>Metadata</CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm">
            <div>
              <dt className="text-muted-foreground">Status</dt>
              <dd className="mt-0.5">
                <StatusBadge status={doc.status} />
              </dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Chunks</dt>
              <dd className="mt-0.5 font-medium">{doc.chunkCount}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Document Type</dt>
              <dd className="mt-0.5">{doc.documentType}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Chunking Strategy</dt>
              <dd className="mt-0.5">{doc.chunkingStrategy}</dd>
            </div>
            {doc.dateEnacted && (
              <div>
                <dt className="text-muted-foreground">Date Enacted</dt>
                <dd className="mt-0.5">{doc.dateEnacted}</dd>
              </div>
            )}
            {doc.lastAmended && (
              <div>
                <dt className="text-muted-foreground">Last Amended</dt>
                <dd className="mt-0.5">{doc.lastAmended}</dd>
              </div>
            )}
            {doc.sourceUrl && (
              <div className="col-span-2">
                <dt className="text-muted-foreground">Source URL</dt>
                <dd className="mt-0.5">
                  <a
                    href={doc.sourceUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="truncate text-primary underline underline-offset-2"
                  >
                    {doc.sourceUrl}
                  </a>
                </dd>
              </div>
            )}
            <div>
              <dt className="text-muted-foreground">Uploaded</dt>
              <dd className="mt-0.5">
                {new Date(doc.uploadedAt).toLocaleString()}
              </dd>
            </div>
          </dl>

          {doc.status === "Failed" && doc.errorMessage && (
            <div className="mt-4 rounded-lg bg-destructive/10 p-3 text-sm text-destructive">
              <strong>Error:</strong> {doc.errorMessage}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Chunks table */}
      {doc.chunks.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Chunks ({doc.chunkCount})</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border">
                  {["#", "Type", "Article", "Tokens", "Embedded"].map((h) => (
                    <th
                      key={h}
                      className="px-4 py-3 text-left text-xs font-medium text-muted-foreground"
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {doc.chunks.map((chunk) => (
                  <tr
                    key={chunk.id}
                    className="border-b border-border transition-colors last:border-0 hover:bg-accent/30"
                  >
                    <td className="px-4 py-2.5 text-muted-foreground">
                      {chunk.chunkIndex}
                    </td>
                    <td className="px-4 py-2.5">{chunk.chunkType}</td>
                    <td className="px-4 py-2.5 text-muted-foreground">
                      {chunk.articleNumber ?? "—"}
                    </td>
                    <td className="px-4 py-2.5 text-muted-foreground">
                      {chunk.tokenCount}
                    </td>
                    <td className="px-4 py-2.5">
                      {chunk.hasEmbedding ? (
                        <CheckCircle2
                          size={14}
                          className="text-green-600 dark:text-green-400"
                        />
                      ) : (
                        <XCircle size={14} className="text-muted-foreground" />
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardContent>
        </Card>
      )}

      {/* Ingestion progress indicator */}
      {isActive && (
        <p className="flex items-center justify-center gap-1.5 text-sm text-muted-foreground">
          <RefreshCw size={13} className="animate-spin" />
          Ingestion in progress — refreshing automatically…
        </p>
      )}
    </div>
  );
}
