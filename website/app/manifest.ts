import type { MetadataRoute } from 'next';

export const dynamic = 'force-static';

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: 'Stream Hub — Mystya',
    short_name: 'Stream Hub',
    description: 'Hub communautaire — classement XP, planning des streams et ressources.',
    start_url: '/',
    display: 'standalone',
    background_color: '#0b0b12',
    theme_color: '#a855f7',
    icons: [{ src: '/icon.svg', sizes: 'any', type: 'image/svg+xml' }],
  };
}
