// Loader du contenu éditorial (content/*.json) — usage SERVEUR (build-time).
// Fallback-safe : retourne des valeurs vides si le fichier est absent/malformé.
import fs from 'fs';
import path from 'path';

const DIR = path.join(process.cwd(), 'content');

function read<T>(file: string, fallback: T): T {
  try {
    const p = path.join(DIR, file);
    if (!fs.existsSync(p)) return fallback;
    return JSON.parse(fs.readFileSync(p, 'utf8')) as T;
  } catch {
    return fallback;
  }
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
