"use client";

import { useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { createApplication } from "@/lib/api";

export default function NewApplicationPage() {
  const { token } = useAuth();
  const router = useRouter();
  const [companyName, setCompanyName] = useState("");
  const [jobTitle, setJobTitle] = useState("");
  const [jobDescription, setJobDescription] = useState("");
  const [notes, setNotes] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

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
    <div className="flex flex-1 flex-col items-center p-8">
      <form
        onSubmit={handleSubmit}
        className="flex w-full max-w-lg flex-col gap-4 rounded-lg border border-zinc-200 bg-white p-8 dark:border-zinc-800 dark:bg-zinc-950"
      >
        <h1 className="text-2xl font-semibold text-black dark:text-zinc-50">
          Add application
        </h1>

        {error && (
          <p className="rounded bg-red-50 px-3 py-2 text-sm text-red-600 dark:bg-red-950 dark:text-red-400">
            {error}
          </p>
        )}

        <label className="flex flex-col gap-1 text-sm text-zinc-700 dark:text-zinc-300">
          Company name
          <input
            type="text"
            required
            value={companyName}
            onChange={(e) => setCompanyName(e.target.value)}
            className="rounded border border-zinc-300 px-3 py-2 text-black dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-50"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm text-zinc-700 dark:text-zinc-300">
          Job title
          <input
            type="text"
            required
            value={jobTitle}
            onChange={(e) => setJobTitle(e.target.value)}
            className="rounded border border-zinc-300 px-3 py-2 text-black dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-50"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm text-zinc-700 dark:text-zinc-300">
          Job description (optional)
          <textarea
            value={jobDescription}
            onChange={(e) => setJobDescription(e.target.value)}
            rows={4}
            className="rounded border border-zinc-300 px-3 py-2 text-black dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-50"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm text-zinc-700 dark:text-zinc-300">
          Notes (optional)
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
            className="rounded border border-zinc-300 px-3 py-2 text-black dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-50"
          />
        </label>

        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded bg-black px-4 py-2 font-medium text-white disabled:opacity-50 dark:bg-white dark:text-black"
        >
          {isSubmitting ? "Adding..." : "Add application"}
        </button>
      </form>
    </div>
  );
}
