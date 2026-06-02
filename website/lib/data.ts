// Loader des JSON exportés — usage SERVEUR uniquement (build-time / Server Components).
// Ne jamais importer depuis un Client Component (fs indisponible côté navigateur).
import fs from 'fs';
import path from 'path';
import type { Meta, Leaderboard, PublicProfile } from './contract';

const DATA_DIR = path.join(process.cwd(), 'public', 'data');

function readJson<T>(file: string, fallback: T): T {
  try {
    const p = path.join(DATA_DIR, file);
    if (!fs.existsSync(p)) return fallback;
    return JSON.parse(fs.readFileSync(p, 'utf8')) as T;
  } catch {
    return fallback;
  }
}

export function getMeta(): Meta {
  return readJson<Meta>('meta.json', {
    schemaVersion: 1,
    generatedAt: 0,
    season: 'all-time',
    streamer: { name: '' },
  });
}

export function getLeaderboard(): Leaderboard {
  return readJson<Leaderboard>('leaderboard.json', {
    generatedAt: 0,
    season: 'all-time',
    players: [],
  });
}

export function getAllUsernames(): string[] {
  try {
    const dir = path.join(DATA_DIR, 'users');
    if (!fs.existsSync(dir)) return [];
    return fs
      .readdirSync(dir)
      .filter((f) => f.endsWith('.json'))
      .map((f) => f.replace(/\.json$/, ''));
  } catch {
    return [];
  }
}

export function getProfile(username: string): PublicProfile | null {
  return readJson<PublicProfile | null>(`users/${username}.json`, null);
}
