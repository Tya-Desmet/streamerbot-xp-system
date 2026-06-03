import type { LeaderboardPlayer } from '@/lib/contract';
import { formatXp } from '@/lib/format';
import { avatarGradient, initials } from '@/lib/avatar';

const RANK_META: Record<number, { tag: string; color: string }> = {
  1: { tag: 'TOP 1', color: 'var(--gold)' },
  2: { tag: 'TOP 2', color: 'var(--silver)' },
  3: { tag: 'TOP 3', color: 'var(--bronze)' },
};

function PodiumCard({ p, big }: { p: LeaderboardPlayer; big: boolean }) {
  const m = RANK_META[p.rank] ?? { tag: `#${p.rank}`, color: 'var(--neon-1)' };
  const s = big ? 88 : 72;

  return (
    <a
      href={`/viewer/${p.username}/`}
      className="card transition-transform hover:-translate-y-1"
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 6,
        padding: big ? '30px 20px 26px' : '24px 18px',
        borderColor: m.color,
        boxShadow: `0 18px 52px -26px ${m.color}`,
      }}
    >
      <span
        style={{
          fontFamily: 'var(--font-display)',
          fontWeight: 700,
          fontSize: 12,
          letterSpacing: '.1em',
          color: m.color,
        }}
      >
        #{p.rank} · {m.tag}
      </span>
      <span
        aria-hidden="true"
        className="ava round"
        style={{ width: s, height: s, fontSize: s * 0.36, background: avatarGradient(p.username), margin: '6px 0' }}
      >
        {initials(p.displayName || p.username)}
      </span>
      <b style={{ fontFamily: 'var(--font-display)', fontSize: big ? 22 : 18 }}>{p.displayName}</b>
      <span className="muted" style={{ fontSize: 13 }}>
        Niveau {p.level}
        {p.title ? ` · ${p.title}` : ''}
      </span>
      <span className="text-grad" style={{ fontFamily: 'var(--font-display)', fontWeight: 700, marginTop: 2 }}>
        {formatXp(p.xp)}
      </span>
    </a>
  );
}

// Rangs 1-3. Desktop : 2 — 1 — 3 (1er au centre, surélevé). Mobile : empilé.
export default function Podium({ players }: { players: LeaderboardPlayer[] }) {
  if (players.length === 0) return null;
  const first = players[0];
  const second = players.length > 1 ? players[1] : null;
  const third = players.length > 2 ? players[2] : null;

  return (
    <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 items-end">
      {second ? (
        <div className="order-2 sm:order-1">
          <PodiumCard p={second} big={false} />
        </div>
      ) : null}
      <div className="order-1 sm:order-2">
        <PodiumCard p={first} big />
      </div>
      {third ? (
        <div className="order-3">
          <PodiumCard p={third} big={false} />
        </div>
      ) : null}
    </div>
  );
}
