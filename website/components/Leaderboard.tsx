'use client';

import { useEffect, useState } from 'react';
import type { Leaderboard as LeaderboardData } from '@/lib/contract';
import { formatXp } from '@/lib/format';
import { avatarGradient, initials } from '@/lib/avatar';
import { leaderboardUrl } from '@/lib/api';
import Podium from './Podium';
import PlayerRow from './PlayerRow';
import FindMe from './FindMe';

const REFRESH_MS = 45000;
const ME_KEY = 'sl-me';

const TABS: { id: string; label: string; enabled: boolean }[] = [
  { id: 'global', label: 'Global', enabled: true },
  { id: 'month', label: 'Ce mois', enabled: false },
  { id: 'week', label: 'Cette semaine', enabled: false },
];

const searchStyle: React.CSSProperties = {
  height: 44,
  padding: '0 16px',
  borderRadius: 999,
  background: 'var(--surface)',
  border: '1px solid var(--line-strong)',
  color: 'var(--text)',
  minWidth: 200,
  flex: '1 1 200px',
  maxWidth: 340,
};

export default function Leaderboard({ seed }: { seed: LeaderboardData }) {
  const [data, setData] = useState<LeaderboardData>(seed);
  const [query, setQuery] = useState('');
  const [me, setMe] = useState('');

  // Polling léger du classement (lecture seule de /data/leaderboard.json).
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

  useEffect(() => {
    try {
      setMe(localStorage.getItem(ME_KEY) ?? '');
    } catch {
      /* ignore */
    }
  }, []);

  function saveMe(name: string) {
    const v = name.trim();
    setMe(v);
    try {
      if (v) localStorage.setItem(ME_KEY, v);
      else localStorage.removeItem(ME_KEY);
    } catch {
      /* ignore */
    }
  }

  const players = data.players ?? [];

  if (players.length === 0) {
    return (
      <p className="muted" style={{ textAlign: 'center', padding: '64px 0' }}>
        Pas encore de données. Le classement apparaîtra dès le premier export du bot.
      </p>
    );
  }

  const q = query.trim().toLowerCase();
  const filtered = q
    ? players.filter(
        (p) => p.displayName.toLowerCase().includes(q) || p.username.toLowerCase().includes(q),
      )
    : players;

  const meLc = me.trim().toLowerCase();
  const myEntry = meLc
    ? players.find((p) => p.username.toLowerCase() === meLc || p.displayName.toLowerCase() === meLc)
    : undefined;
  const isMe = (u: string) => !!myEntry && u === myEntry.username;

  const top3 = players.slice(0, 3);
  const rest = players.slice(3);

  return (
    <div className="flex" style={{ flexDirection: 'column', gap: 24 }}>
      {/* Onglets + recherche */}
      <div className="flex center gap-s" style={{ flexWrap: 'wrap' }}>
        {TABS.map((t) =>
          t.enabled ? (
            <button key={t.id} type="button" className="btn btn-primary btn-sm">
              {t.label}
            </button>
          ) : (
            <button
              key={t.id}
              type="button"
              className="btn btn-ghost btn-sm"
              disabled
              title="Bientôt — nécessite le suivi par période côté bot"
              style={{ opacity: 0.45, cursor: 'not-allowed' }}
            >
              {t.label}
            </button>
          ),
        )}
        <input
          style={{ ...searchStyle, marginLeft: 'auto' }}
          placeholder="Rechercher un viewer…"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          aria-label="Rechercher un viewer"
        />
      </div>

      {/* Trouve-toi */}
      <FindMe value={me} onSave={saveMe} />
      {myEntry ? (
        <div
          className="card flex center"
          style={{ padding: '14px 20px', gap: 14, borderColor: 'var(--neon-1)' }}
        >
          <span style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 22, color: 'var(--neon-1)' }}>
            #{myEntry.rank}
          </span>
          <span
            className="ava round"
            style={{ width: 44, height: 44, fontSize: 15, background: avatarGradient(myEntry.username) }}
          >
            {initials(myEntry.displayName || myEntry.username)}
          </span>
          <div style={{ flex: 1, minWidth: 0 }}>
            <b>{myEntry.displayName} · c&apos;est toi</b>
            <div className="muted" style={{ fontSize: 13 }}>
              Niveau {myEntry.level} · {formatXp(myEntry.xp)}
            </div>
          </div>
        </div>
      ) : meLc ? (
        <p className="muted" style={{ fontSize: 13 }}>
          « {me} » n&apos;est pas (encore) dans le classement.
        </p>
      ) : null}

      {/* Résultats */}
      {q ? (
        <ul className="flex" style={{ flexDirection: 'column', gap: 8 }}>
          {filtered.length === 0 ? (
            <li className="muted" style={{ textAlign: 'center', padding: '24px 0' }}>
              Aucun viewer ne correspond à « {query} ».
            </li>
          ) : (
            filtered.map((p) => <PlayerRow key={p.username} player={p} highlight={isMe(p.username)} />)
          )}
        </ul>
      ) : (
        <>
          <Podium players={top3} />
          {rest.length > 0 && (
            <ul className="flex" style={{ flexDirection: 'column', gap: 8 }}>
              {rest.map((p) => (
                <PlayerRow key={p.username} player={p} highlight={isMe(p.username)} />
              ))}
            </ul>
          )}
        </>
      )}
    </div>
  );
}
