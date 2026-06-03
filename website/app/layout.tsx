import type { Metadata } from 'next';
import { Space_Grotesk, Poppins } from 'next/font/google';
import './globals.css';
import NavBar from '@/components/NavBar';
import Footer from '@/components/Footer';
import JsonLd from '@/components/JsonLd';
import { getSocials, getSite } from '@/lib/content';
import { getMeta } from '@/lib/data';
import { SITE_URL } from '@/lib/seo';

const display = Space_Grotesk({ subsets: ['latin'], variable: '--font-space-grotesk', display: 'swap' });
const body = Poppins({
  subsets: ['latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-poppins',
  display: 'swap',
});

export const metadata: Metadata = {
  metadataBase: new URL(SITE_URL),
  title: { default: 'Stream Hub — Mystya', template: '%s · Stream Hub' },
  description: 'Hub communautaire — classement XP, planning des streams et ressources.',
  openGraph: {
    type: 'website',
    locale: 'fr_FR',
    siteName: 'Stream Hub',
    images: ['/og.jpg'],
  },
  twitter: { card: 'summary_large_image', images: ['/og.jpg'] },
};

// Le thème est choisi par le STREAMER (content/site.json), appliqué au build.
// Les visiteurs ne le changent pas → pas de switcher, pas de localStorage.
export default function RootLayout({ children }: { children: React.ReactNode }) {
  const socials = getSocials();
  const site = getSite();
  const streamerName = getMeta().streamer.name || 'Mystya';

  const ld = {
    '@context': 'https://schema.org',
    '@graph': [
      { '@type': 'WebSite', name: 'Stream Hub', url: SITE_URL },
      {
        '@type': 'Person',
        name: streamerName,
        url: SITE_URL,
        sameAs: Object.values(socials).filter((v): v is string => typeof v === 'string' && v.length > 0),
      },
    ],
  };

  return (
    <html lang="fr" data-theme={site.theme || 'shibuya'} className={`${display.variable} ${body.variable}`}>
      <body>
        <JsonLd data={ld} />
        <NavBar twitchUrl={socials.twitch ?? '#'} twitchChannel={site.twitchChannel} />
        {children}
        <Footer socials={socials} />
      </body>
    </html>
  );
}
