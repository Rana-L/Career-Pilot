"use client";

import { useEffect, useState } from "react";
import { useAuth } from "@/context/AuthContext";
import {
  searchJobs,
  createApplication,
  getApplications,
  getSavedJobSearch,
  saveJobSearch,
  type JobSearchResult,
} from "@/lib/api";

const RADIUS_OPTIONS = [5, 10, 15, 25, 50, 100];

function applicationKey(companyName: string, jobTitle: string): string {
  return `${companyName.trim().toLowerCase()}::${jobTitle.trim().toLowerCase()}`;
}

function formatRelativeTime(dateString: string, now: Date): string {
  const posted = new Date(dateString);
  const seconds = Math.floor((now.getTime() - posted.getTime()) / 1000);

  if (seconds < 60) return "Posted just now";

  const units: [string, number][] = [
    ["year", 31536000],
    ["month", 2592000],
    ["day", 86400],
    ["hour", 3600],
    ["minute", 60],
  ];

  for (const [unit, secondsInUnit] of units) {
    const value = Math.floor(seconds / secondsInUnit);
    if (value >= 1) {
      return `Posted ${value} ${unit}${value === 1 ? "" : "s"} ago`;
    }
  }

  return "Posted just now";
}

export default function JobSearchPage() {
  const { token } = useAuth();
  const [title, setTitle] = useState("");
  const [location, setLocation] = useState("");
  const [radiusMiles, setRadiusMiles] = useState(10);
  const [results, setResults] = useState<JobSearchResult[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [isRestoring, setIsRestoring] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [hasSearched, setHasSearched] = useState(false);
  const [now, setNow] = useState(() => new Date());

  // Keep "posted X ago" labels advancing live without needing a page refresh.
  useEffect(() => {
    const interval = setInterval(() => setNow(new Date()), 60_000);
    return () => clearInterval(interval);
  }, []);

  const [addingUrl, setAddingUrl] = useState<string | null>(null);
  const [addedKeys, setAddedKeys] = useState<Set<string>>(new Set());

  async function runSearch(
    searchTitle: string,
    searchLocation: string,
    searchRadiusMiles: number,
  ) {
    if (!token) return;
    setIsSearching(true);
    setError(null);
    try {
      const jobs = await searchJobs(
        token,
        searchTitle,
        searchLocation,
        searchRadiusMiles,
      );
      setResults(jobs);
      setHasSearched(true);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to search for jobs",
      );
    } finally {
      setIsSearching(false);
    }
  }

  // Restore the last saved search from the database and re-run it fresh, so
  // navigating away (e.g. to add a job to the tracker) and coming back
  // doesn't lose the search — without caching stale results client-side.
  useEffect(() => {
    if (!token) return;
    let cancelled = false;

    async function restore() {
      try {
        const [saved, applications] = await Promise.all([
          getSavedJobSearch(token!),
          getApplications(token!),
        ]);

        if (cancelled) return;

        setAddedKeys(
          new Set(
            applications.map((a) => applicationKey(a.companyName, a.jobTitle)),
          ),
        );

        if (saved) {
          setTitle(saved.title);
          setLocation(saved.location);
          setRadiusMiles(saved.radiusMiles);
          await runSearch(saved.title, saved.location, saved.radiusMiles);
        }
      } catch {
        // Nothing saved yet, or the request failed — just start fresh.
      } finally {
        if (!cancelled) setIsRestoring(false);
      }
    }

    restore();
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function handleSearch() {
    if (!token || !title.trim() || !location.trim()) return;
    await runSearch(title, location, radiusMiles);
    try {
      await saveJobSearch(token, { title, location, radiusMiles });
    } catch {
      // Not critical if this fails — the search itself already succeeded.
    }
  }

  async function handleAddToTracker(job: JobSearchResult) {
    if (!token) return;
    setAddingUrl(job.url);
    setError(null);
    try {
      await createApplication(token, {
        companyName: job.companyName,
        jobTitle: job.title,
        jobDescription: job.description,
      });
      setAddedKeys((prev) =>
        new Set(prev).add(applicationKey(job.companyName, job.title)),
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to add to tracker");
    } finally {
      setAddingUrl(null);
    }
  }

  return (
    <div className="flex flex-1 flex-col gap-6 p-6 sm:p-8">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 dark:text-white">
          Job Search
        </h1>
        <p className="mt-1 text-sm text-zinc-500 dark:text-zinc-400">
          Search for open roles near you and add the ones you like straight to
          your tracker.
        </p>
      </div>

      {error && (
        <p className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-600 dark:bg-red-950/50 dark:text-red-400">
          {error}
        </p>
      )}

      <div className="flex flex-col gap-3 rounded-xl border border-zinc-200 bg-white p-6 shadow-sm dark:border-zinc-800 dark:bg-zinc-900 sm:flex-row sm:items-end sm:flex-wrap">
        <label className="flex flex-1 min-w-[10rem] flex-col gap-1 text-sm font-medium text-zinc-700 dark:text-zinc-300">
          Job title
          <input
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="e.g. Backend Engineer"
            className="rounded-lg border border-zinc-300 px-3 py-2 text-zinc-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:outline-none dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
          />
        </label>

        <label className="flex flex-1 min-w-[10rem] flex-col gap-1 text-sm font-medium text-zinc-700 dark:text-zinc-300">
          Location
          <input
            type="text"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
            placeholder="e.g. Manchester"
            className="rounded-lg border border-zinc-300 px-3 py-2 text-zinc-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:outline-none dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-zinc-700 dark:text-zinc-300">
          Radius
          <select
            value={radiusMiles}
            onChange={(e) => setRadiusMiles(Number(e.target.value))}
            className="rounded-lg border border-zinc-300 px-3 py-2 text-zinc-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:outline-none dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
          >
            {RADIUS_OPTIONS.map((r) => (
              <option key={r} value={r}>
                {r} miles
              </option>
            ))}
          </select>
        </label>

        <button
          onClick={handleSearch}
          disabled={!title.trim() || !location.trim() || isSearching}
          className="rounded-lg bg-indigo-600 px-5 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-indigo-500 disabled:opacity-50"
        >
          {isSearching ? "Searching..." : "Search"}
        </button>
      </div>

      {!isRestoring && hasSearched && !isSearching && results.length === 0 && (
        <p className="text-zinc-500 dark:text-zinc-400">
          No results — try a different title, location, or a wider radius.
        </p>
      )}

      <div className="flex flex-col gap-3">
        {results.map((job) => {
          const isAdded = addedKeys.has(
            applicationKey(job.companyName, job.title),
          );
          return (
            <div
              key={job.url}
              className="flex flex-col gap-2 rounded-xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900"
            >
              <div className="flex flex-col gap-1 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <p className="font-medium text-zinc-900 dark:text-white">
                    {job.title}
                  </p>
                  <p className="text-sm text-zinc-500 dark:text-zinc-400">
                    {job.companyName} — {job.location}
                  </p>
                </div>
                <p
                  className="text-xs text-zinc-400 dark:text-zinc-500"
                  title={new Date(job.created).toLocaleString()}
                >
                  {formatRelativeTime(job.created, now)}
                </p>
              </div>

              <p className="line-clamp-3 text-sm text-zinc-600 dark:text-zinc-400">
                {job.description}
              </p>

              <div className="mt-2 flex items-center gap-4">
                <a
                  href={job.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-sm font-medium text-indigo-600 hover:text-indigo-500 dark:text-indigo-400"
                >
                  View listing ↗
                </a>
                <button
                  onClick={() => handleAddToTracker(job)}
                  disabled={isAdded || addingUrl === job.url}
                  className="rounded-lg border border-zinc-300 px-3 py-1.5 text-sm font-medium text-zinc-700 transition-colors hover:bg-zinc-100 disabled:opacity-50 dark:border-zinc-700 dark:text-zinc-300 dark:hover:bg-zinc-800"
                >
                  {isAdded
                    ? "Added ✓"
                    : addingUrl === job.url
                      ? "Adding..."
                      : "Add to tracker"}
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
