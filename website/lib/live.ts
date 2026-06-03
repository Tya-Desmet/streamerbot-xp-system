// Détection du statut live via decapi.me (API publique Twitch, sans auth, CORS-friendly).
// Client-only (fetch navigateur). Aucune clé secrète.

// Extrait le login Twitch d'une URL twitch.tv/xxx
export function channelFromUrl(url: string): string {
  const m = (url || '').match(/twitch\.tv\/([^/?#]+)/i);
  return m ? m[1] : '';
}

// true si la chaîne est en live. Best-effort : false en cas d'erreur/offline.
export async function checkLive(channel: string): Promise<boolean> {
  if (!channel) return false;
  try {
    const res = await fetch(`https://decapi.me/twitch/uptime/${encodeURIComponent(channel)}`, {
      cache: 'no-store',
    });
    if (!res.ok) return false;
    const txt = (await res.text()).toLowerCase();
    // decapi renvoie une durée si live, sinon "{channel} is offline" / message d'erreur.
    return !(txt.includes('offline') || txt.includes('error') || txt.includes('unable') || txt.includes('not'));
  } catch {
    return false;
  }
}

// URL d'aperçu live publique de Twitch (valide uniquement quand la chaîne est en live).
export function livePreview(channel: string, w = 440, h = 248): string {
  return `https://static-cdn.jtvnw.net/previews-ttv/live_user_${channel}-${w}x${h}.jpg`;
}
