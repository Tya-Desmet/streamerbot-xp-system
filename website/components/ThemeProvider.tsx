'use client';

import { createContext, useContext, useEffect, useState } from 'react';
import { DEFAULT_THEME, THEME_KEY, type ThemeId } from '@/lib/theme';

type Ctx = { theme: ThemeId; setTheme: (t: ThemeId) => void };

const ThemeContext = createContext<Ctx>({ theme: DEFAULT_THEME, setTheme: () => {} });

export const useTheme = () => useContext(ThemeContext);

export default function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [theme, setThemeState] = useState<ThemeId>(DEFAULT_THEME);

  // Applique le thème mémorisé au montage (shibuya = valeurs :root par défaut).
  useEffect(() => {
    let stored: ThemeId | null = null;
    try {
      stored = localStorage.getItem(THEME_KEY) as ThemeId | null;
    } catch {
      /* localStorage indisponible */
    }
    const initial = stored ?? DEFAULT_THEME;
    setThemeState(initial);
    document.documentElement.setAttribute('data-theme', initial);
  }, []);

  function setTheme(t: ThemeId) {
    setThemeState(t);
    document.documentElement.setAttribute('data-theme', t);
    try {
      localStorage.setItem(THEME_KEY, t);
    } catch {
      /* ignore */
    }
  }

  return <ThemeContext.Provider value={{ theme, setTheme }}>{children}</ThemeContext.Provider>;
}
