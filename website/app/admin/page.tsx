import { notFound } from 'next/navigation';
import { getDownloads, getSchedule } from '@/lib/content';
import DownloadsEditor from '@/components/admin/DownloadsEditor';
import ScheduleEditor from '@/components/admin/ScheduleEditor';

// Admin éditorial LOCAL : actif uniquement en dev (.env.development pose
// NEXT_PUBLIC_ENABLE_ADMIN=1). En build de production, la variable est absente
// → notFound() → la page n'est pas servie. AUCUN contrôle d'XP.
const ADMIN_ENABLED = process.env.NEXT_PUBLIC_ENABLE_ADMIN === '1';

export default function AdminPage() {
  if (!ADMIN_ENABLED) notFound();

  const downloads = getDownloads();
  const schedule = getSchedule();

  return (
    <main className="section">
      <div className="wrap">
        <div className="eyebrow">Admin · 管理</div>
        <h2>
          Admin éditorial <span className="muted" style={{ fontSize: 16 }}>(local · dev)</span>
        </h2>
        <p className="dim" style={{ marginBottom: 24, maxWidth: '60ch' }}>
          Gestion du contenu <b>downloads</b> &amp; <b>planning</b>. Aucun contrôle d&apos;XP — l&apos;XP se gère
          dans Streamer.bot. Édite, puis <b>exporte le JSON</b> et recopie-le dans <code>content/</code>.
        </p>

        <h3 style={{ margin: '24px 0 12px' }}>Downloads</h3>
        <DownloadsEditor seed={downloads} />

        <h3 style={{ margin: '44px 0 12px' }}>Planning</h3>
        <ScheduleEditor seed={schedule} />
      </div>
    </main>
  );
}
