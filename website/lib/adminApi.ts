// Appels API admin (login + écriture). Client uniquement.
const API = process.env.NEXT_PUBLIC_API_URL;

export function adminApiBase(): string | null {
  return API && API.length > 0 ? API.replace(/\/$/, '') : null;
}

export async function adminLogin(password: string): Promise<string | null> {
  const base = adminApiBase();
  if (!base) return null;
  try {
    const res = await fetch(`${base}/api/admin/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ password }),
    });
    if (!res.ok) return null;
    const json = (await res.json()) as { token?: string };
    return json.token ?? null;
  } catch {
    return null;
  }
}

export async function adminSave(kind: string, data: unknown, token: string): Promise<boolean> {
  const base = adminApiBase();
  if (!base) return false;
  try {
    const res = await fetch(`${base}/api/admin/content/${kind}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify(data),
    });
    return res.ok;
  } catch {
    return false;
  }
}
