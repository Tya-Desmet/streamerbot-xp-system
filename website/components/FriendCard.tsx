import type { Friend } from '@/lib/content';

// Carte d'un streamer ami (config statique). Statut live réel = futur (API Twitch).
export default function FriendCard({ friend }: { friend: Friend }) {
  const initials =
    friend.name.replace(/[^A-Za-zÀ-ÿ]/g, '').slice(0, 2).toUpperCase() || '??';
  const grad = `linear-gradient(135deg, oklch(0.78 0.18 ${friend.hue}), oklch(0.7 0.2 ${(friend.hue + 40) % 360}))`;

  return (
    <div className={`friend ${friend.live ? 'live' : 'offline'}`}>
      <div className="top">
        <div className="ava round" style={{ background: grad }}>
          {initials}
        </div>
        <div style={{ minWidth: 0 }}>
          <div className="nm">{friend.name}</div>
          <div className="hd">{friend.handle}</div>
        </div>
        <span className="status">
          {friend.live ? (
            <>
              <span className="d" />
              EN LIVE
            </>
          ) : (
            'HORS LIGNE'
          )}
        </span>
      </div>

      <div className="meta">
        <span className="g">{friend.game}</span>
        {friend.live ? (
          <span className="v">{new Intl.NumberFormat('fr-FR').format(friend.viewers)} viewers</span>
        ) : null}
      </div>

      <a
        className={`btn ${friend.live ? 'btn-primary' : 'btn-ghost'} btn-sm btn-block watch-btn`}
        href={friend.url}
        target="_blank"
        rel="noopener noreferrer"
      >
        {friend.live ? 'Regarder' : 'Voir la chaîne'}
      </a>
    </div>
  );
}
