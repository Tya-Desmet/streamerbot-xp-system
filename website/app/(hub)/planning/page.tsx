import { notFound } from 'next/navigation';
import { getSchedule, isFeatureOn } from '@/lib/content';
import PlanningView from '@/components/PlanningView';
import Reveal from '@/components/Reveal';
import { buildMetadata, siteName } from '@/lib/seo';

const s = siteName();

export const metadata = buildMetadata({
  title: 'Planning des streams',
  description: `Programme des lives de ${s} — horaires, jeux de la semaine et compte à rebours du prochain stream.`,
  path: '/planning/',
  keywords: [`planning stream ${s}`, `prochain live ${s}`, `horaires stream ${s}`],
});

export default function PlanningPage() {
  if (!isFeatureOn('planning')) notFound();
  const schedule = getSchedule();

  return (
    <main className="section">
      <div className="wrap">
        <Reveal>
          <div className="section-head">
            <div>
              <div className="eyebrow">Planning · スケジュール</div>
              <h1>
                Streams de la <span className="text-grad">semaine</span>
              </h1>
              <p className="dim">
                Horaires {schedule.timezone || 'France (CET)'}. Reset du classement hebdo chaque jeudi soir.
              </p>
            </div>
          </div>
        </Reveal>
        <Reveal>
          <PlanningView seed={schedule} />
        </Reveal>
      </div>
    </main>
  );
}
