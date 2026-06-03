'use client';

import { useEffect, useState } from 'react';
import { checkLive, livePreview } from '@/lib/live';

const RECHECK_MS = 2 * 60 * 1000; // re-vérification toutes les 2 min

// Aperçu du live du streamer : affiché uniquement quand il est en live (decapi),
// avec la miniature publique Twitch. Re-check périodique pour détecter le début du live.
export default function LivePreview({ channel, twitchUrl }: { channel: string; twitchUrl: string }) {
  const [live, setLive] = useState(false);
  const [tick, setTick] = useState(0); // force le re-rendu pour rafraîchir la miniature

  useEffect(() => {
    let cancelled = false;
    async function check() {
      const v = await checkLive(channel);
      if (!cancelled) {
        setLive(v);
        if (v) setTick((t) => t + 1); // rafraîchit la miniature si en live
      }
    }
    check();
    const id = setInterval(check, RECHECK_MS);
    return () => {
      cancelled = true;
      clearInterval(id);
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
        key={tick}
        alt="Aperçu du live"
        width={320}
        height={180}
        style={{ borderRadius: 'var(--r-md)', display: 'block' }}
      />
    </a>
  );
}
