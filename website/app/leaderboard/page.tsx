import { getLeaderboard, getMeta } from '@/lib/data';
import Leaderboard from '@/components/Leaderboard';
import Reveal from '@/components/Reveal';
import JsonLd from '@/components/JsonLd';
import { buildMetadata, SITE_URL } from '@/lib/seo';

export const metadata = buildMetadata({
  title: 'Classement',
  description: 'Le classement XP de la communauté : podium, niveaux et watchtime.',
  path: '/leaderboard/',
});

export default function LeaderboardPage() {
  const seed = getLeaderboard();
  const meta = getMeta();

  const itemList = {
    '@context': 'https://schema.org',
    '@type': 'ItemList',
    name: 'Classement de la communauté',
    itemListElement: (seed.players ?? []).slice(0, 10).map((p) => ({
      '@type': 'ListItem',
      position: p.rank,
      name: p.displayName,
      url: new URL(`/viewer/${p.username}/`, SITE_URL).toString(),
    })),
  };

  return (
    <main className="section">
      <div className="wrap">
        <JsonLd data={itemList} />
        <Reveal>
          <div className="section-head">
            <div>
              <div className="eyebrow">Classement · 番付</div>
              <h1>
                Le classement {meta.streamer.name ? `de ${meta.streamer.name}` : ''}
              </h1>
              <p className="dim">Mis à jour en continu pendant le live · tri par niveau puis XP.</p>
            </div>
          </div>
        </Reveal>
        <Leaderboard seed={seed} />
      </div>
    </main>
  );
}
