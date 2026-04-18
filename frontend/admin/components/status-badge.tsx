import { cn } from "@/lib/utils";

const config: Record<string, { label: string; className: string }> = {
  Pending: {
    label: "Pending",
    className: "bg-muted text-muted-foreground",
  },
  Processing: {
    label: "Processing",
    className:
      "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400",
  },
  Ready: {
    label: "Ready",
    className:
      "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400",
  },
  Failed: {
    label: "Failed",
    className: "bg-destructive/10 text-destructive",
  },
};

export function StatusBadge({ status }: { status: string }) {
  const c = config[status] ?? config.Pending;
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium",
        c.className
      )}
    >
      {c.label}
    </span>
  );
}
