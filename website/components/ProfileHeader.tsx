import type { PublicProfile } from '@/lib/contract';
import { avatarGradient, initials } from '@/lib/avatar';

export default function ProfileHeader({ profile }: { profile: PublicProfile }) {
  return (
    <header className="card flex center" style={{ gap: 18, padding: '22px 24px' }}>
      <span
        className="ava round"
        style={{ width: 72, height: 72, fontSize: 26, background: avatarGradient(profile.username) }}
      >
        {initials(profile.displayName || profile.username)}
      </span>
      <div style={{ minWidth: 0 }}>
        <h1 className="truncate" style={{ fontSize: 28 }}>
          {profile.displayName}
        </h1>
        <p className="muted">
          {profile.title ? `${profile.title} · ` : ''}Niveau {profile.level}
        </p>
        <span className="chip" style={{ marginTop: 6 }}>
          #{profile.rank} au classement
        </span>
      </div>
    </header>
  );
}
