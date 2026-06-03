import type { Metadata } from 'next';
import { getDownloads } from '@/lib/content';
import DownloadsBrowser from '@/components/DownloadsBrowser';
import Reveal from '@/components/Reveal';

export const metadata: Metadata = { title: 'Ressources — Stream Hub' };

export default function RessourcesPage() {
  const items = getDownloads();

  return (
    <main className="section">
      <div className="wrap">
        <Reveal>
          <div className="section-head">
            <div>
              <div className="eyebrow">Ressources · リソース</div>
              <h2>
                À <span className="text-grad">télécharger</span>
              </h2>
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
