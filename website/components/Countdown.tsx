'use client';

import { useEffect, useState } from 'react';

// Après 4 h de dépassement, la date est clairement passée (pas "en live / bientôt").
const PAST_THRESHOLD_MS = 4 * 60 * 60 * 1000;

type Parts = { d: number; h: number; m: number; s: number; done: boolean; past: boolean };

function compute(target: number): Parts {
  const ms = target - Date.now();
  if (ms <= 0) return { d: 0, h: 0, m: 0, s: 0, done: true, past: -ms > PAST_THRESHOLD_MS };
  const s = Math.floor(ms / 1000);
  return {
    d: Math.floor(s / 86400),
    h: Math.floor((s % 86400) / 3600),
    m: Math.floor((s % 3600) / 60),
    s: s % 60,
    done: false,
    past: false,
  };
}

function Box({ value, label }: { value: string; label: string }) {
  return (
    <div className="card" style={{ padding: '12px 16px', textAlign: 'center', minWidth: 64 }}>
      <div style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 26 }}>{value}</div>
      <div className="muted" style={{ fontSize: 11, letterSpacing: '.12em' }}>{label}</div>
    </div>
  );
}

// SSR-safe : rend un placeholder identique serveur/client, puis décompte après montage.
export default function Countdown({ target }: { target: string }) {
  const [parts, setParts] = useState<Parts | null>(null);

  useEffect(() => {
    const ts = new Date(target).getTime();
    if (Number.isNaN(ts)) return;
    const tick = () => setParts(compute(ts));
    tick();
    const id = setInterval(tick, 1000);
    return () => clearInterval(id);
  }, [target]);

  if (!parts) {
    return (
      <div className="flex gap-s" style={{ flexWrap: 'wrap' }}>
        <Box value="--" label="JOURS" />
        <Box value="--" label="HEURES" />
        <Box value="--" label="MIN" />
        <Box value="--" label="SEC" />
      </div>
    );
  }

  if (parts.done) {
    if (parts.past) {
      return (
        <span className="chip" style={{ opacity: 0.6 }}>
          Prochain live à venir
        </span>
      );
    }
    return (
      <span className="chip">
        <span className="dot" /> En live / très bientôt
      </span>
    );
  }

  const pad = (n: number) => n.toString().padStart(2, '0');
  return (
    <div className="flex gap-s" style={{ flexWrap: 'wrap' }}>
      <Box value={String(parts.d)} label="JOURS" />
      <Box value={pad(parts.h)} label="HEURES" />
      <Box value={pad(parts.m)} label="MIN" />
      <Box value={pad(parts.s)} label="SEC" />
    </div>
  );
}
