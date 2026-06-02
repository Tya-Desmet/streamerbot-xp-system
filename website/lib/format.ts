// Helpers de PRÉSENTATION uniquement (aucune logique métier, aucun calcul XP).

export function formatWatchTime(minutes: number): string {
  if (!minutes || minutes <= 0) return '0 min';
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  if (h <= 0) return m + ' min';
  return h + 'h' + m.toString().padStart(2, '0');
}

export function formatXp(xp: number): string {
  return new Intl.NumberFormat('fr-FR').format(xp ?? 0) + ' XP';
}
