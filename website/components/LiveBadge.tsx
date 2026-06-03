'use client';

import { useEffect, useState } from 'react';
import { checkLive } from '@/lib/live';

// Bouton "Live" de la nav reflétant le vrai statut (decapi).
export default function LiveBadge({ channel, twitchUrl }: { channel: string; twitchUrl: string }) {
  const [live, setLive] = useState(false);

  useEffect(() => {
    let cancelled = false;
    checkLive(channel).then((v) => {
      if (!cancelled) setLive(v);
    });
    return () => {
      cancelled = true;
    };
  }, [channel]);

  return (
    <a
      className={`btn btn-sm ${live ? 'btn-primary' : 'btn-ghost'}`}
      href={twitchUrl}
      target="_blank"
      rel="noopener noreferrer"
      title={live ? 'En live maintenant !' : 'Hors ligne'}
    >
      <span
        style={{
          width: 8,
          height: 8,
          borderRadius: '50%',
          background: live ? 'oklch(0.7 0.2 25)' : 'var(--text-mute)',
          boxShadow: live ? '0 0 8px oklch(0.7 0.2 25)' : 'none',
        }}
      />
      {live ? 'EN LIVE' : 'Live'}
    </a>
  );
}
