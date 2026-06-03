'use client';

import { useEffect, useState } from 'react';
import { checkLive, livePreview } from '@/lib/live';

// Aperçu du live du streamer : affiché uniquement quand il est en live (decapi),
// avec la miniature publique Twitch.
export default function LivePreview({ channel, twitchUrl }: { channel: string; twitchUrl: string }) {
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

  if (!live) return null;

  return (
    <a
      href={twitchUrl}
      target="_blank"
      rel="noopener noreferrer"
      className="card transition-transform hover:-translate-y-1"
      style={{ display: 'inline-block', padding: 8, marginTop: 22, borderColor: 'oklch(0.7 0.2 25)' }}
      title="Regarder le live"
    >
      <span className="chip" style={{ marginBottom: 8 }}>
        <span className="dot" style={{ background: 'oklch(0.7 0.2 25)', boxShadow: '0 0 8px oklch(0.7 0.2 25)' }} />
        EN LIVE
      </span>
      {/* miniature Twitch (valide seulement quand live) */}
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img
        src={livePreview(channel)}
        alt="Aperçu du live"
        width={320}
        height={180}
        style={{ borderRadius: 'var(--r-md)', display: 'block' }}
      />
    </a>
  );
}
