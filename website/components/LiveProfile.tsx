'use client';

import { useEffect, useState } from 'react';
import type { PublicProfile, Leaderboard } from '@/lib/contract';
import { userUrl, leaderboardUrl } from '@/lib/api';
import ProfileHeader from './ProfileHeader';
import XpBar from './XpBar';
import StatGrid from './StatGrid';

// Profil viewer en lecture quasi temps réel.
// `seed` = données figées au build (premier rendu + fallback hors-ligne).
// On n'adopte le profil live que si le snapshot backend est >= au build
// (même règle que le classement → les deux pages restent cohérentes).
export default function LiveProfile({
  seed,
  seedGeneratedAt,
}: {
  seed: PublicProfile;
  seedGeneratedAt: number;
}) {
  const [profile, setProfile] = useState<PublicProfile>(seed);

  useEffect(() => {
    let cancelled = false;
    async function refetch() {
      try {
        // 1. Version du snapshot backend (porté par le classement).
        const lbRes = await fetch(leaderboardUrl(), { cache: 'no-store' });
        if (!lbRes.ok) return;
        const lb: Leaderboard = await lbRes.json();
        if (cancelled || (lb.generatedAt ?? 0) < seedGeneratedAt) return;

        // 2. Snapshot au moins aussi récent que le build → on prend le profil live.
        const res = await fetch(userUrl(seed.username), { cache: 'no-store' });
        if (!res.ok) return;
        const fresh: PublicProfile = await res.json();
        if (!cancelled && fresh && typeof fresh.xp === 'number') setProfile(fresh);
      } catch {
        /* hors-ligne : on garde le seed */
      }
    }
    refetch();
    return () => {
      cancelled = true;
    };
  }, [seed.username, seedGeneratedAt]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 20, marginTop: 12 }}>
      <ProfileHeader profile={profile} />
      <div className="card" style={{ padding: '20px 24px' }}>
        <XpBar
          percentage={profile.percentage}
          xpIntoLevel={profile.xpIntoLevel}
          xpForNext={profile.xpForNext}
        />
      </div>
      <StatGrid profile={profile} />
    </div>
  );
}
