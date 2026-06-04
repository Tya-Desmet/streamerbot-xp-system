import { Suspense } from 'react';
import { getLeaderboard, getMeta } from '@/lib/data';
import EmbedLeaderboard from '@/components/EmbedLeaderboard';

// Route embed SSG : seed = classement build-time, rafraîchi en live côté client.
// Suspense obligatoire autour d'un composant qui lit useSearchParams (export statique).
export const dynamic = 'force-static';

export default function EmbedLeaderboardPage() {
  const seed = getLeaderboard();
  const streamerName = getMeta().streamer.name || '';
  return (
    <Suspense fallback={null}>
      <EmbedLeaderboard seed={seed} streamerName={streamerName} />
    </Suspense>
  );
}
