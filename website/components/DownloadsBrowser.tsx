'use client';

import { useEffect, useState } from 'react';
import type { Download } from '@/lib/content';
import { fetchContent } from '@/lib/api';
import DownloadCard from './DownloadCard';

export default function DownloadsBrowser({ items: seed }: { items: Download[] }) {
  const [items, setItems] = useState<Download[]>(seed);
  const [cat, setCat] = useState('Tous');

  // Rafraîchit depuis l'API (kind "resources") si dispo ; sinon garde le seed.
  useEffect(() => {
    let cancelled = false;
    fetchContent<Download[]>('resources', seed).then((list) => {
      if (!cancelled && Array.isArray(list) && list.length > 0) setItems(list);
    });
    return () => {
      cancelled = true;
    };
  }, [seed]);

  const featured = items.find((d) => d.featured);
  const rest = items.filter((d) => !d.featured);
  const cats = ['Tous', ...Array.from(new Set(rest.map((d) => d.cat)))];
  const shown = cat === 'Tous' ? rest : rest.filter((d) => d.cat === cat);

  return (
    <>
      {featured ? (
        <div style={{ marginBottom: 28 }}>
          <DownloadCard d={featured} featured />
        </div>
      ) : null}

      <div className="flex center gap-s" style={{ flexWrap: 'wrap', marginBottom: 20 }}>
        {cats.map((c) => (
          <button
            key={c}
            type="button"
            className={`btn btn-sm ${c === cat ? 'btn-primary' : 'btn-ghost'}`}
            onClick={() => setCat(c)}
          >
            {c}
          </button>
        ))}
      </div>

      <div className="grid" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))' }}>
        {shown.map((d) => (
          <DownloadCard key={d.id} d={d} />
        ))}
      </div>

      {shown.length === 0 ? (
        <p className="muted" style={{ textAlign: 'center', padding: '30px 0' }}>
          Aucune ressource dans cette catégorie.
        </p>
      ) : null}
    </>
  );
}
