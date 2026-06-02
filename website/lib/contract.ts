// Types dérivés de docs/EXPORT_CONTRACT.md (schemaVersion 1).
// Toute évolution du contrat doit être répercutée ici ET dans le .md.

export interface Meta {
  schemaVersion: number;
  generatedAt: number;
  season: string;
  streamer: { name: string };
}

export interface LeaderboardPlayer {
  rank: number;
  username: string;
  displayName: string;
  level: number;
  xp: number;
  watchTime: number;
  title: string;
}

export interface Leaderboard {
  generatedAt: number;
  season: string;
  players: LeaderboardPlayer[];
}

export interface PublicProfile {
  username: string;
  displayName: string;
  level: number;
  xp: number;
  xpIntoLevel: number;
  xpForNext: number;
  percentage: number;
  rank: number;
  title: string;
  messages: number;
  watchTime: number;
  watchStreak: number;
  sources: { chat: number; watch: number; rewards: number };
}
