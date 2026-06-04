import type { Metadata } from 'next';
import { getSite } from '@/lib/content';

export const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000';

// Identité de marque — pilotée par content/site.json (jamais en dur ici).
const FALLBACK_NAME = 'Stream Hub';

export function siteName(): string {
  return getSite().siteName || FALLBACK_NAME;
}
export function tagline(): string {
  return getSite().tagline || '';
}
export function ogImage(): string {
  return getSite().ogImage || '/og.jpg';
}

export function buildMetadata(opts: {
  title?: string;
  description: string;
  path?: string;
  image?: string;
  keywords?: string[];
}): Metadata {
  const name = siteName();
  const url = new URL(opts.path || '/', SITE_URL).toString();
  const image = opts.image || ogImage();
  const fullTitle = opts.title ? `${opts.title} · ${name}` : undefined;

  return {
    title: fullTitle,
    description: opts.description,
    keywords: opts.keywords,
    alternates: { canonical: url },
    openGraph: {
      title: fullTitle,
      description: opts.description,
      url,
      siteName: name,
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
