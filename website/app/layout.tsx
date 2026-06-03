import type { Metadata } from 'next';
import { Space_Grotesk, Poppins } from 'next/font/google';
import './globals.css';
import NavBar from '@/components/NavBar';
import Footer from '@/components/Footer';
import { getSocials, getSite } from '@/lib/content';

const display = Space_Grotesk({ subsets: ['latin'], variable: '--font-space-grotesk', display: 'swap' });
const body = Poppins({
  subsets: ['latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-poppins',
  display: 'swap',
});

export const metadata: Metadata = {
  title: 'Stream Hub — Mystya',
  description: 'Hub communautaire — leaderboard, planning et ressources',
};

// Le thème est choisi par le STREAMER (content/site.json), appliqué au build.
// Les visiteurs ne le changent pas → pas de switcher, pas de localStorage.
export default function RootLayout({ children }: { children: React.ReactNode }) {
  const socials = getSocials();
  const site = getSite();

  return (
    <html lang="fr" data-theme={site.theme || 'shibuya'} className={`${display.variable} ${body.variable}`}>
      <body>
        <NavBar twitchUrl={socials.twitch ?? '#'} twitchChannel={site.twitchChannel} />
        {children}
        <Footer socials={socials} />
      </body>
    </html>
  );
}
