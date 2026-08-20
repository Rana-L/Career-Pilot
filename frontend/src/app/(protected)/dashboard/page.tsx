"use client";

import { useEffect, useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { getDashboardSummary, type DashboardSummary } from "@/lib/api";

export default function DashboardPage() {
  const { token, email, logout } = useAuth();
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
    <div className="flex flex-1 flex-col gap-6 p-8">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold text-black dark:text-zinc-50">
          Welcome, {email}
        </h1>
        <button
          onClick={logout}
          className="rounded border border-zinc-300 px-4 py-2 text-sm dark:border-zinc-700"
        >
          Log out
        </button>
      </div>

      {error && <p className="text-red-600 dark:text-red-400">{error}</p>}

      {summary && (
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-6">
          <StatCard label="Wishlist" value={summary.wishlist} />
          <StatCard label="Applied" value={summary.applied} />
          <StatCard label="Assessment" value={summary.assessment} />
          <StatCard label="Interview" value={summary.interview} />
          <StatCard label="Offer" value={summary.offer} />
          <StatCard label="Rejected" value={summary.rejected} />
        </div>
      )}
    </div>
  );
}

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="flex flex-col gap-1 rounded-lg border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-950">
      <span className="text-sm text-zinc-500 dark:text-zinc-400">{label}</span>
      <span className="text-2xl font-semibold text-black dark:text-zinc-50">
        {value}
      </span>
    </div>
  );
}
