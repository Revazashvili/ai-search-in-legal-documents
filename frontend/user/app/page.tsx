"use client";

import { useState, useRef, useCallback } from "react";
import Link from "next/link";
import { Search, Zap, Brain, MessageSquare, Loader2 } from "lucide-react";
import {
  keywordSearch,
  semanticSearch,
  createRagEventSource,
  type SearchResponse,
  type SearchResult,
} from "@/lib/api";

type SearchMode = "keyword" | "semantic" | "rag";

const modes: { key: SearchMode; label: string; icon: typeof Search }[] = [
  { key: "keyword", label: "საკვანძო სიტყვა", icon: Search },
  { key: "semantic", label: "სემანტიკური", icon: Brain },
  { key: "rag", label: "RAG", icon: MessageSquare },
];

export default function SearchPage() {
  const [mode, setMode] = useState<SearchMode>("keyword");
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState<SearchResponse | null>(null);
  const [ragAnswer, setRagAnswer] = useState("");
  const [ragSources, setRagSources] = useState<string[]>([]);
  const [ragStreaming, setRagStreaming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const eventSourceRef = useRef<EventSource | null>(null);
  const ragAnswerRef = useRef<HTMLDivElement>(null);

  const stopStreaming = useCallback(() => {
    if (eventSourceRef.current) {
      eventSourceRef.current.close();
      eventSourceRef.current = null;
    }
    setRagStreaming(false);
  }, []);

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!query.trim()) return;

    setError(null);
    setResults(null);
    setRagAnswer("");
    setRagSources([]);
    stopStreaming();

    if (mode === "rag") {
      setRagStreaming(true);
      setLoading(true);
      const es = createRagEventSource(query);
      eventSourceRef.current = es;

      es.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data);
          if (data.error) {
            setError(data.error);
            stopStreaming();
            setLoading(false);
            return;
          }
          if (data.done) {
            setRagSources(data.sources || []);
            stopStreaming();
            setLoading(false);
            return;
          }
          if (data.token) {
            setRagAnswer((prev) => prev + data.token);
            if (ragAnswerRef.current) {
              ragAnswerRef.current.scrollTop = ragAnswerRef.current.scrollHeight;
            }
          }
        } catch {
          // ignore parse errors
        }
      };

      es.onerror = () => {
        setError("კავშირი შეწყდა");
        stopStreaming();
        setLoading(false);
      };
    } else {
      setLoading(true);
      try {
        const searchFn = mode === "keyword" ? keywordSearch : semanticSearch;
        const response = await searchFn(query);
        setResults(response);
      } catch {
        setError("ძიება ვერ მოხერხდა");
      } finally {
        setLoading(false);
      }
    }
  };

  return (
    <div>
      {/* Mode Tabs */}
      <div className="flex items-center gap-1 mb-6 bg-muted p-1 rounded-lg w-fit">
        {modes.map(({ key, label, icon: Icon }) => (
          <button
            key={key}
            onClick={() => {
              setMode(key);
              setResults(null);
              setRagAnswer("");
              setRagSources([]);
              setError(null);
              stopStreaming();
            }}
            className={`flex items-center gap-2 px-4 py-2 rounded-md text-sm font-medium transition-colors ${
              mode === key
                ? "bg-card text-foreground shadow-sm"
                : "text-muted-foreground hover:text-foreground"
            }`}
          >
            <Icon className="w-4 h-4" />
            {label}
          </button>
        ))}
      </div>

      {/* Search Form */}
      <form onSubmit={handleSearch} className="mb-6">
        <div className="flex gap-3">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
            <input
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="შეიყვანეთ საძიებო ტექსტი..."
              className="w-full pl-10 pr-4 py-3 border border-input rounded-lg bg-card text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent"
            />
          </div>
          <button
            type="submit"
            disabled={loading || !query.trim()}
            className="px-6 py-3 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:bg-primary-hover disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-2"
          >
            {loading ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : (
              <Zap className="w-4 h-4" />
            )}
            ძიება
          </button>
        </div>

      </form>

      {/* Error */}
      {error && (
        <div className="mb-4 p-3 bg-red-50 text-red-700 rounded-lg text-sm">
          {error}
        </div>
      )}

      {/* RAG Answer */}
      {mode === "rag" && (ragAnswer || ragStreaming) && (
        <div className="mb-6">
          <div
            ref={ragAnswerRef}
            className="bg-card border border-border rounded-lg p-5 max-h-96 overflow-y-auto"
          >
            <div className="prose prose-sm max-w-none whitespace-pre-wrap">
              {ragAnswer}
              {ragStreaming && (
                <span className="inline-block w-2 h-4 bg-primary animate-pulse ml-0.5" />
              )}
            </div>
          </div>
          {ragSources.length > 0 && (
            <div className="mt-3 flex items-center gap-2 flex-wrap">
              <span className="text-xs text-muted-foreground">წყაროები:</span>
              {ragSources.map((source, i) => (
                <span
                  key={i}
                  className="px-2 py-0.5 bg-primary/10 text-primary rounded-full text-xs font-medium"
                >
                  {source}
                </span>
              ))}
            </div>
          )}
          {ragStreaming && (
            <button
              onClick={() => {
                stopStreaming();
                setLoading(false);
              }}
              className="mt-3 px-4 py-1.5 text-sm border border-border rounded-lg hover:bg-accent transition-colors"
            >
              შეწყვეტა
            </button>
          )}
        </div>
      )}

      {/* Search Results */}
      {results && (
        <div>
          <div className="flex items-center justify-between mb-4">
            <span className="text-sm text-muted-foreground">
              მოიძებნა {results.results.length} შედეგი, {results.latencyMs} მწ
            </span>
            <span className="text-xs text-muted-foreground px-2 py-1 bg-muted rounded-full">
              {results.mode}
            </span>
          </div>

          {results.results.length === 0 ? (
            <div className="text-center py-12 text-muted-foreground">
              შედეგები ვერ მოიძებნა
            </div>
          ) : (
            <div className="space-y-3">
              {results.results.map((result) => (
                <ResultCard key={result.chunkId} result={result} query={query} />
              ))}
            </div>
          )}
        </div>
      )}

      {/* Initial state */}
      {!loading && !results && !ragAnswer && !error && (
        <div className="text-center py-16 text-muted-foreground">
          <Search className="w-12 h-12 mx-auto mb-4 opacity-30" />
          <p>შეიყვანეთ საძიებო ტექსტი ძიების დასაწყებად</p>
        </div>
      )}
    </div>
  );
}

function ResultCard({ result, query }: { result: SearchResult; query: string }) {
  return (
    <Link
      href={`/documents/${result.documentId}?chunk=${result.chunkId}&q=${encodeURIComponent(query)}`}
      className="block bg-card border border-border rounded-lg p-4 hover:shadow-md hover:border-primary/30 transition-all"
    >
      <div className="flex items-start justify-between mb-2">
        <div>
          <h3 className="text-sm font-semibold">{result.documentTitle}</h3>
          {result.articleNumber && (
            <span className="text-xs text-primary font-medium">
              მუხლი {result.articleNumber}
            </span>
          )}
        </div>
        <span className="text-xs px-2 py-0.5 bg-muted rounded-full text-muted-foreground flex-shrink-0">
          {(result.score * 100).toFixed(1)}%
        </span>
      </div>
      <p className="text-sm text-muted-foreground leading-relaxed line-clamp-4">
        <HighlightedText text={result.chunkText} query={query} />
      </p>
    </Link>
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
