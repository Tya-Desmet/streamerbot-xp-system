'use client';

import { useEffect, useState } from 'react';
import type { Site } from '@/lib/content';
import { THEMES } from '@/lib/theme';
import { downloadJson } from '@/lib/exportJson';
import PublishButton from './PublishButton';

const KEY = 'sl-admin-site';
const inp: React.CSSProperties = {
  height: 40,
  padding: '0 12px',
  borderRadius: 10,
  background: 'var(--surface)',
  border: '1px solid var(--line-strong)',
  color: 'var(--text)',
};

export default function ThemeEditor({ seed }: { seed: Site }) {
  const [site, setSite] = useState<Site>(seed);

  useEffect(() => {
    try {
      const s = localStorage.getItem(KEY);
      if (s) setSite(JSON.parse(s));
    } catch {
      /* ignore */
    }
  }, []);

  function update(next: Site) {
    setSite(next);
    try {
      localStorage.setItem(KEY, JSON.stringify(next));
    } catch {
      /* ignore */
    }
  }

  return (
    <div className="flex" style={{ flexDirection: 'column', gap: 14 }}>
      <div className="card" style={{ padding: '16px 18px', display: 'flex', gap: 20, flexWrap: 'wrap', alignItems: 'flex-end' }}>
        <label className="flex" style={{ flexDirection: 'column', gap: 4, fontSize: 12 }}>
          <span className="muted">Chaîne Twitch (login)</span>
          <input style={inp} value={site.twitchChannel} onChange={(e) => update({ ...site, twitchChannel: e.target.value })} placeholder="mystya" />
        </label>
        <div>
          <span className="muted" style={{ fontSize: 12, display: 'block', marginBottom: 6 }}>
            Thème du site (visible par tous — les visiteurs ne le changent pas)
          </span>
          <div className="flex gap-s" style={{ flexWrap: 'wrap' }}>
            {THEMES.map((t) => (
              <button
                key={t.id}
                type="button"
                className={`btn btn-sm ${site.theme === t.id ? 'btn-primary' : 'btn-ghost'}`}
                onClick={() => update({ ...site, theme: t.id })}
              >
                {t.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      <div className="flex center gap-s" style={{ flexWrap: 'wrap' }}>
        <PublishButton kind="site" data={site} />
        <button className="btn btn-ghost btn-sm" type="button" onClick={() => downloadJson('site.json', site)}>
          Exporter site.json
        </button>
      </div>
    </div>
  );
}
