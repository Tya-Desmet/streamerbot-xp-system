import type { Socials } from '@/lib/content';
import { SOCIAL_META, SOCIAL_ICON, type Platform } from './SocialCard';

// Footer : brand + petits boutons sociaux (icône seule) + mentions.
export default function Footer({ socials, siteName }: { socials: Socials; siteName: string }) {
  const platforms = (Object.keys(SOCIAL_META) as Platform[]).filter((p) => socials[p]);
  const year = new Date().getFullYear();

  return (
    <footer className="footer">
      <div className="wrap">
        <div className="brand">
          <div className="logo">{siteName.charAt(0).toUpperCase()}</div>
          <div className="name">
            {siteName.toUpperCase()}<small>STREAMLEVELS</small>
          </div>
        </div>

        <div className="footer-soc">
          {platforms.map((p) => (
            <a
              key={p}
              className={`icon-btn ${SOCIAL_META[p].cls}`}
              href={socials[p] as string}
              target="_blank"
              rel="noopener noreferrer"
              title={SOCIAL_META[p].label}
              aria-label={SOCIAL_META[p].label}
              style={{ background: 'transparent' }}
            >
              {SOCIAL_ICON[p]}
            </a>
          ))}
        </div>

        <div className="fine">
          <span>© {year} {siteName} · StreamLevels</span>
          <span className="flex center gap-m" style={{ flexWrap: 'wrap' }}>
            <a href="/privacy/" style={{ textDecoration: 'underline' }}>Confidentialité</a>
            <span>Propulsé par Streamer.bot XP System</span>
          </span>
        </div>
      </div>
    </footer>
  );
}
