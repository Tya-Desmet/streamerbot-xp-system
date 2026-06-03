import type { Metadata } from 'next';

export const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000';
export const SITE_NAME = 'Mystya';
const DEFAULT_OG = '/og.jpg';

export function buildMetadata(opts: {
  title?: string;
  description: string;
  path?: string;
  image?: string;
  keywords?: string[];
}): Metadata {
  const url = new URL(opts.path || '/', SITE_URL).toString();
  const image = opts.image || DEFAULT_OG;
  const fullTitle = opts.title ? `${opts.title} · Mystya` : undefined;

  return {
    title: fullTitle,
    description: opts.description,
    keywords: opts.keywords,
    alternates: { canonical: url },
    openGraph: {
      title: fullTitle,
      description: opts.description,
      url,
      siteName: SITE_NAME,
      type: 'website',
      locale: 'fr_FR',
      images: [{ url: image, width: 1200, height: 630 }],
    },
    twitter: {
      card: 'summary_large_image',
      title: fullTitle,
      description: opts.description,
      images: [image],
    },
  };
}
