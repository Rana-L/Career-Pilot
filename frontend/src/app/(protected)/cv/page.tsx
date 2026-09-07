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

export default function CvPage() {
  const { token } = useAuth();
  const [cvs, setCvs] = useState<Cv[]>([]);
  const [applications, setApplications] = useState<JobApplication[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const [selectedJobByCv, setSelectedJobByCv] = useState<
    Record<number, number>
  >({});
  const [analyzingCvId, setAnalyzingCvId] = useState<number | null>(null);
  const [results, setResults] = useState<Record<number, CvAnalysisResult>>({});

  useEffect(() => {
    if (!token) return;
    Promise.all([getCvs(token), getApplications(token)])
      .then(([cvsData, applicationsData]) => {
        setCvs(cvsData);
        setApplications(applicationsData);
      })
      .catch((err) =>
        setError(err instanceof Error ? err.message : "Failed to load"),
      )
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
      setError(
        err instanceof Error ? err.message : "Failed to get download link",
      );
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
      setError(err instanceof Error ? err.message : "Failed to analyze");
    } finally {
      setAnalyzingCvId(null);
    }
  }

  return (
    <div className="flex flex-1 flex-col gap-6 p-8">
      <h1 className="text-2xl font-semibold text-black dark:text-zinc-50">
        CVs
      </h1>

      {error && <p className="text-red-600 dark:text-red-400">{error}</p>}

      <div className="flex items-center gap-3">
        <input
          type="file"
          onChange={(e) => setSelectedFile(e.target.files?.[0] ?? null)}
          className="text-sm text-zinc-600 dark:text-zinc-400"
        />
        <button
          onClick={handleUpload}
          disabled={!selectedFile || isUploading}
          className="rounded bg-black px-4 py-2 text-sm font-medium text-white disabled:opacity-50 dark:bg-white dark:text-black"
        >
          {isUploading ? "Uploading..." : "Upload"}
        </button>
      </div>

      {isLoading ? (
        <p className="text-zinc-500 dark:text-zinc-400">Loading...</p>
      ) : cvs.length === 0 ? (
        <p className="text-zinc-500 dark:text-zinc-400">No CVs uploaded yet.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {cvs.map((cv) => (
            <div
              key={cv.id}
              className="flex flex-col gap-3 rounded-lg border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-950"
            >
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="font-medium text-black dark:text-zinc-50">
                    {cv.fileName}
                  </p>
                  <p className="text-sm text-zinc-500 dark:text-zinc-400">
                    Uploaded {new Date(cv.uploadedAt).toLocaleString()}
                  </p>
                </div>
                <div className="flex items-center gap-3">
                  <button
                    onClick={() => handleDownload(cv.id)}
                    className="text-sm text-black dark:text-zinc-50"
                  >
                    Download
                  </button>
                  <button
                    onClick={() => handleDelete(cv.id)}
                    className="text-sm text-red-600 dark:text-red-400"
                  >
                    Delete
                  </button>
                </div>
              </div>

              {applications.length > 0 && (
                <div className="flex flex-col gap-2 border-t border-zinc-200 pt-3 dark:border-zinc-800 sm:flex-row sm:items-center">
                  <select
                    value={selectedJobByCv[cv.id] ?? ""}
                    onChange={(e) =>
                      setSelectedJobByCv((prev) => ({
                        ...prev,
                        [cv.id]: Number(e.target.value),
                      }))
                    }
                    className="rounded border border-zinc-300 px-2 py-1 text-sm dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-50"
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
                    disabled={
                      !selectedJobByCv[cv.id] || analyzingCvId === cv.id
                    }
                    className="rounded border border-zinc-300 px-3 py-1 text-sm disabled:opacity-50 dark:border-zinc-700 dark:text-zinc-50"
                  >
                    {analyzingCvId === cv.id ? "Analysing..." : "Analyse match"}
                  </button>
                </div>
              )}

              {results[cv.id] && (
                <div className="rounded bg-zinc-50 p-3 text-sm dark:bg-zinc-900">
                  <p className="font-medium text-black dark:text-zinc-50">
                    Match score: {results[cv.id].matchScore}%
                  </p>
                  {results[cv.id].missingSkills && (
                    <p className="text-zinc-600 dark:text-zinc-400">
                      Missing: {results[cv.id].missingSkills}
                    </p>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
