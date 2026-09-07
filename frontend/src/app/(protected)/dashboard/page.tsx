"use client";

import { useEffect, useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { getDashboardSummary, type DashboardSummary } from "@/lib/api";

const STAT_CONFIG: {
  key: keyof DashboardSummary;
  label: string;
  accent: string;
}[] = [
  { key: "wishlist", label: "Wishlist", accent: "bg-zinc-400" },
  { key: "applied", label: "Applied", accent: "bg-blue-500" },
  { key: "assessment", label: "Assessment", accent: "bg-amber-500" },
  { key: "interview", label: "Interview", accent: "bg-purple-500" },
  { key: "offer", label: "Offer", accent: "bg-green-500" },
  { key: "rejected", label: "Rejected", accent: "bg-red-500" },
];

export default function DashboardPage() {
  const { email, token } = useAuth();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!token) return;

    getDashboardSummary(token)
      .then(setSummary)
      .catch((err) =>
        setError(err instanceof Error ? err.message : "Failed to load"),
      );
  }, [token]);

  return (
    <div className="flex flex-1 flex-col gap-8 p-6 sm:p-8">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 dark:text-white">
          Welcome back
        </h1>
        <p className="mt-1 text-sm text-zinc-500 dark:text-zinc-400">{email}</p>
      </div>

      {error && (
        <p className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-600 dark:bg-red-950/50 dark:text-red-400">
          {error}
        </p>
      )}

      {summary && (
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6">
          {STAT_CONFIG.map(({ key, label, accent }) => (
            <div
              key={key}
              className="flex flex-col gap-3 rounded-xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900"
            >
              <span className={`h-2 w-2 rounded-full ${accent}`} />
              <div>
                <p className="text-3xl font-semibold text-zinc-900 dark:text-white">
                  {summary[key]}
                </p>
                <p className="text-sm text-zinc-500 dark:text-zinc-400">{label}</p>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
