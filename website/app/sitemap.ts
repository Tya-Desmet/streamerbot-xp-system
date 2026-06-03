import type { MetadataRoute } from 'next';
import { getAllUsernames } from '@/lib/data';
import { SITE_URL } from '@/lib/seo';

export const dynamic = 'force-static';

export default function sitemap(): MetadataRoute.Sitemap {
  const now = new Date();

  const routes = ['/', '/leaderboard/', '/planning/', '/ressources/'].map((p) => ({
    url: new URL(p, SITE_URL).toString(),
    lastModified: now,
  }));

  const viewers = getAllUsernames().map((u) => ({
    url: new URL(`/viewer/${u}/`, SITE_URL).toString(),
    lastModified: now,
  }));

  return [...routes, ...viewers];
}
