"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/context/AuthContext";
import {
  getApplications,
  deleteApplication,
  updateApplication,
  APPLICATION_STATUSES,
  type JobApplication,
} from "@/lib/api";

export default function ApplicationsPage() {
  const { token } = useAuth();
  const [applications, setApplications] = useState<JobApplication[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!token) return;

    getApplications(token)
      .then(setApplications)
      .catch((err) =>
        setError(err instanceof Error ? err.message : "Failed to load"),
      )
      .finally(() => setIsLoading(false));
  }, [token]);

  async function handleStatusChange(app: JobApplication, newStatus: number) {
    if (!token) return;
    try {
      await updateApplication(token, app.id, {
        companyName: app.companyName,
        jobTitle: app.jobTitle,
        jobDescription: app.jobDescription ?? undefined,
        notes: app.notes ?? undefined,
        status: newStatus,
      });
      setApplications((prev) =>
        prev.map((a) => (a.id === app.id ? { ...a, status: newStatus } : a)),
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update");
    }
  }

  async function handleDelete(id: number) {
    if (!token) return;
    try {
      await deleteApplication(token, id);
      setApplications((prev) => prev.filter((a) => a.id !== id));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete");
    }
  }

  return (
    <div className="flex flex-1 flex-col gap-6 p-8">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold text-black dark:text-zinc-50">
          Applications
        </h1>
        <Link
          href="/applications/new"
          className="rounded bg-black px-4 py-2 text-sm font-medium text-white dark:bg-white dark:text-black"
        >
          + Add application
        </Link>
      </div>

      {error && <p className="text-red-600 dark:text-red-400">{error}</p>}

      {isLoading ? (
        <p className="text-zinc-500 dark:text-zinc-400">Loading...</p>
      ) : applications.length === 0 ? (
        <p className="text-zinc-500 dark:text-zinc-400">No applications yet.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {applications.map((app) => (
            <div
              key={app.id}
              className="flex flex-col gap-2 rounded-lg border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-950 sm:flex-row sm:items-center sm:justify-between"
            >
              <div>
                <p className="font-medium text-black dark:text-zinc-50">
                  {app.jobTitle} — {app.companyName}
                </p>
                {app.notes && (
                  <p className="text-sm text-zinc-500 dark:text-zinc-400">
                    {app.notes}
                  </p>
                )}
              </div>

              <div className="flex items-center gap-3">
                <select
                  value={app.status}
                  onChange={(e) =>
                    handleStatusChange(app, Number(e.target.value))
                  }
                  className="rounded border border-zinc-300 px-2 py-1 text-sm dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-50"
                >
                  {APPLICATION_STATUSES.map((label, index) => (
                    <option key={label} value={index}>
                      {label}
                    </option>
                  ))}
                </select>

                <button
                  onClick={() => handleDelete(app.id)}
                  className="text-sm text-red-600 dark:text-red-400"
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
