import { notFound } from 'next/navigation';
import { getDownloads, isFeatureOn } from '@/lib/content';
import DownloadsBrowser from '@/components/DownloadsBrowser';
import Reveal from '@/components/Reveal';
import { buildMetadata } from '@/lib/seo';

export const metadata = buildMetadata({
  title: 'Ressources',
  description: 'Overlays, packs, templates et ressources gratuites à télécharger.',
  path: '/ressources/',
});

export default function RessourcesPage() {
  if (!isFeatureOn('ressources')) notFound();
  const items = getDownloads();

  return (
    <main className="section">
      <div className="wrap">
        <Reveal>
          <div className="section-head">
            <div>
              <div className="eyebrow">Ressources · リソース</div>
              <h1>
                À <span className="text-grad">télécharger</span>
              </h1>
              <p className="dim">Overlays, packs, templates — gratuits, prêts à l&apos;emploi.</p>
            </div>
          </div>
        </Reveal>

        {items.length > 0 ? (
          <Reveal>
            <DownloadsBrowser items={items} />
          </Reveal>
        ) : (
          <p className="muted" style={{ textAlign: 'center', padding: '40px 0' }}>
            Aucune ressource pour le moment.
          </p>
        )}
      </div>
    </main>
  );
}
