import type { Socials } from '@/lib/content';

export type Platform = keyof Socials;

export const SOCIAL_META: Record<Platform, { label: string; cls: string; tagline: string }> = {
  twitch: { label: 'Twitch', cls: 'sc-twitch', tagline: 'Suis le live' },
  youtube: { label: 'YouTube', cls: 'sc-youtube', tagline: 'Vidéos & VODs' },
  instagram: { label: 'Instagram', cls: 'sc-insta', tagline: 'Coulisses' },
  tiktok: { label: 'TikTok', cls: 'sc-tiktok', tagline: 'Clips' },
  discord: { label: 'Discord', cls: 'sc-discord', tagline: 'Rejoins la commu' },
};

export const SOCIAL_ICON: Record<Platform, React.ReactNode> = {
  twitch: (
    <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M4 3 3 7v12h4v3h3l3-3h4l5-5V3H4Zm16 9-3 3h-4l-3 3v-3H7V5h13v7Z" />
      <path d="M17 7h-2v5h2V7Zm-5 0h-2v5h2V7Z" />
    </svg>
  ),
  youtube: (
    <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M23 12s0-3.2-.4-4.7a2.5 2.5 0 0 0-1.8-1.8C19.3 5 12 5 12 5s-7.3 0-8.8.5A2.5 2.5 0 0 0 1.4 7.3C1 8.8 1 12 1 12s0 3.2.4 4.7a2.5 2.5 0 0 0 1.8 1.8C4.7 19 12 19 12 19s7.3 0 8.8-.5a2.5 2.5 0 0 0 1.8-1.8C23 15.2 23 12 23 12ZM10 15V9l5 3-5 3Z" />
    </svg>
  ),
  instagram: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} aria-hidden="true">
      <rect x="3" y="3" width="18" height="18" rx="5" />
      <circle cx="12" cy="12" r="4" />
      <circle cx="17.5" cy="6.5" r="1" fill="currentColor" stroke="none" />
    </svg>
  ),
  tiktok: (
    <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M16 3c.3 2.3 1.7 3.9 4 4.2v3c-1.5 0-2.9-.4-4-1.1V15a6 6 0 1 1-6-6c.3 0 .7 0 1 .1v3.1A3 3 0 1 0 13 15V3h3Z" />
    </svg>
  ),
  discord: (
    <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <path d="M20 5.3A17 17 0 0 0 15.7 4l-.3.5a13 13 0 0 1 3.7 1.8 12 12 0 0 0-14.2 0A13 13 0 0 1 8.6 4.5L8.3 4A17 17 0 0 0 4 5.3 18 18 0 0 0 1 17a17 17 0 0 0 5.2 2.6l.6-1a11 11 0 0 1-1.8-.9l.4-.3a12 12 0 0 0 10.2 0l.4.3c-.6.4-1.2.6-1.8.9l.6 1A17 17 0 0 0 23 17a18 18 0 0 0-3-11.7ZM9 14.3c-.8 0-1.5-.8-1.5-1.7S8.2 11 9 11s1.5.8 1.5 1.7-.7 1.6-1.5 1.6Zm6 0c-.8 0-1.5-.8-1.5-1.7S14.2 11 15 11s1.5.8 1.5 1.7-.7 1.6-1.5 1.6Z" />
    </svg>
  ),
};

// Carte sociale "pleine" (utilisée sur la Home).
export default function SocialCard({ platform, url }: { platform: Platform; url: string }) {
  const m = SOCIAL_META[platform];
  return (
    <a className="soc" href={url} target="_blank" rel="noopener noreferrer">
      <span className={`ic ${m.cls}`}>{SOCIAL_ICON[platform]}</span>
      <span className="meta">
        <b>{m.label}</b>
        <span>{m.tagline}</span>
      </span>
      <span className="go" aria-hidden="true">↗</span>
    </a>
  );
}
