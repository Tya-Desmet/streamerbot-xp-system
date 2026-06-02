/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './app/**/*.{ts,tsx}',
    './components/**/*.{ts,tsx}',
  ],
  theme: {
    extend: {
      // Variables CSS de thème (branding streamer) — définies en P09.
      colors: {
        accent: 'var(--accent, #a855f7)',
      },
    },
  },
  plugins: [],
};
