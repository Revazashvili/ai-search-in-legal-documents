"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Upload } from "lucide-react";
import { uploadDocument } from "@/lib/api";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";

const selectClass =
  "w-full h-8 rounded-lg border border-input bg-transparent px-2.5 py-1 text-sm outline-none transition-[color,box-shadow] focus:border-ring focus:ring-3 focus:ring-ring/50 disabled:opacity-50";

export default function UploadPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      const result = await uploadDocument(new FormData(e.currentTarget));
      router.push(`/dashboard/documents/${result.documentId}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Upload failed.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="max-w-2xl space-y-6">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Link
          href="/dashboard"
          className={cn(buttonVariants({ variant: "ghost", size: "icon-sm" }))}
        >
          <ArrowLeft size={15} />
        </Link>
        <h1 className="text-xl font-semibold">Upload Document</h1>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Document Details</CardTitle>
          <CardDescription>
            Upload a PDF or plain-text file to index for search.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* File */}
            <div className="space-y-1.5">
              <Label htmlFor="file">
                File <span className="text-destructive">*</span>
              </Label>
              <input
                id="file"
                name="file"
                type="file"
                accept=".pdf,.txt"
                required
                className="block w-full text-sm text-foreground file:mr-3 file:h-7 file:cursor-pointer file:rounded-md file:border file:border-border file:bg-muted file:px-2.5 file:text-xs file:font-medium file:text-foreground hover:file:bg-accent"
              />
            </div>

            {/* Title */}
            <div className="space-y-1.5">
              <Label htmlFor="title">
                Title <span className="text-destructive">*</span>
              </Label>
              <Input
                id="title"
                name="title"
                placeholder="Labor Code of Georgia"
                required
              />
            </div>

            {/* Source law name */}
            <div className="space-y-1.5">
              <Label htmlFor="sourceLawName">
                Source Law Name <span className="text-destructive">*</span>
              </Label>
              <Input
                id="sourceLawName"
                name="sourceLawName"
                placeholder="Labor Code of Georgia"
                required
              />
            </div>

            {/* Document type + chunking strategy */}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label htmlFor="documentType">
                  Document Type <span className="text-destructive">*</span>
                </Label>
                <select
                  id="documentType"
                  name="documentType"
                  required
                  defaultValue=""
                  className={selectClass}
                >
                  <option value="" disabled>
                    Select type…
                  </option>
                  <option value="Law">Law</option>
                  <option value="Code">Code</option>
                  <option value="Regulation">Regulation</option>
                  <option value="Other">Other</option>
                </select>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="chunkingStrategy">
                  Chunking Strategy <span className="text-destructive">*</span>
                </Label>
                <select
                  id="chunkingStrategy"
                  name="chunkingStrategy"
                  required
                  defaultValue=""
                  className={selectClass}
                >
                  <option value="" disabled>
                    Select strategy…
                  </option>
                  <option value="FixedSize">Fixed Size</option>
                  <option value="ArticleLevel">Article Level</option>
                  <option value="Hierarchical">Hierarchical</option>
                </select>
              </div>
            </div>

            {/* Optional dates */}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label htmlFor="dateEnacted">Date Enacted</Label>
                <Input id="dateEnacted" name="dateEnacted" type="date" />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="lastAmended">Last Amended</Label>
                <Input id="lastAmended" name="lastAmended" type="date" />
              </div>
            </div>

            {/* Source URL */}
            <div className="space-y-1.5">
              <Label htmlFor="sourceUrl">Source URL</Label>
              <Input
                id="sourceUrl"
                name="sourceUrl"
                type="url"
                placeholder="https://parliament.ge/…"
              />
            </div>

            {error && <p className="text-sm text-destructive">{error}</p>}

            <div className="flex justify-end gap-2 pt-2">
              <Link
                href="/dashboard"
                className={cn(buttonVariants({ variant: "outline" }))}
              >
                Cancel
              </Link>
              <Button type="submit" disabled={loading}>
                {loading ? (
                  "Uploading…"
                ) : (
                  <>
                    <Upload size={14} />
                    Upload
                  </>
                )}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
