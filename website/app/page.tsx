import { getMeta, getLeaderboard } from '@/lib/data';
import { getFriends, getSocials, getSite } from '@/lib/content';
import Hero from '@/components/Hero';
import FriendsLive from '@/components/FriendsLive';
import LivePreview from '@/components/LivePreview';
import SocialCard, { SOCIAL_META, type Platform } from '@/components/SocialCard';
import Podium from '@/components/Podium';
import Reveal from '@/components/Reveal';
import { buildMetadata } from '@/lib/seo';

export const metadata = buildMetadata({
  description: 'Le hub de la communauté : classement XP, copains en live, planning et ressources.',
  path: '/',
});

export default function Home() {
  const meta = getMeta();
  const lb = getLeaderboard();
  const friends = getFriends();
  const socials = getSocials();
  const site = getSite();

  const top3 = (lb.players ?? []).slice(0, 3);
  const platforms = (Object.keys(SOCIAL_META) as Platform[]).filter((p) => socials[p]);

  return (
    <main>
      <Hero streamerName={meta.streamer.name} twitchUrl={socials.twitch ?? '#'} />

      {site.twitchChannel ? (
        <div className="wrap" style={{ textAlign: 'center', marginTop: -24 }}>
          <LivePreview channel={site.twitchChannel} twitchUrl={socials.twitch ?? '#'} />
        </div>
      ) : null}

      {friends.length > 0 && (
        <section className="section">
          <div className="wrap">
            <Reveal>
              <div className="section-head">
                <div>
                  <div className="eyebrow">Copains · 仲間</div>
                  <h2>Copains en live</h2>
                </div>
              </div>
            </Reveal>
            <Reveal>
              <FriendsLive friends={friends} />
            </Reveal>
          </div>
        </section>
      )}

      {top3.length > 0 && (
        <section className="section">
          <div className="wrap">
            <Reveal>
              <div className="section-head">
                <div>
                  <div className="eyebrow">Top viewers · 番付</div>
                  <h2>Le podium</h2>
                </div>
                <a className="btn btn-ghost btn-sm" href="/leaderboard/">
                  Tout le classement
                </a>
              </div>
            </Reveal>
            <Reveal>
              <Podium players={top3} />
            </Reveal>
          </div>
        </section>
      )}

      {platforms.length > 0 && (
        <section className="section">
          <div className="wrap">
            <Reveal>
              <div className="section-head">
                <div>
                  <div className="eyebrow">Réseaux · ソーシャル</div>
                  <h2>Rejoins-moi</h2>
                </div>
              </div>
            </Reveal>
            <Reveal>
              <div className="grid" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))' }}>
                {platforms.map((p) => (
                  <SocialCard key={p} platform={p} url={socials[p] as string} />
                ))}
              </div>
            </Reveal>
          </div>
        </section>
      )}
    </main>
  );
}
