import type { Download } from '@/lib/content';

const ICONS: Record<string, React.ReactNode> = {
  layers: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinejoin="round" aria-hidden="true">
      <path d="m12 3 9 5-9 5-9-5 9-5Z" />
      <path d="m3 13 9 5 9-5" />
    </svg>
  ),
  image: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} aria-hidden="true">
      <rect x="3" y="4" width="18" height="16" rx="3" />
      <circle cx="8.5" cy="9.5" r="1.5" />
      <path d="m4 18 5-5 4 4 3-3 4 4" />
    </svg>
  ),
  code: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="m8 8-4 4 4 4M16 8l4 4-4 4M13 6l-2 12" />
    </svg>
  ),
  package: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinejoin="round" aria-hidden="true">
      <path d="M21 8 12 3 3 8v8l9 5 9-5V8Z" />
      <path d="m3 8 9 5 9-5M12 13v8" />
    </svg>
  ),
  music: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} aria-hidden="true">
      <circle cx="6" cy="18" r="2.5" />
      <circle cx="17" cy="16" r="2.5" />
      <path d="M8.5 18V6l11-2v10" />
    </svg>
  ),
  download: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M12 3v12m0 0 4-4m-4 4-4-4M5 21h14" />
    </svg>
  ),
};

export default function DownloadCard({ d, featured = false }: { d: Download; featured?: boolean }) {
  const icon = ICONS[d.icon] ?? ICONS.package;
  const meta = [d.ext, d.size, d.version ? `v${d.version}` : null, d.date].filter(Boolean).join(' · ');

  return (
    <div className="card" style={{ padding: featured ? '26px 28px' : '20px 22px', display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div className="flex gap-m" style={{ alignItems: 'flex-start' }}>
        <span
          style={{
            width: 48,
            height: 48,
            borderRadius: 12,
            display: 'grid',
            placeItems: 'center',
            background: 'var(--neon-1-soft)',
            color: 'var(--neon-1)',
            flexShrink: 0,
          }}
        >
          <span style={{ width: 24, height: 24, display: 'block' }}>{icon}</span>
        </span>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div className="flex center between" style={{ gap: 8 }}>
            <b style={{ fontFamily: 'var(--font-display)', fontSize: featured ? 20 : 16 }}>{d.title}</b>
            {d.featured ? (
              <span className="badge" data-b="vip">★ En avant</span>
            ) : null}
          </div>
          <div className="muted" style={{ fontSize: 12 }}>{d.cat}</div>
        </div>
      </div>

      <p className="dim" style={{ fontSize: 14 }}>{d.desc}</p>

      {d.tags.length > 0 ? (
        <div className="flex gap-s" style={{ flexWrap: 'wrap' }}>
          {d.tags.map((t) => (
            <span key={t} className="chip">{t}</span>
          ))}
        </div>
      ) : null}

      <div className="flex center between" style={{ gap: 10, marginTop: 'auto', flexWrap: 'wrap' }}>
        <span className="muted" style={{ fontSize: 12 }}>
          {meta}
          {d.dls ? ` · ${new Intl.NumberFormat('fr-FR').format(d.dls)} DL` : ''}
        </span>
        <a className="btn btn-primary btn-sm" href={d.url || '#'} download target="_blank" rel="noopener noreferrer">
          Télécharger
        </a>
      </div>
    </div>
  );
}
