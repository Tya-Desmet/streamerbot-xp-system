// Barre de progression XP. Remplissage piloté par `percentage` (0-100) via
// transform: scaleX() (GPU) sur `.xpbar > i`. Aucun recalcul.
export default function XpBar({
  percentage,
  xpIntoLevel,
  xpForNext,
}: {
  percentage: number;
  xpIntoLevel: number;
  xpForNext: number;
}) {
  const pct = Math.max(0, Math.min(100, percentage));

  return (
    <div>
      <div className="flex between" style={{ fontSize: 12 }}>
        <span className="muted">Progression</span>
        <span className="muted">
          {xpIntoLevel} / {xpForNext} XP
        </span>
      </div>
      <div className="xpbar" style={{ margin: '6px 0' }}>
        <i style={{ width: '100%', transform: `scaleX(${pct / 100})` }} />
      </div>
      <p className="muted" style={{ textAlign: 'right', fontSize: 12 }}>{pct}%</p>
    </div>
  );
}
