'use client';

import { THEMES } from '@/lib/theme';
import { useTheme } from './ThemeProvider';

export default function ThemeDots() {
  const { theme, setTheme } = useTheme();

  return (
    <div className="theme-dots" role="group" aria-label="Thème">
      {THEMES.map((t) => (
        <button
          key={t.id}
          type="button"
          className={`tdot-${t.id}`}
          aria-pressed={theme === t.id}
          title={`${t.label} · ${t.jp}`}
          onClick={() => setTheme(t.id)}
        />
      ))}
    </div>
  );
}
