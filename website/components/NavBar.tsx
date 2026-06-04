'use client';

import { useState } from 'react';
import { usePathname } from 'next/navigation';
import LiveBadge from './LiveBadge';

type NavFeatures = { planning?: boolean; ressources?: boolean };

const LINKS: { href: string; label: string; feature?: keyof NavFeatures }[] = [
  { href: '/', label: 'Accueil' },
  { href: '/leaderboard/', label: 'Leaderboard' },
  { href: '/planning/', label: 'Planning', feature: 'planning' },
  { href: '/ressources/', label: 'Ressources', feature: 'ressources' },
];

function norm(p: string) {
  return p !== '/' && p.endsWith('/') ? p.slice(0, -1) : p;
}

const ICON_BURGER = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" aria-hidden="true">
    <path d="M4 7h16M4 12h16M4 17h16" />
  </svg>
);

export default function NavBar({
  twitchUrl,
  twitchChannel,
  siteName,
  features = {},
}: {
  twitchUrl: string;
  twitchChannel: string;
  siteName: string;
  features?: NavFeatures;
}) {
  const pathname = usePathname() || '/';
  const cur = norm(pathname);
  const [open, setOpen] = useState(false);
  const links = LINKS.filter((l) => !l.feature || features[l.feature] !== false);

  return (
    <header className="nav">
      <div className="wrap">
        <a className="brand" href="/">
          <div className="logo">{siteName.charAt(0).toUpperCase()}</div>
          <div className="name">
            {siteName.toUpperCase()}<small>STREAMLEVELS</small>
          </div>
        </a>

        <nav className={`nav-links ${open ? 'open' : ''}`} onClick={() => setOpen(false)}>
          {links.map((l) => (
            <a key={l.href} href={l.href} className={cur === norm(l.href) ? 'active' : ''}>
              {l.label}
            </a>
          ))}
        </nav>

        <div className="nav-actions">
          <LiveBadge channel={twitchChannel} twitchUrl={twitchUrl} />
          <button type="button" className="nav-burger icon-btn" aria-label="Menu" onClick={() => setOpen((o) => !o)}>
            {ICON_BURGER}
          </button>
        </div>
      </div>
    </header>
  );
}
