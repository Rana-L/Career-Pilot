"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "@/context/AuthContext";
import { createApplication, parseJobPosting } from "@/lib/api";

const inputClasses =
  "rounded-lg border border-zinc-300 px-3 py-2 text-zinc-900 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:outline-none dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50";

export default function NewApplicationPage() {
  const { token } = useAuth();
  const router = useRouter();
  const [companyName, setCompanyName] = useState("");
  const [jobTitle, setJobTitle] = useState("");
  const [jobDescription, setJobDescription] = useState("");
  const [notes, setNotes] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [pastedPosting, setPastedPosting] = useState("");
  const [isParsing, setIsParsing] = useState(false);

  async function handleParse() {
    if (!token || !pastedPosting.trim()) return;
    setIsParsing(true);
    setError(null);
    try {
      const parsed = await parseJobPosting(token, pastedPosting);
      setCompanyName(parsed.companyName);
      setJobTitle(parsed.jobTitle);
      setJobDescription(parsed.jobDescription);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to parse job posting");
    } finally {
      setIsParsing(false);
    }
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!token) return;

    setError(null);
    setIsSubmitting(true);
    try {
      await createApplication(token, {
        companyName,
        jobTitle,
        jobDescription: jobDescription || undefined,
        notes: notes || undefined,
      });
      router.push("/applications");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="flex flex-1 flex-col items-center p-6 sm:p-8">
      <form
        onSubmit={handleSubmit}
        className="flex w-full max-w-lg flex-col gap-4 rounded-xl border border-zinc-200 bg-white p-8 shadow-sm dark:border-zinc-800 dark:bg-zinc-900"
      >
        <div>
          <Link
            href="/applications"
            className="text-sm text-zinc-500 hover:text-zinc-700 dark:text-zinc-400 dark:hover:text-zinc-200"
          >
            ← Back to applications
          </Link>
          <h1 className="mt-2 text-2xl font-semibold tracking-tight text-zinc-900 dark:text-white">
            Add application
          </h1>
        </div>

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600 dark:bg-red-950/50 dark:text-red-400">
            {error}
          </p>
        )}

        <div className="flex flex-col gap-2 rounded-lg border border-dashed border-zinc-300 bg-zinc-50 p-4 dark:border-zinc-700 dark:bg-zinc-950">
          <p className="text-sm font-medium text-zinc-700 dark:text-zinc-300">
            Paste a job posting to auto-fill
          </p>
          <textarea
            value={pastedPosting}
            onChange={(e) => setPastedPosting(e.target.value)}
            rows={4}
            placeholder="Copy the job posting text from LinkedIn, Indeed, a careers page, etc. and paste it here."
            className={inputClasses}
          />
          <button
            type="button"
            onClick={handleParse}
            disabled={!pastedPosting.trim() || isParsing}
            className="self-start rounded-lg border border-indigo-200 bg-indigo-50 px-3 py-1.5 text-sm font-medium text-indigo-700 transition-colors hover:bg-indigo-100 disabled:opacity-50 dark:border-indigo-500/20 dark:bg-indigo-500/10 dark:text-indigo-400 dark:hover:bg-indigo-500/20"
          >
            {isParsing ? "Parsing..." : "Parse and fill fields"}
          </button>
        </div>

        <label className="flex flex-col gap-1 text-sm font-medium text-zinc-700 dark:text-zinc-300">
          Company name
          <input
            type="text"
            required
            value={companyName}
            onChange={(e) => setCompanyName(e.target.value)}
            className={inputClasses}
          />
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-zinc-700 dark:text-zinc-300">
          Job title
          <input
            type="text"
            required
            value={jobTitle}
            onChange={(e) => setJobTitle(e.target.value)}
            className={inputClasses}
          />
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-zinc-700 dark:text-zinc-300">
          Job description (optional)
          <textarea
            value={jobDescription}
            onChange={(e) => setJobDescription(e.target.value)}
            rows={4}
            className={inputClasses}
          />
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-zinc-700 dark:text-zinc-300">
          Notes (optional)
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
            className={inputClasses}
          />
        </label>

        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded-lg bg-indigo-600 px-4 py-2 font-medium text-white shadow-sm transition-colors hover:bg-indigo-500 disabled:opacity-50"
        >
          {isSubmitting ? "Adding..." : "Add application"}
        </button>
      </form>
    </div>
  );
}
