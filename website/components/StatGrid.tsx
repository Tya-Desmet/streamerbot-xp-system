import type { PublicProfile } from '@/lib/contract';
import { formatWatchTime } from '@/lib/format';

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="card" style={{ padding: '14px 16px' }}>
      <p className="muted" style={{ fontSize: 12 }}>{label}</p>
      <p style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 20 }}>{value}</p>
    </div>
  );
}

export default function StatGrid({ profile }: { profile: PublicProfile }) {
  const s = profile.sources;
  const nf = new Intl.NumberFormat('fr-FR');

  return (
    <section className="flex" style={{ flexDirection: 'column', gap: 16 }}>
      <div className="grid" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))' }}>
        <Stat label="XP total" value={nf.format(profile.xp)} />
        <Stat label="Messages" value={nf.format(profile.messages)} />
        <Stat label="Watchtime" value={formatWatchTime(profile.watchTime)} />
        <Stat label="Check-in" value={`${profile.totalCheckIns ?? 0}`} />
      </div>

      <div>
        <p className="muted" style={{ fontSize: 12, marginBottom: 8 }}>Sources d&apos;XP</p>
        <div className="grid" style={{ gridTemplateColumns: 'repeat(3, 1fr)' }}>
          <Stat label="Chat" value={nf.format(s.chat)} />
          <Stat label="Watchtime" value={nf.format(s.watch)} />
          <Stat label="Rewards" value={nf.format(s.rewards)} />
        </div>
      </div>

      {/* badges : réactivés en R10 (extension du contrat d'export) */}
    </section>
  );
}
