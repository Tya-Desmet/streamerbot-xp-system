'use client';

import { useEffect, useState } from 'react';
import type { Schedule } from '@/lib/content';
import { fetchContent } from '@/lib/api';
import Countdown from './Countdown';
import ScheduleCard from './ScheduleCard';

// Rendu du planning. Seed = données build-time (content/schedule.json) ;
// si l'API est dispo, rafraîchit depuis le backend au runtime.
export default function PlanningView({ seed }: { seed: Schedule }) {
  const [schedule, setSchedule] = useState<Schedule>(seed);

  useEffect(() => {
    let cancelled = false;
    fetchContent<Schedule>('schedule', seed).then((s) => {
      // Garde le seed build-time si l'API ne renvoie pas de jours (backend pas encore peuplé).
      if (!cancelled && s && Array.isArray(s.days) && s.days.length > 0) setSchedule(s);
    });
    return () => {
      cancelled = true;
    };
  }, [seed]);

  const days = schedule.days ?? [];
  const next = days.find((d) => !d.off);

  return (
    <>
      {schedule.nextLiveISO && next ? (
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
      ) : null}

      {days.length > 0 ? (
        <div className="grid" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))' }}>
          {days.map((d, i) => (
            <ScheduleCard key={i} day={d} />
          ))}
        </div>
      ) : (
        <p className="muted" style={{ textAlign: 'center', padding: '40px 0' }}>
          Planning bientôt disponible.
        </p>
      )}
    </>
  );
}
