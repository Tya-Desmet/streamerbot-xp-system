// Client API (runtime, navigateur). Si NEXT_PUBLIC_API_URL est défini, le site
// récupère le contenu éditorial depuis le backend ; sinon il garde les données
// build-time (content/*.json) → le site reste 100% statique sans backend.

const API = process.env.NEXT_PUBLIC_API_URL;

export function apiBase(): string | null {
  return API && API.length > 0 ? API.replace(/\/$/, '') : null;
}

// URL du leaderboard : API temps réel si configurée, sinon le fichier statique exporté.
export function leaderboardUrl(): string {
  const base = apiBase();
  return base ? `${base}/api/leaderboard` : '/data/leaderboard.json';
}

// Récupère un type de contenu depuis l'API ; renvoie `fallback` si pas d'API ou erreur.
export async function fetchContent<T>(kind: string, fallback: T): Promise<T> {
  const base = apiBase();
  if (!base) return fallback;
  try {
    const res = await fetch(`${base}/api/content/${kind}`, { cache: 'no-store' });
    if (!res.ok) return fallback;
    return (await res.json()) as T;
  } catch {
    return fallback;
  }
}
