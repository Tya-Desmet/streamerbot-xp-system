import type { MetadataRoute } from 'next';
import { siteName } from '@/lib/seo';

export const dynamic = 'force-static';

export default function manifest(): MetadataRoute.Manifest {
  const name = siteName();
  return {
    name: `${name} — Hub communautaire`,
    short_name: name,
    description: `Hub communautaire de ${name} — classement XP, planning des streams et ressources.`,
    start_url: '/',
    display: 'standalone',
    background_color: '#0b0b12',
    theme_color: '#a855f7',
    icons: [{ src: '/icon.svg', sizes: 'any', type: 'image/svg+xml' }],
  };
}
