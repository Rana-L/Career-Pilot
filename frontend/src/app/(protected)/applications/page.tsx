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
import { STATUS_BADGE_STYLES } from "@/lib/statusStyles";

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
    <div className="flex flex-1 flex-col gap-6 p-6 sm:p-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 dark:text-white">
            Applications
          </h1>
          <p className="mt-1 text-sm text-zinc-500 dark:text-zinc-400">
            Track every role you&apos;ve applied to in one place.
          </p>
        </div>
        <Link
          href="/applications/new"
          className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-indigo-500"
        >
          + Add application
        </Link>
      </div>

      {error && (
        <p className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-600 dark:bg-red-950/50 dark:text-red-400">
          {error}
        </p>
      )}

      {isLoading ? (
        <p className="text-zinc-500 dark:text-zinc-400">Loading...</p>
      ) : applications.length === 0 ? (
        <div className="flex flex-col items-center gap-3 rounded-xl border border-dashed border-zinc-300 bg-white py-16 text-center dark:border-zinc-700 dark:bg-zinc-900">
          <p className="text-zinc-500 dark:text-zinc-400">
            No applications yet.
          </p>
          <Link
            href="/applications/new"
            className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-indigo-500"
          >
            Add your first application
          </Link>
        </div>
      ) : (
        <div className="flex flex-col gap-3">
          {applications.map((app) => (
            <div
              key={app.id}
              className="flex flex-col gap-3 rounded-xl border border-zinc-200 bg-white p-5 shadow-sm sm:flex-row sm:items-center sm:justify-between dark:border-zinc-800 dark:bg-zinc-900"
            >
              <div className="flex flex-col gap-1.5">
                <div className="flex flex-wrap items-center gap-2">
                  <p className="font-medium text-zinc-900 dark:text-white">
                    {app.jobTitle}
                  </p>
                  <span
                    className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${STATUS_BADGE_STYLES[app.status]}`}
                  >
                    {APPLICATION_STATUSES[app.status]}
                  </span>
                </div>
                <p className="text-sm text-zinc-500 dark:text-zinc-400">
                  {app.companyName}
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
                  className="rounded-lg border border-zinc-300 px-2.5 py-1.5 text-sm text-zinc-700 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:outline-none dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
                >
                  {APPLICATION_STATUSES.map((label, index) => (
                    <option key={label} value={index}>
                      {label}
                    </option>
                  ))}
                </select>

                <button
                  onClick={() => handleDelete(app.id)}
                  className="text-sm font-medium text-red-600 transition-colors hover:text-red-500 dark:text-red-400"
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
