import type { LeaderboardPlayer } from '@/lib/contract';
import { formatWatchTime, formatXp } from '@/lib/format';
import { avatarGradient, initials } from '@/lib/avatar';

export default function PlayerRow({
  player,
  highlight = false,
}: {
  player: LeaderboardPlayer;
  highlight?: boolean;
}) {
  return (
    <li>
      <a
        href={`/viewer/${player.username}/`}
        className="transition-transform hover:-translate-y-0.5"
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 14,
          padding: '12px 16px',
          borderRadius: 'var(--r-md)',
          background: 'var(--surface)',
          border: `1px solid ${highlight ? 'var(--neon-1)' : 'var(--line)'}`,
          boxShadow: highlight ? '0 14px 40px -22px var(--neon-1)' : 'none',
        }}
      >
        <span style={{ width: 34, textAlign: 'center', fontWeight: 700, color: 'var(--text-mute)' }}>
          #{player.rank}
        </span>
        <span
          aria-hidden="true"
          className="ava round"
          style={{ width: 40, height: 40, fontSize: 14, background: avatarGradient(player.username) }}
        >
          {initials(player.displayName || player.username)}
        </span>
        <div style={{ flex: 1, minWidth: 0 }}>
          <p className="truncate" style={{ fontWeight: 600 }}>
            {player.displayName}
            {highlight ? ' · toi' : ''}
          </p>
          <p className="muted truncate" style={{ fontSize: 12 }}>
            Niv. {player.level}
            {player.title ? ` · ${player.title}` : ''}
          </p>
        </div>
        <div style={{ textAlign: 'right', flexShrink: 0 }}>
          <p className="text-grad" style={{ fontWeight: 700 }}>
            {formatXp(player.xp)}
          </p>
          <p className="muted" style={{ fontSize: 12 }}>
            {formatWatchTime(player.watchTime)}
          </p>
        </div>
      </a>
    </li>
  );
}
