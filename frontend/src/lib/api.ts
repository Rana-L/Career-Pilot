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
  const res = await fetch(`${API_URL}/api/dashboard/summary`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!res.ok) {
    throw new Error("Failed to load dashboard summary");
  }

  return res.json();
}
