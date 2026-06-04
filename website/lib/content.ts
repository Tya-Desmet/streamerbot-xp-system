// Loader du contenu éditorial (content/*.json) — usage SERVEUR (build-time).
// Fallback-safe : retourne des valeurs vides si le fichier est absent/malformé.
import fs from 'fs';
import path from 'path';

const DIR = path.join(process.cwd(), 'content');

// Override local prioritaire : "site.json" → "site.local.json" (gitignoré, non distribué).
// Si le .local existe, il REMPLACE le fichier committé (override complet, pas de fusion).
// Permet de garder un template neutre committé + ses vraies valeurs hors du dépôt.
function read<T>(file: string, fallback: T): T {
  const local = file.replace(/\.json$/, '.local.json');
  for (const name of [local, file]) {
    try {
      const p = path.join(DIR, name);
      if (fs.existsSync(p)) return JSON.parse(fs.readFileSync(p, 'utf8')) as T;
    } catch {
      /* fichier illisible → on tente le suivant */
    }
  }
  return fallback;
}

export type Socials = {
  twitch?: string;
  youtube?: string;
  instagram?: string;
  tiktok?: string;
  discord?: string;
};

export type Friend = {
  name: string;
  handle: string;
  live: boolean;
  game: string;
  viewers: number;
  hue: number;
  url: string;
  channel?: string; // login Twitch (pour la détection live decapi) — dérivé de url si absent
};

// Sections optionnelles du hub (template) — flag absent = activé (défaut true).
export type SiteFeatures = {
  planning?: boolean;
  ressources?: boolean;
  friends?: boolean;
  socials?: boolean;
};

export type Site = {
  theme: string;
  twitchChannel: string;
  siteName?: string;
  tagline?: string;
  description?: string;
  keywords?: string[];
  alternateNames?: string[];
  ogImage?: string;
  features?: SiteFeatures;
};

export type ScheduleDay = {
  day: string;
  date: string;
  off: boolean;
  game?: string | null;
  mode?: string;
  time?: string;
  dur?: string;
  note?: string;
  tag?: string;
};

export type Schedule = { nextLiveISO?: string; timezone?: string; days: ScheduleDay[] };

export type Download = {
  id: string;
  title: string;
  cat: string;
  desc: string;
  icon: string;
  ext: string;
  size?: string;
  version?: string;
  date?: string;
  dls?: number;
  featured?: boolean;
  downloadable?: boolean; // true = bouton « Télécharger » (sinon « Bientôt »)
  tags: string[];
  url?: string;
};

export function getSocials(): Socials {
  return read<Socials>('socials.json', {});
}
export function getFriends(): Friend[] {
  return read<Friend[]>('friends.json', []);
}
export function getSchedule(): Schedule {
  return read<Schedule>('schedule.json', { days: [] });
}
export function getDownloads(): Download[] {
  return read<Download[]>('downloads.json', []);
}
export function getSite(): Site {
  return read<Site>('site.json', { theme: 'shibuya', twitchChannel: '' });
}

// Un flag absent vaut true (défaut tout activé → non-régression pour une install existante).
export function isFeatureOn(key: keyof SiteFeatures): boolean {
  return getSite().features?.[key] !== false;
}
