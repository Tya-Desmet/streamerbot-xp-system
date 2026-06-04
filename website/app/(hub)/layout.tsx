import type { Metadata } from 'next';
import { Space_Grotesk, Poppins } from 'next/font/google';
import './globals.css';
import NavBar from '@/components/NavBar';
import Footer from '@/components/Footer';
import JsonLd from '@/components/JsonLd';
import { getSocials, getSite } from '@/lib/content';
import { getMeta } from '@/lib/data';
import { SITE_URL, SITE_NAME } from '@/lib/seo';

const display = Space_Grotesk({ subsets: ['latin'], variable: '--font-space-grotesk', display: 'swap' });
const body = Poppins({
  subsets: ['latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-poppins',
  display: 'swap',
});

export const metadata: Metadata = {
  metadataBase: new URL(SITE_URL),
  title: {
    default: 'Mystya — Streameuse Twitch',
    template: '%s · Mystya',
  },
  description:
    'Mystya est une streameuse Twitch francophone. Rejoins la communauté : classement XP, planning des lives, ressources.',
  keywords: ['Mystya', 'mystya', 'MystyaTV', 'streameuse Twitch', 'stream Valorant', 'stream Minecraft', 'hub communautaire'],
  openGraph: {
    type: 'website',
    locale: 'fr_FR',
    siteName: SITE_NAME,
    images: [{ url: '/og.jpg', width: 1200, height: 630 }],
  },
  twitter: { card: 'summary_large_image', images: ['/og.jpg'] },
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  const socials = getSocials();
  const site = getSite();
  const streamerName = getMeta().streamer.name || 'Mystya';

  const sameAs = Object.values(socials).filter((v): v is string => typeof v === 'string' && v.length > 0);

  const ld = {
    '@context': 'https://schema.org',
    '@graph': [
      {
        '@type': 'WebSite',
        '@id': `${SITE_URL}/#website`,
        name: SITE_NAME,
        url: SITE_URL,
        description: 'Hub communautaire de Mystya — classement XP, planning des streams et ressources.',
        inLanguage: 'fr-FR',
        // Active la sitelinks search box Google quand on cherche "mystya".
        potentialAction: {
          '@type': 'SearchAction',
          target: `${SITE_URL}/leaderboard/?q={search_term_string}`,
          'query-input': 'required name=search_term_string',
        },
      },
      {
        '@type': 'Person',
        '@id': `${SITE_URL}/#person`,
        name: streamerName,
        alternateName: ['mystya', 'MystyaTV', 'Mystya TV'],
        description:
          'Streameuse Twitch francophone — Valorant, Minecraft, événements communautaires et soirées viewers.',
        jobTitle: 'Streameuse Twitch',
        url: SITE_URL,
        image: `${SITE_URL}/og.jpg`,
        sameAs,
        knowsAbout: ['Twitch', 'Valorant', 'Minecraft', 'Elden Ring', 'Gaming', 'Streaming', 'Just Chatting'],
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
