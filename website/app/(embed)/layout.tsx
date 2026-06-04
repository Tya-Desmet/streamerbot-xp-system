import type { Metadata } from 'next';
import { Space_Grotesk, Poppins } from 'next/font/google';
import '../globals.css';

// Layout racine DÉDIÉ à l'embed (route group (embed)) : aucun NavBar/Footer/JSON-LD
// du hub. <html>/<body> nus, fond transparent — le composant peint son propre fond.
// Polices identiques au hub pour rester cohérent visuellement.
const display = Space_Grotesk({ subsets: ['latin'], variable: '--font-space-grotesk', display: 'swap' });
const body = Poppins({
  subsets: ['latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-poppins',
  display: 'swap',
});

export const metadata: Metadata = {
  // L'embed n'est pas une page à indexer pour elle-même (il vit en iframe).
  robots: { index: false, follow: false },
};

export default function EmbedLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="fr" className={`${display.variable} ${body.variable}`}>
      <body style={{ margin: 0, background: 'transparent', backgroundImage: 'none', minHeight: 0 }}>
        {children}
      </body>
    </html>
  );
}
