const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5081";

export async function login(email: string, password: string): Promise<void> {
  const res = await fetch(`${API_BASE}/api/admin/login?useCookies=true`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    body: JSON.stringify({ email, password }),
  });

  if (!res.ok) {
    throw new Error("Invalid email or password");
  }
}

export async function logout(): Promise<void> {
  await fetch(`${API_BASE}/api/admin/logout`, {
    method: "POST",
    credentials: "include",
  });
}
