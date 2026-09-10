const API_URL = process.env.NEXT_PUBLIC_API_URL;

export interface AuthResponse {
  token: string;
  email: string;
}

export async function registerUser(
  email: string,
  password: string,
): Promise<AuthResponse> {
  const res = await fetch(`${API_URL}/api/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  if (!res.ok) {
    const message = await res.text();
    throw new Error(message || "Registration failed");
  }

  return res.json();
}

export async function loginUser(
  email: string,
  password: string,
): Promise<AuthResponse> {
  const res = await fetch(`${API_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  if (!res.ok) {
    const message = await res.text();
    throw new Error(message || "Login failed");
  }

  return res.json();
}

async function authFetch(
  path: string,
  token: string,
  options: RequestInit = {},
): Promise<Response> {
  return fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
      ...options.headers,
    },
  });
}

export interface DashboardSummary {
  wishlist: number;
  applied: number;
  assessment: number;
  interview: number;
  offer: number;
  rejected: number;
  total: number;
}

export async function getDashboardSummary(
  token: string,
): Promise<DashboardSummary> {
  const res = await authFetch("/api/dashboard/summary", token);
  if (!res.ok) throw new Error("Failed to load dashboard summary");
  return res.json();
}

export const APPLICATION_STATUSES = [
  "Wishlist",
  "Applied",
  "Assessment",
  "Interview",
  "Offer",
  "Rejected",
] as const;

export interface JobApplication {
  id: number;
  companyName: string;
  jobTitle: string;
  jobDescription: string | null;
  status: number;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateApplicationInput {
  companyName: string;
  jobTitle: string;
  jobDescription?: string;
  notes?: string;
}

export interface UpdateApplicationInput extends CreateApplicationInput {
  status: number;
}

export async function getApplications(
  token: string,
): Promise<JobApplication[]> {
  const res = await authFetch("/api/applications", token);
  if (!res.ok) throw new Error("Failed to load applications");
  return res.json();
}

export async function getApplication(
  token: string,
  id: number,
): Promise<JobApplication> {
  const res = await authFetch(`/api/applications/${id}`, token);
  if (!res.ok) throw new Error("Failed to load application");
  return res.json();
}

export async function createApplication(
  token: string,
  input: CreateApplicationInput,
): Promise<JobApplication> {
  const res = await authFetch("/api/applications", token, {
    method: "POST",
    body: JSON.stringify(input),
  });
  if (!res.ok) throw new Error("Failed to create application");
  return res.json();
}

export async function updateApplication(
  token: string,
  id: number,
  input: UpdateApplicationInput,
): Promise<void> {
  const res = await authFetch(`/api/applications/${id}`, token, {
    method: "PUT",
    body: JSON.stringify(input),
  });
  if (!res.ok) throw new Error("Failed to update application");
}

export async function deleteApplication(
  token: string,
  id: number,
): Promise<void> {
  const res = await authFetch(`/api/applications/${id}`, token, {
    method: "DELETE",
  });
  if (!res.ok) throw new Error("Failed to delete application");
}

export interface Cv {
  id: number;
  fileName: string;
  uploadedAt: string;
}

export async function uploadCv(token: string, file: File): Promise<Cv> {
  const formData = new FormData();
  formData.append("file", file);

  const res = await fetch(`${API_URL}/api/cv/upload`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: formData,
  });

  if (!res.ok) {
    const message = await res.text();
    throw new Error(message || "Failed to upload CV");
  }
  return res.json();
}

export async function getCvs(token: string): Promise<Cv[]> {
  const res = await authFetch("/api/cv", token);
  if (!res.ok) throw new Error("Failed to load CVs");
  return res.json();
}

export async function deleteCv(token: string, id: number): Promise<void> {
  const res = await authFetch(`/api/cv/${id}`, token, { method: "DELETE" });
  if (!res.ok) throw new Error("Failed to delete CV");
}

export async function getCvDownloadUrl(
  token: string,
  id: number,
): Promise<string> {
  const res = await authFetch(`/api/cv/${id}/download`, token);
  if (!res.ok) throw new Error("Failed to get download link");
  const data = await res.json();
  return data.url;
}

export interface CvAnalysisResult {
  id: number;
  cvId: number;
  jobApplicationId: number;
  matchScore: number;
  missingSkills: string | null;
  createdAt: string;
}

export async function analyzeCv(
  token: string,
  cvId: number,
  jobApplicationId: number,
): Promise<CvAnalysisResult> {
  const res = await authFetch(
    `/api/cv/${cvId}/analyze/${jobApplicationId}`,
    token,
    {
      method: "POST",
    },
  );
  if (!res.ok) {
    const message = await res.text();
    throw new Error(message || "Failed to analyze CV");
  }
  return res.json();
}

export async function getCvAnalyses(
  token: string,
  cvId: number,
): Promise<CvAnalysisResult[]> {
  const res = await authFetch(`/api/cv/${cvId}/analyses`, token);
  if (!res.ok) throw new Error("Failed to load analysis history");
  return res.json();
}

export interface CvRewriteResult {
  rewrittenCv: string;
}

export async function rewriteCv(
  token: string,
  cvId: number,
  jobApplicationId: number,
): Promise<CvRewriteResult> {
  const res = await authFetch(
    `/api/cv/${cvId}/rewrite/${jobApplicationId}`,
    token,
    {
      method: "POST",
    },
  );
  if (!res.ok) {
    const message = await res.text();
    throw new Error(message || "Failed to rewrite CV");
  }
  return res.json();
}

export async function downloadDocument(
  token: string,
  markdown: string,
  fileName: string,
  format: "pdf" | "docx",
): Promise<void> {
  const res = await fetch(`${API_URL}/api/documents/${format}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ markdown, fileName }),
  });
  if (!res.ok) throw new Error("Failed to generate document");

  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `${fileName}.${format}`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
