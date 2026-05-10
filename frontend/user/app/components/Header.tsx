"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Scale, Search, FileText } from "lucide-react";

const navLinks = [
  { href: "/", label: "ძიება", icon: Search },
  { href: "/documents", label: "დოკუმენტები", icon: FileText },
];

export default function Header() {
  const pathname = usePathname();

  return (
    <header className="border-b border-border bg-card">
      <div className="max-w-6xl mx-auto px-6 flex items-center justify-between h-16">
        <Link href="/" className="flex items-center gap-2 text-primary font-semibold text-lg">
          <Scale className="w-6 h-6" />
          <span>სამართლებრივი ძიება</span>
        </Link>
        <nav className="flex items-center gap-1">
          {navLinks.map(({ href, label, icon: Icon }) => {
            const isActive = pathname === href;
            return (
              <Link
                key={href}
                href={href}
                className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                  isActive
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                }`}
              >
                <Icon className="w-4 h-4" />
                {label}
              </Link>
            );
          })}
        </nav>
      </div>
    </header>
  );
}
