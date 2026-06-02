'use client';

import { useEffect, useState } from 'react';

type Petal = { left: number; delay: number; dur: number; scale: number };

// Pétales de sakura (hero uniquement). Générés côté client pour éviter tout
// mismatch d'hydratation (Math.random). Masqués si prefers-reduced-motion (CSS).
export default function Sakura({ count = 14 }: { count?: number }) {
  const [petals, setPetals] = useState<Petal[]>([]);

  useEffect(() => {
    setPetals(
      Array.from({ length: count }, () => ({
        left: Math.random() * 100,
        delay: Math.random() * 12,
        dur: 9 + Math.random() * 10,
        scale: 0.6 + Math.random() * 0.9,
      })),
    );
  }, [count]);

  return (
    <div className="sakura" aria-hidden="true">
      {petals.map((p, i) => (
        <i
          key={i}
          className="petal"
          style={{
            left: `${p.left}%`,
            animationDelay: `${p.delay}s`,
            animationDuration: `${p.dur}s`,
            scale: String(p.scale),
          }}
        />
      ))}
    </div>
  );
}
