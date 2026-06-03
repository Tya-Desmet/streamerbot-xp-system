import type { MetadataRoute } from 'next';
import { getAllUsernames } from '@/lib/data';
import { SITE_URL } from '@/lib/seo';

export const dynamic = 'force-static';

export default function sitemap(): MetadataRoute.Sitemap {
  const now = new Date();

  const routes: MetadataRoute.Sitemap = [
    { url: new URL('/', SITE_URL).toString(), lastModified: now, changeFrequency: 'daily', priority: 1.0 },
    { url: new URL('/leaderboard/', SITE_URL).toString(), lastModified: now, changeFrequency: 'hourly', priority: 0.9 },
    { url: new URL('/planning/', SITE_URL).toString(), lastModified: now, changeFrequency: 'weekly', priority: 0.8 },
    { url: new URL('/ressources/', SITE_URL).toString(), lastModified: now, changeFrequency: 'monthly', priority: 0.6 },
  ];

  const viewers: MetadataRoute.Sitemap = getAllUsernames().map((u) => ({
    url: new URL(`/viewer/${u}/`, SITE_URL).toString(),
    lastModified: now,
    changeFrequency: 'daily',
    priority: 0.5,
  }));

  return [...routes, ...viewers];
}
