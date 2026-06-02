import type { Metadata } from 'next';
import { getLeaderboard, getMeta } from '@/lib/data';
import Leaderboard from '@/components/Leaderboard';
import Reveal from '@/components/Reveal';

export const metadata: Metadata = { title: 'Classement — Stream Hub' };

export default function LeaderboardPage() {
  const seed = getLeaderboard();
  const meta = getMeta();

  return (
    <main className="section">
      <div className="wrap">
        <Reveal>
          <div className="section-head">
            <div>
              <div className="eyebrow">Classement · 番付</div>
              <h2>
                Le classement {meta.streamer.name ? `de ${meta.streamer.name}` : ''}
              </h2>
              <p className="dim">Mis à jour en continu pendant le live · tri par niveau puis XP.</p>
            </div>
          </div>
        </Reveal>
        <Leaderboard seed={seed} />
      </div>
    </main>
  );
}
