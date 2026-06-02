'use client';

import { useEffect, useState } from 'react';
import { usePathname } from 'next/navigation';
import ThemeDots from './ThemeDots';

const LINKS = [
  { href: '/', label: 'Accueil' },
  { href: '/leaderboard/', label: 'Leaderboard' },
  { href: '/planning/', label: 'Planning' },
  { href: '/downloads/', label: 'Downloads' },
];

function norm(p: string) {
  return p !== '/' && p.endsWith('/') ? p.slice(0, -1) : p;
}

const ICON_PLAY = (
  <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
    <path d="M8 5v14l11-7z" />
  </svg>
);
const ICON_SOUND = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <path d="M11 5 6 9H3v6h3l5 4V5Z" />
    <path d="M16 9a4 4 0 0 1 0 6" />
  </svg>
);
const ICON_MUTE = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    <path d="M11 5 6 9H3v6h3l5 4V5Z" />
    <path d="m17 9 4 6M21 9l-4 6" />
  </svg>
);
const ICON_BURGER = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" aria-hidden="true">
    <path d="M4 7h16M4 12h16M4 17h16" />
  </svg>
);

export default function NavBar({ twitchUrl }: { twitchUrl: string }) {
  const pathname = usePathname() || '/';
  const cur = norm(pathname);
  const [open, setOpen] = useState(false);
  const [muted, setMuted] = useState(true);

  useEffect(() => {
    try {
      setMuted(localStorage.getItem('sl-sound') !== 'on');
    } catch {
      /* ignore */
    }
  }, []);

  function toggleSound() {
    setMuted((m) => {
      const next = !m;
      try {
        localStorage.setItem('sl-sound', next ? 'off' : 'on');
      } catch {
        /* ignore */
      }
      return next;
    });
  }

  return (
    <header className="nav">
      <div className="wrap">
        <a className="brand" href="/">
          <div className="logo">M</div>
          <div className="name">
            MYSTYA<small>STREAMLEVELS</small>
          </div>
        </a>

        <nav className={`nav-links ${open ? 'open' : ''}`} onClick={() => setOpen(false)}>
          {LINKS.map((l) => (
            <a key={l.href} href={l.href} className={cur === norm(l.href) ? 'active' : ''}>
              {l.label}
            </a>
          ))}
        </nav>

        <div className="nav-actions">
          <ThemeDots />
          <button
            type="button"
            className="icon-btn"
            aria-pressed={!muted}
            title={muted ? 'Activer le son' : 'Couper le son'}
            onClick={toggleSound}
          >
            {muted ? ICON_MUTE : ICON_SOUND}
          </button>
          <a className="btn btn-primary btn-sm" href={twitchUrl} target="_blank" rel="noopener noreferrer">
            {ICON_PLAY}
            Live
          </a>
          <button type="button" className="nav-burger icon-btn" aria-label="Menu" onClick={() => setOpen((o) => !o)}>
            {ICON_BURGER}
          </button>
        </div>
      </div>
    </header>
  );
}
