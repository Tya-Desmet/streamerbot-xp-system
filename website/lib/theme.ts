export type ThemeId = 'shibuya' | 'akiba' | 'hara';

export const THEMES: { id: ThemeId; label: string; jp: string }[] = [
  { id: 'shibuya', label: 'Shibuya', jp: '渋谷' },
  { id: 'akiba', label: 'Akihabara', jp: '秋葉原' },
  { id: 'hara', label: 'Yozakura', jp: '夜桜' },
];

export const DEFAULT_THEME: ThemeId = 'shibuya';
export const THEME_KEY = 'sl-theme';
