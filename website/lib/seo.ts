import type { Metadata } from 'next';

export const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000';
const DEFAULT_OG = '/og.jpg';

// Construit les métadonnées d'une page (title + description + canonical + OG + Twitter).
export function buildMetadata(opts: {
  title?: string;
  description: string;
  path?: string;
  image?: string;
}): Metadata {
  const url = new URL(opts.path || '/', SITE_URL).toString();
  const image = opts.image || DEFAULT_OG;

  return {
    title: opts.title,
    description: opts.description,
    alternates: { canonical: url },
    openGraph: {
      title: opts.title,
      description: opts.description,
      url,
      siteName: 'Stream Hub',
      type: 'website',
      locale: 'fr_FR',
      images: [{ url: image, width: 1200, height: 630 }],
    },
    twitter: {
      card: 'summary_large_image',
      title: opts.title,
      description: opts.description,
      images: [image],
    },
  };
}
