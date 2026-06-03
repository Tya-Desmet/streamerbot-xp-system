import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { getAllUsernames, getProfile, getMeta } from '@/lib/data';
import LiveProfile from '@/components/LiveProfile';
import Reveal from '@/components/Reveal';
import { buildMetadata } from '@/lib/seo';

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
  const name = profile ? profile.displayName : username;
  const description = profile
    ? `Profil de ${name} — niveau ${profile.level}, rang #${profile.rank} au classement.`
    : `Profil ${name}.`;
  return buildMetadata({ title: name, description, path: `/viewer/${username}/` });
}

export default async function ViewerPage({ params }: { params: Promise<{ username: string }> }) {
  const { username } = await params;
  const profile = getProfile(username);
  if (!profile) notFound();

  const generatedAt = getMeta().generatedAt;

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
          <LiveProfile seed={profile} seedGeneratedAt={generatedAt} />
        </Reveal>
      </div>
    </main>
  );
}
