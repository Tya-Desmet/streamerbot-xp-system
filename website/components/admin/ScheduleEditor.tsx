'use client';

import { useEffect, useState } from 'react';
import type { Schedule, ScheduleDay } from '@/lib/content';
import { downloadJson } from '@/lib/exportJson';

const KEY = 'sl-admin-schedule';

const inp: React.CSSProperties = {
  height: 36,
  padding: '0 10px',
  borderRadius: 8,
  background: 'var(--surface)',
  border: '1px solid var(--line-strong)',
  color: 'var(--text)',
  minWidth: 0,
};

export default function ScheduleEditor({ seed }: { seed: Schedule }) {
  const [sched, setSched] = useState<Schedule>(seed);

  useEffect(() => {
    try {
      const s = localStorage.getItem(KEY);
      if (s) setSched(JSON.parse(s));
    } catch {
      /* ignore */
    }
  }, []);

  function persist(next: Schedule) {
    setSched(next);
    try {
      localStorage.setItem(KEY, JSON.stringify(next));
    } catch {
      /* ignore */
    }
  }

  const setTop = (k: keyof Schedule, v: string) => persist({ ...sched, [k]: v });
  const setDay = <K extends keyof ScheduleDay>(i: number, k: K, v: ScheduleDay[K]) =>
    persist({ ...sched, days: sched.days.map((d, idx) => (idx === i ? { ...d, [k]: v } : d)) });

  return (
    <div className="flex" style={{ flexDirection: 'column', gap: 16 }}>
      <div className="card flex gap-s" style={{ padding: '16px 18px', flexWrap: 'wrap' }}>
        <label className="flex" style={{ flexDirection: 'column', gap: 4, fontSize: 12 }}>
          <span className="muted">Prochain live (ISO)</span>
          <input style={{ ...inp, minWidth: 240 }} value={sched.nextLiveISO ?? ''} onChange={(e) => setTop('nextLiveISO', e.target.value)} placeholder="2026-06-06T20:30:00+02:00" />
        </label>
        <label className="flex" style={{ flexDirection: 'column', gap: 4, fontSize: 12 }}>
          <span className="muted">Fuseau</span>
          <input style={inp} value={sched.timezone ?? ''} onChange={(e) => setTop('timezone', e.target.value)} placeholder="France (CET)" />
        </label>
      </div>

      {sched.days.map((d, i) => (
        <div key={i} className="card" style={{ padding: '14px 16px', display: 'grid', gap: 8, gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))' }}>
          <div className="flex center gap-s" style={{ gridColumn: '1 / -1' }}>
            <b style={{ fontFamily: 'var(--font-display)' }}>{d.day}</b>
            <span className="muted">· {d.date}</span>
            <label className="flex center gap-s muted" style={{ marginLeft: 'auto', fontSize: 13 }}>
              <input type="checkbox" checked={d.off} onChange={(e) => setDay(i, 'off', e.target.checked)} /> Repos
            </label>
          </div>
          {!d.off ? (
            <>
              <input style={inp} placeholder="Jeu" value={d.game ?? ''} onChange={(e) => setDay(i, 'game', e.target.value)} />
              <input style={inp} placeholder="Mode" value={d.mode ?? ''} onChange={(e) => setDay(i, 'mode', e.target.value)} />
              <input style={inp} placeholder="Heure" value={d.time ?? ''} onChange={(e) => setDay(i, 'time', e.target.value)} />
              <input style={inp} placeholder="Durée" value={d.dur ?? ''} onChange={(e) => setDay(i, 'dur', e.target.value)} />
              <input style={inp} placeholder="Tag" value={d.tag ?? ''} onChange={(e) => setDay(i, 'tag', e.target.value)} />
            </>
          ) : null}
          <input style={{ ...inp, gridColumn: '1 / -1' }} placeholder="Note" value={d.note ?? ''} onChange={(e) => setDay(i, 'note', e.target.value)} />
        </div>
      ))}

      <div>
        <button className="btn btn-primary btn-sm" type="button" onClick={() => downloadJson('schedule.json', sched)}>
          Exporter schedule.json
        </button>
      </div>
      <p className="muted" style={{ fontSize: 12 }}>
        Édité localement. Exporte le JSON puis remplace <code>content/schedule.json</code> et rebuild.
      </p>
    </div>
  );
}
