"use client";

import { useState, useEffect } from "react";
import { useParams, useSearchParams } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Layers } from "lucide-react";
import { getDocument, getDocumentFileUrl, type DocumentDetail } from "@/lib/api";

export default function DocumentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const searchParams = useSearchParams();
  const query = searchParams.get("q") || "";

  const [doc, setDoc] = useState<DocumentDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const hasPdf = doc?.filePath != null;
  const fileUrl = hasPdf ? getDocumentFileUrl(id) : null;

  useEffect(() => {
    (async () => {
      try {
        const data = await getDocument(id);
        setDoc(data);
      } catch {
        setError("დოკუმენტის ჩატვირთვა ვერ მოხერხდა");
      } finally {
        setLoading(false);
      }
    })();
  }, [id]);

  if (loading) {
    return (
      <div className="space-y-4 max-w-4xl mx-auto">
        <div className="h-8 bg-muted rounded animate-pulse w-1/3" />
        <div className="h-4 bg-muted rounded animate-pulse w-1/4" />
        <div className="space-y-2 mt-8">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="h-4 bg-muted rounded animate-pulse" />
          ))}
        </div>
      </div>
    );
  }

  if (error || !doc) {
    return (
      <div className="text-center py-12">
        <p className="text-sm text-red-600 mb-4">{error || "დოკუმენტი ვერ მოიძებნა"}</p>
        <Link href="/" className="text-primary hover:underline text-sm">
          უკან დაბრუნება
        </Link>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto">
      {/* Header */}
      <div className="mb-6">
        <Link
          href={query ? `/?q=${encodeURIComponent(query)}` : "/documents"}
          className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground mb-3 transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          {query ? "ძიებაზე დაბრუნება" : "დოკუმენტებზე დაბრუნება"}
        </Link>
        <h1 className="text-2xl font-semibold mb-1">{doc.title}</h1>
        <p className="text-sm text-muted-foreground">{doc.sourceLawName}</p>
        <div className="flex items-center gap-4 mt-3 text-xs text-muted-foreground">
          {doc.dateEnacted && <span>მიღებულია: {doc.dateEnacted}</span>}
          {doc.lastAmended && <span>ცვლილება: {doc.lastAmended}</span>}
          <span className="inline-flex items-center gap-1">
            <Layers className="w-3 h-3" />
            {doc.chunkCount} ნაწილი
          </span>
        </div>
      </div>

      {/* Content */}
      {hasPdf && fileUrl ? (
        <div className="border border-border rounded-lg overflow-hidden bg-card">
          <iframe
            src={fileUrl}
            className="w-full"
            style={{ height: "80vh" }}
            title={doc.title}
          />
        </div>
      ) : (
        <div className="bg-card border border-border rounded-lg p-6">
          <div className="text-sm leading-relaxed whitespace-pre-wrap">
            {query ? <HighlightedText text={doc.rawText} query={query} /> : doc.rawText}
          </div>
        </div>
      )}

      {/* Source URL */}
      {doc.sourceUrl && (
        <div className="mt-4 text-sm">
          <span className="text-muted-foreground">წყარო: </span>
          <a
            href={doc.sourceUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="text-primary hover:underline"
          >
            {doc.sourceUrl}
          </a>
        </div>
      )}
    </div>
  );
}

function HighlightedText({ text, query }: { text: string; query: string }) {
  if (!query.trim()) return <>{text}</>;

  const words = query
    .trim()
    .split(/\s+/)
    .filter((w) => w.length > 1)
    .map((w) => w.replace(/[.*+?^${}()|[\]\\]/g, "\\$&"));

  if (words.length === 0) return <>{text}</>;

  const regex = new RegExp(`(${words.join("|")})`, "gi");
  const parts = text.split(regex);

  return (
    <>
      {parts.map((part, i) =>
        regex.test(part) ? (
          <mark key={i} className="bg-yellow-200 text-foreground rounded px-0.5">
            {part}
          </mark>
        ) : (
          <span key={i}>{part}</span>
        )
      )}
    </>
  );
}
