import type { Metadata } from 'next';
import { getSchedule } from '@/lib/content';
import Countdown from '@/components/Countdown';
import ScheduleCard from '@/components/ScheduleCard';
import Reveal from '@/components/Reveal';

export const metadata: Metadata = { title: 'Planning — Stream Hub' };

export default function PlanningPage() {
  const schedule = getSchedule();
  const days = schedule.days ?? [];
  const next = days.find((d) => !d.off);

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

        {schedule.nextLiveISO && next ? (
          <Reveal>
            <div className="card" style={{ padding: '26px 28px', marginBottom: 28 }}>
              <span className="chip">
                <span className="dot" /> PROCHAIN LIVE
              </span>
              <h3 style={{ fontSize: 'clamp(26px, 4vw, 40px)', margin: '14px 0 6px' }}>
                <span className="text-grad">{next.game}</span>
                {next.mode ? ` — ${next.mode}` : ''}
              </h3>
              <p className="muted" style={{ marginBottom: 16 }}>
                {next.day} {next.date} · {next.time}
                {next.dur ? ` · ${next.dur}` : ''}
              </p>
              <Countdown target={schedule.nextLiveISO} />
            </div>
          </Reveal>
        ) : null}

        {days.length > 0 ? (
          <Reveal>
            <div className="grid" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))' }}>
              {days.map((d, i) => (
                <ScheduleCard key={i} day={d} />
              ))}
            </div>
          </Reveal>
        ) : (
          <p className="muted" style={{ textAlign: 'center', padding: '40px 0' }}>
            Planning bientôt disponible.
          </p>
        )}
      </div>
    </main>
  );
}
