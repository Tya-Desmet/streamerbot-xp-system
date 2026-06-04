'use client';

import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import type { Leaderboard as LeaderboardData } from '@/lib/contract';
import { formatXp } from '@/lib/format';
import { avatarGradient, initials } from '@/lib/avatar';
import { leaderboardUrl } from '@/lib/api';

const REFRESH_MS = 45000;
const THEMES = ['shibuya', 'akiba', 'hara'];

// Leaderboard embarquable (iframe) : classement seul, AUCUN lien vers /viewer/*,
// aucun calcul côté client (données telles quelles depuis l'API/JSON). Paramétrable
// par query : theme, limit, bg, accent, title.
export default function EmbedLeaderboard({
  seed,
  streamerName,
}: {
  seed: LeaderboardData;
  streamerName: string;
}) {
  const params = useSearchParams();
  const [data, setData] = useState<LeaderboardData>(seed);

  // Polling léger de /api/leaderboard (live) avec garde generatedAt ; fallback = seed.
  useEffect(() => {
    let cancelled = false;
    async function refetch() {
      try {
        const res = await fetch(leaderboardUrl(), { cache: 'no-store' });
        if (!res.ok) return;
        const fresh: LeaderboardData = await res.json();
        if (cancelled) return;
        setData((prev) => (fresh.generatedAt >= prev.generatedAt ? fresh : prev));
      } catch {
        /* hors-ligne : on garde l'état courant */
      }
    }
    refetch();
    const id = setInterval(refetch, REFRESH_MS);
    return () => {
      cancelled = true;
      clearInterval(id);
    };
  }, []);

  // Lecture des options d'embed (query string).
  const theme = params.get('theme');
  const dataTheme = theme && THEMES.includes(theme) ? theme : undefined;
  const transparent = params.get('bg') === 'transparent';
  const accent = params.get('accent');
  const showTitle = params.get('title') !== 'off';
  const limit = useMemo(() => {
    const n = parseInt(params.get('limit') ?? '', 10);
    if (Number.isNaN(n)) return 10;
    return Math.min(50, Math.max(1, n));
  }, [params]);

  const players = (data.players ?? []).slice(0, limit);

  const wrapStyle: React.CSSProperties = {
    fontFamily: 'var(--font-body)',
    color: 'var(--text)',
    background: transparent ? 'transparent' : 'var(--bg-0)',
    padding: 14,
    boxSizing: 'border-box',
    minHeight: '100vh',
  };
  if (accent) {
    // Override de l'accent (couleur CSS valide attendue, ex. %23ff66cc encodé).
    (wrapStyle as Record<string, string>)['--neon-1'] = accent;
    (wrapStyle as Record<string, string>)['--accent'] = accent;
  }

  return (
    <div data-theme={dataTheme} style={wrapStyle}>
      {showTitle && (
        <div
          style={{
            fontFamily: 'var(--font-display)',
            fontWeight: 700,
            fontSize: 16,
            marginBottom: 12,
            display: 'flex',
            alignItems: 'baseline',
            gap: 8,
          }}
        >
          <span>Classement</span>
          {streamerName ? (
            <span style={{ color: 'var(--text-mute)', fontSize: 13, fontWeight: 500 }}>· {streamerName}</span>
          ) : null}
        </div>
      )}

      {players.length === 0 ? (
        <p style={{ color: 'var(--text-mute)', fontSize: 13, textAlign: 'center', padding: '32px 0' }}>
          Pas encore de données.
        </p>
      ) : (
        <ul style={{ display: 'flex', flexDirection: 'column', gap: 6, margin: 0, padding: 0 }}>
          {players.map((p) => (
            <li
              key={p.username}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 12,
                padding: '10px 12px',
                borderRadius: 'var(--r-md)',
                background: 'var(--surface)',
                border: '1px solid var(--line)',
              }}
            >
              <span
                style={{
                  width: 28,
                  textAlign: 'center',
                  fontWeight: 700,
                  fontSize: 14,
                  color: 'var(--text-mute)',
                  flexShrink: 0,
                }}
              >
                {p.rank}
              </span>
              <span
                aria-hidden="true"
                style={{
                  width: 34,
                  height: 34,
                  borderRadius: '50%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: 12,
                  fontWeight: 600,
                  color: '#fff',
                  background: avatarGradient(p.username),
                  flexShrink: 0,
                }}
              >
                {initials(p.displayName || p.username)}
              </span>
              <div style={{ flex: 1, minWidth: 0 }}>
                <p
                  style={{
                    fontWeight: 600,
                    fontSize: 14,
                    margin: 0,
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    whiteSpace: 'nowrap',
                  }}
                >
                  {p.displayName}
                </p>
                <p style={{ color: 'var(--text-mute)', fontSize: 12, margin: 0 }}>
                  Niv. {p.level}
                  {p.title ? ` · ${p.title}` : ''}
                </p>
              </div>
              <span className="text-grad" style={{ fontWeight: 700, fontSize: 14, flexShrink: 0 }}>
                {formatXp(p.xp)}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
