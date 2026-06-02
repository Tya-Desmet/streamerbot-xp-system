import type { Metadata } from 'next';
import { Space_Grotesk, Poppins } from 'next/font/google';
import './globals.css';
import ThemeProvider from '@/components/ThemeProvider';
import NavBar from '@/components/NavBar';
import Footer from '@/components/Footer';
import { getSocials } from '@/lib/content';

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

// Applique le thème mémorisé avant peinture (évite le flash sur akiba/hara).
const THEME_BOOT = "try{var t=localStorage.getItem('sl-theme');if(t)document.documentElement.setAttribute('data-theme',t);}catch(e){}";

export default function RootLayout({ children }: { children: React.ReactNode }) {
  const socials = getSocials();

  return (
    <html lang="fr" suppressHydrationWarning className={`${display.variable} ${body.variable}`}>
      <body>
        <script dangerouslySetInnerHTML={{ __html: THEME_BOOT }} />
        <ThemeProvider>
          <NavBar twitchUrl={socials.twitch ?? '#'} />
          {children}
          <Footer socials={socials} />
        </ThemeProvider>
      </body>
    </html>
  );
}
