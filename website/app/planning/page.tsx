import { getSchedule } from '@/lib/content';
import PlanningView from '@/components/PlanningView';
import Reveal from '@/components/Reveal';
import { buildMetadata } from '@/lib/seo';

export const metadata = buildMetadata({
  title: 'Planning',
  description: 'Le programme des streams de la semaine et le compte à rebours du prochain live.',
  path: '/planning/',
});

export default function PlanningPage() {
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
