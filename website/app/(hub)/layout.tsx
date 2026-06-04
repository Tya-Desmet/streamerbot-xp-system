import type { Metadata } from 'next';
import { Space_Grotesk, Poppins } from 'next/font/google';
import '../globals.css';
import NavBar from '@/components/NavBar';
import Footer from '@/components/Footer';
import JsonLd from '@/components/JsonLd';
import { getSocials, getSite } from '@/lib/content';
import { getMeta } from '@/lib/data';
import { SITE_URL, siteName, tagline, ogImage } from '@/lib/seo';

const display = Space_Grotesk({ subsets: ['latin'], variable: '--font-space-grotesk', display: 'swap' });
const body = Poppins({
  subsets: ['latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-poppins',
  display: 'swap',
});

export function generateMetadata(): Metadata {
  const site = getSite();
  const name = siteName();
  const og = ogImage();
  return {
    metadataBase: new URL(SITE_URL),
    title: {
      default: `${name} — ${tagline()}`,
      template: `%s · ${name}`,
    },
    description: site.description || '',
    keywords: site.keywords || [],
    openGraph: {
      type: 'website',
      locale: 'fr_FR',
      siteName: name,
      images: [{ url: og, width: 1200, height: 630 }],
    },
    twitter: { card: 'summary_large_image', images: [og] },
  };
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  const socials = getSocials();
  const site = getSite();
  const name = siteName();
  const streamerName = getMeta().streamer.name || name;

  const sameAs = Object.values(socials).filter((v): v is string => typeof v === 'string' && v.length > 0);

  const ld = {
    '@context': 'https://schema.org',
    '@graph': [
      {
        '@type': 'WebSite',
        '@id': `${SITE_URL}/#website`,
        name,
        url: SITE_URL,
        description: `Hub communautaire de ${name} — classement XP, planning des streams et ressources.`,
        inLanguage: 'fr-FR',
        // Active la sitelinks search box Google quand on cherche le nom du streamer.
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
        alternateName: site.alternateNames || [],
        description:
          'Streameuse Twitch francophone — Valorant, Minecraft, événements communautaires et soirées viewers.',
        jobTitle: tagline(),
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
        <NavBar twitchUrl={socials.twitch ?? '#'} twitchChannel={site.twitchChannel} siteName={name} />
        {children}
        <Footer socials={socials} siteName={name} />
      </body>
    </html>
  );
}
