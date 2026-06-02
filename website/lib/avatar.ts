// Avatar « initiales en dégradé » — déterministe (même seed → même couleur).
export function avatarHue(seed: string): number {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) % 360;
  return h;
}

export function avatarGradient(seed: string): string {
  const h = avatarHue(seed);
  return `linear-gradient(135deg, oklch(0.78 0.18 ${h}), oklch(0.7 0.2 ${(h + 50) % 360}))`;
}

export function initials(name: string): string {
  return (name.replace(/[^A-Za-zÀ-ÿ0-9]/g, '').slice(0, 2) || '??').toUpperCase();
}
