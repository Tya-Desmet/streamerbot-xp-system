import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { getAllUsernames, getProfile } from '@/lib/data';
import ProfileHeader from '@/components/ProfileHeader';
import XpBar from '@/components/XpBar';
import StatGrid from '@/components/StatGrid';
import Reveal from '@/components/Reveal';

// Export statique : une page par profil, et uniquement celles-ci.
export const dynamicParams = false;

export function generateStaticParams() {
  return getAllUsernames().map((username) => ({ username }));
}

// Next 16 : params est asynchrone (Promise) → await.
export async function generateMetadata(
  { params }: { params: Promise<{ username: string }> },
): Promise<Metadata> {
  const { username } = await params;
  const profile = getProfile(username);
  return { title: (profile ? profile.displayName : username) + ' — Stream Hub' };
}

export default async function ViewerPage({ params }: { params: Promise<{ username: string }> }) {
  const { username } = await params;
  const profile = getProfile(username);
  if (!profile) notFound();

  return (
    <main className="section">
      <div className="wrap" style={{ maxWidth: 720 }}>
        <Reveal>
          <a href="/leaderboard/" className="muted" style={{ fontSize: 14 }}>
            ← Classement
          </a>
          <div className="eyebrow" style={{ marginTop: 14 }}>
            Profil · プロフィール
          </div>
        </Reveal>

        <Reveal>
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
        </Reveal>
      </div>
    </main>
  );
}
