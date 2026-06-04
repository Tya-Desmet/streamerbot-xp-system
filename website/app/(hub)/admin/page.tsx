import { notFound } from 'next/navigation';
import { getDownloads, getSchedule, getFriends, getSite } from '@/lib/content';
import AdminAuth from '@/components/admin/AdminAuth';
import ThemeEditor from '@/components/admin/ThemeEditor';
import FriendsEditor from '@/components/admin/FriendsEditor';
import DownloadsEditor from '@/components/admin/DownloadsEditor';
import ScheduleEditor from '@/components/admin/ScheduleEditor';

// Admin accessible si : flag dev (.env.development) OU backend configuré (NEXT_PUBLIC_API_URL).
// - Sans backend : mode local (export JSON).
// - Avec backend : login (AdminAuth) → publication directe via l'API. AUCUN contrôle d'XP.
const ADMIN_ENABLED =
  process.env.NEXT_PUBLIC_ENABLE_ADMIN === '1' || !!process.env.NEXT_PUBLIC_API_URL;

export default function AdminPage() {
  if (!ADMIN_ENABLED) notFound();

  const site = getSite();
  const friends = getFriends();
  const downloads = getDownloads();
  const schedule = getSchedule();

  return (
    <main className="section">
      <div className="wrap">
        <div className="eyebrow">Admin · 管理</div>
        <h2>Admin éditorial</h2>
        <p className="dim" style={{ marginBottom: 24, maxWidth: '64ch' }}>
          Gestion <b>copains</b>, <b>ressources</b>, <b>planning</b> &amp; <b>thème</b>. Aucun contrôle
          d&apos;XP (l&apos;XP se gère dans Streamer.bot). Connecté au backend → <b>publication directe</b> ;
          sinon édition locale + export JSON.
        </p>

        <AdminAuth>
          <h3 style={{ margin: '8px 0 12px' }}>Thème &amp; chaîne</h3>
          <ThemeEditor seed={site} />

          <h3 style={{ margin: '44px 0 12px' }}>Copains en live</h3>
          <FriendsEditor seed={friends} />

          <h3 style={{ margin: '44px 0 12px' }}>Ressources</h3>
          <DownloadsEditor seed={downloads} />

          <h3 style={{ margin: '44px 0 12px' }}>Planning</h3>
          <ScheduleEditor seed={schedule} />
        </AdminAuth>
      </div>
    </main>
  );
}
