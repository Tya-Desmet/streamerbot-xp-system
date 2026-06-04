'use client';

import { useEffect, useState } from 'react';
import type { Leaderboard as LeaderboardData } from '@/lib/contract';
import { leaderboardUrl } from '@/lib/api';
import Podium from './Podium';

const REFRESH_MS = 45000;

// Podium de l'accueil en LIVE : même pattern de polling que Leaderboard/Embed.
// Rendu initial = seed build-time (SSR identique → non-régression), puis rafraîchi.
export default function LivePodium({ seed }: { seed: LeaderboardData }) {
  const [data, setData] = useState<LeaderboardData>(seed);

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

  const top3 = (data.players ?? []).slice(0, 3);
  return <Podium players={top3} />;
}
