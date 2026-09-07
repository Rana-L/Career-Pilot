"use client";

import { useEffect, useState } from "react";
import { useAuth } from "@/context/AuthContext";
import {
  getCvs,
  uploadCv,
  deleteCv,
  getCvDownloadUrl,
  analyzeCv,
  getApplications,
  type Cv,
  type JobApplication,
  type CvAnalysisResult,
} from "@/lib/api";
import { scoreBarColor, scoreColor } from "@/lib/statusStyles";

export default function CvPage() {
  const { token } = useAuth();
  const [cvs, setCvs] = useState<Cv[]>([]);
  const [applications, setApplications] = useState<JobApplication[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const [selectedJobByCv, setSelectedJobByCv] = useState<Record<number, number>>({});
  const [analyzingCvId, setAnalyzingCvId] = useState<number | null>(null);
  const [results, setResults] = useState<Record<number, CvAnalysisResult>>({});

  useEffect(() => {
    if (!token) return;
    Promise.all([getCvs(token), getApplications(token)])
      .then(([cvsData, applicationsData]) => {
        setCvs(cvsData);
        setApplications(applicationsData);
      })
      .catch((err) => setError(err instanceof Error ? err.message : "Failed to load"))
      .finally(() => setIsLoading(false));
  }, [token]);

  async function handleUpload() {
    if (!token || !selectedFile) return;
    setIsUploading(true);
    setError(null);
    try {
      const cv = await uploadCv(token, selectedFile);
      setCvs((prev) => [...prev, cv]);
      setSelectedFile(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to upload");
    } finally {
      setIsUploading(false);
    }
  }

  async function handleDelete(id: number) {
    if (!token) return;
    try {
      await deleteCv(token, id);
      setCvs((prev) => prev.filter((cv) => cv.id !== id));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete");
    }
  }

  async function handleDownload(id: number) {
    if (!token) return;
    try {
      const url = await getCvDownloadUrl(token, id);
      window.open(url, "_blank");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to get download link");
    }
  }

  async function handleAnalyze(cvId: number) {
    if (!token) return;
    const jobApplicationId = selectedJobByCv[cvId];
    if (!jobApplicationId) return;

    setAnalyzingCvId(cvId);
    setError(null);
    try {
      const result = await analyzeCv(token, cvId, jobApplicationId);
      setResults((prev) => ({ ...prev, [cvId]: result }));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to analyse");
    } finally {
      setAnalyzingCvId(null);
    }
  }

  return (
    <div className="flex flex-1 flex-col gap-6 p-6 sm:p-8">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-zinc-900 dark:text-white">
          CVs
        </h1>
        <p className="mt-1 text-sm text-zinc-500 dark:text-zinc-400">
          Upload a CV and check how well it matches a role you&apos;re applying to.
        </p>
      </div>

      {error && (
        <p className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-600 dark:bg-red-950/50 dark:text-red-400">
          {error}
        </p>
      )}

      <div className="flex flex-col gap-3 rounded-xl border border-dashed border-zinc-300 bg-white p-6 dark:border-zinc-700 dark:bg-zinc-900">
        <p className="text-sm font-medium text-zinc-700 dark:text-zinc-300">
          Upload a new CV
        </p>
        <div className="flex flex-col items-start gap-3 sm:flex-row sm:items-center">
          <input
            type="file"
            onChange={(e) => setSelectedFile(e.target.files?.[0] ?? null)}
            className="text-sm text-zinc-600 file:mr-3 file:rounded-lg file:border-0 file:bg-zinc-100 file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-zinc-700 dark:text-zinc-400 dark:file:bg-zinc-800 dark:file:text-zinc-200"
          />
          <button
            onClick={handleUpload}
            disabled={!selectedFile || isUploading}
            className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-indigo-500 disabled:opacity-50"
          >
            {isUploading ? "Uploading..." : "Upload"}
          </button>
        </div>
      </div>

      {isLoading ? (
        <p className="text-zinc-500 dark:text-zinc-400">Loading...</p>
      ) : cvs.length === 0 ? (
        <p className="text-zinc-500 dark:text-zinc-400">No CVs uploaded yet.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {cvs.map((cv) => {
            const result = results[cv.id];
            return (
              <div
                key={cv.id}
                className="flex flex-col gap-4 rounded-xl border border-zinc-200 bg-white p-5 shadow-sm dark:border-zinc-800 dark:bg-zinc-900"
              >
                <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    <p className="font-medium text-zinc-900 dark:text-white">{cv.fileName}</p>
                    <p className="text-sm text-zinc-500 dark:text-zinc-400">
                      Uploaded {new Date(cv.uploadedAt).toLocaleString()}
                    </p>
                  </div>
                  <div className="flex items-center gap-4">
                    <button
                      onClick={() => handleDownload(cv.id)}
                      className="text-sm font-medium text-zinc-600 hover:text-zinc-900 dark:text-zinc-400 dark:hover:text-zinc-50"
                    >
                      Download
                    </button>
                    <button
                      onClick={() => handleDelete(cv.id)}
                      className="text-sm font-medium text-red-600 hover:text-red-500 dark:text-red-400"
                    >
                      Delete
                    </button>
                  </div>
                </div>

                {applications.length > 0 && (
                  <div className="flex flex-col gap-2 border-t border-zinc-100 pt-4 sm:flex-row sm:items-center dark:border-zinc-800">
                    <select
                      value={selectedJobByCv[cv.id] ?? ""}
                      onChange={(e) =>
                        setSelectedJobByCv((prev) => ({
                          ...prev,
                          [cv.id]: Number(e.target.value),
                        }))
                      }
                      className="rounded-lg border border-zinc-300 px-2.5 py-1.5 text-sm text-zinc-700 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 focus:outline-none dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-50"
                    >
                      <option value="">Select a job application...</option>
                      {applications.map((app) => (
                        <option key={app.id} value={app.id}>
                          {app.jobTitle} — {app.companyName}
                        </option>
                      ))}
                    </select>
                    <button
                      onClick={() => handleAnalyze(cv.id)}
                      disabled={!selectedJobByCv[cv.id] || analyzingCvId === cv.id}
                      className="rounded-lg border border-indigo-200 bg-indigo-50 px-3 py-1.5 text-sm font-medium text-indigo-700 transition-colors hover:bg-indigo-100 disabled:opacity-50 dark:border-indigo-500/20 dark:bg-indigo-500/10 dark:text-indigo-400 dark:hover:bg-indigo-500/20"
                    >
                      {analyzingCvId === cv.id ? "Analysing..." : "Analyse match"}
                    </button>
                  </div>
                )}

                {result && (
                  <div className="rounded-lg bg-zinc-50 p-4 dark:bg-zinc-950">
                    <div className="flex items-center justify-between">
                      <p className="text-sm font-medium text-zinc-700 dark:text-zinc-300">
                        Match score
                      </p>
                      <p className={`text-lg font-semibold ${scoreColor(result.matchScore)}`}>
                        {result.matchScore}%
                      </p>
                    </div>
                    <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-zinc-200 dark:bg-zinc-800">
                      <div
                        className={`h-full rounded-full ${scoreBarColor(result.matchScore)}`}
                        style={{ width: `${Math.min(100, Math.max(0, result.matchScore))}%` }}
                      />
                    </div>
                    {result.missingSkills && (
                      <p className="mt-3 text-sm text-zinc-600 dark:text-zinc-400">
                        <span className="font-medium text-zinc-700 dark:text-zinc-300">
                          Missing:
                        </span>{" "}
                        {result.missingSkills}
                      </p>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
