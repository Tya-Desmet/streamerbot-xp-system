import type { Metadata } from 'next';
import { Space_Grotesk, Poppins } from 'next/font/google';
import './globals.css';
import { getSite } from '@/lib/content';
import NotFoundView from '@/components/NotFoundView';

// 404 GLOBALE (URLs ne correspondant à aucune route) → génère out/404.html.
// Avec des layouts racine par route group, ce not-found n'est wrappé par aucun layout :
// il fournit donc lui-même <html>/<body> et le thème.
const display = Space_Grotesk({ subsets: ['latin'], variable: '--font-space-grotesk', display: 'swap' });
const body = Poppins({
  subsets: ['latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-poppins',
  display: 'swap',
});

export const metadata: Metadata = {
  title: 'Page introuvable',
  robots: { index: false, follow: false },
};

export default function GlobalNotFound() {
  const site = getSite();
  return (
    <html lang="fr" data-theme={site.theme || 'shibuya'} className={`${display.variable} ${body.variable}`}>
      <body>
        <NotFoundView />
      </body>
    </html>
  );
}
