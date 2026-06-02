import type { ScheduleDay } from '@/lib/content';

export default function ScheduleCard({ day }: { day: ScheduleDay }) {
  if (day.off) {
    return (
      <div className="card" style={{ padding: '20px 22px', opacity: 0.6 }}>
        <div className="flex between center" style={{ gap: 8 }}>
          <div>
            <b style={{ fontFamily: 'var(--font-display)' }}>{day.day}</b>{' '}
            <span className="muted">· {day.date}</span>
          </div>
          <span className="chip">Repos</span>
        </div>
        {day.note ? (
          <p className="muted" style={{ marginTop: 8, fontSize: 14 }}>{day.note}</p>
        ) : null}
      </div>
    );
  }

  return (
    <div className="card" style={{ padding: '20px 22px' }}>
      <div className="flex between center" style={{ flexWrap: 'wrap', gap: 8 }}>
        <div className="eyebrow">
          {day.day} · {day.date}
        </div>
        {day.tag ? <span className="chip">{day.tag}</span> : null}
      </div>

      <h3 style={{ fontSize: 22, margin: '12px 0 4px' }}>
        <span className="text-grad">{day.game}</span>
        {day.mode ? <span className="muted" style={{ fontSize: 16 }}> — {day.mode}</span> : null}
      </h3>

      <div className="flex gap-m" style={{ margin: '10px 0', flexWrap: 'wrap' }}>
        <span>
          <b style={{ fontFamily: 'var(--font-display)' }}>{day.time}</b>{' '}
          <span className="muted" style={{ fontSize: 12 }}>DÉBUT</span>
        </span>
        {day.dur ? (
          <span>
            <b style={{ fontFamily: 'var(--font-display)' }}>{day.dur}</b>{' '}
            <span className="muted" style={{ fontSize: 12 }}>DURÉE</span>
          </span>
        ) : null}
      </div>

      {day.note ? <p className="dim" style={{ fontSize: 14 }}>{day.note}</p> : null}
    </div>
  );
}
