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

// Dépose un fichier téléchargeable ; retourne son URL absolue (ou null).
// Ne PAS définir Content-Type : le navigateur ajoute la frontière multipart.
export async function adminUpload(
  file: File,
  token: string,
): Promise<{ url: string; name: string; size: number } | null> {
  const base = adminApiBase();
  if (!base) return null;
  const form = new FormData();
  form.append('file', file);
  try {
    const res = await fetch(`${base}/api/admin/upload`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
      body: form,
    });
    if (!res.ok) return null;
    const j = (await res.json()) as { name: string; size: number; path: string };
    return { url: `${base}${j.path}`, name: j.name, size: j.size };
  } catch {
    return null;
  }
}
