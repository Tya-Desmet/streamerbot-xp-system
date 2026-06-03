import type { Metadata } from 'next';
import { getSchedule } from '@/lib/content';
import PlanningView from '@/components/PlanningView';
import Reveal from '@/components/Reveal';

export const metadata: Metadata = { title: 'Planning — Stream Hub' };

export default function PlanningPage() {
  const schedule = getSchedule();

  return (
    <main className="section">
      <div className="wrap">
        <Reveal>
          <div className="section-head">
            <div>
              <div className="eyebrow">Planning · スケジュール</div>
              <h2>
                Streams de la <span className="text-grad">semaine</span>
              </h2>
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
