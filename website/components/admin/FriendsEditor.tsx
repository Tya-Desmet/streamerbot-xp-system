'use client';

import { useEffect, useState } from 'react';
import type { Friend } from '@/lib/content';
import { downloadJson } from '@/lib/exportJson';
import { channelFromUrl } from '@/lib/live';

const KEY = 'sl-admin-friends';

const blank = (): Friend => ({
  name: '',
  handle: '',
  channel: '',
  live: false,
  game: '',
  viewers: 0,
  hue: Math.floor(Math.random() * 360),
  url: '',
});

const inp: React.CSSProperties = {
  width: '100%',
  height: 40,
  padding: '0 12px',
  borderRadius: 10,
  background: 'var(--surface)',
  border: '1px solid var(--line-strong)',
  color: 'var(--text)',
};

export default function FriendsEditor({ seed }: { seed: Friend[] }) {
  const [items, setItems] = useState<Friend[]>(seed);
  const [draft, setDraft] = useState<Friend>(blank());

  useEffect(() => {
    try {
      const s = localStorage.getItem(KEY);
      if (s) setItems(JSON.parse(s));
    } catch {
      /* ignore */
    }
  }, []);

  function persist(next: Friend[]) {
    setItems(next);
    try {
      localStorage.setItem(KEY, JSON.stringify(next));
    } catch {
      /* ignore */
    }
  }

  function save() {
    if (!draft.name.trim()) return;
    const channel = (draft.channel || channelFromUrl(draft.url)).trim();
    const url = draft.url || (channel ? `https://www.twitch.tv/${channel}` : '');
    const handle = draft.handle || (channel ? `@${channel}` : draft.name);
    const d: Friend = { ...draft, channel, url, handle };
    const exists = items.some((x) => x.handle === d.handle);
    persist(exists ? items.map((x) => (x.handle === d.handle ? d : x)) : [...items, d]);
    setDraft(blank());
  }

  const set = <K extends keyof Friend>(k: K, v: Friend[K]) => setDraft((d) => ({ ...d, [k]: v }));

  return (
    <div className="flex" style={{ flexDirection: 'column', gap: 16 }}>
      <div className="card" style={{ padding: '18px 20px', display: 'grid', gap: 10, gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))' }}>
        <input style={inp} placeholder="Nom affiché" value={draft.name} onChange={(e) => set('name', e.target.value)} />
        <input style={inp} placeholder="Chaîne Twitch (login)" value={draft.channel ?? ''} onChange={(e) => set('channel', e.target.value)} />
        <input style={inp} placeholder="Jeu (optionnel)" value={draft.game} onChange={(e) => set('game', e.target.value)} />
        <input style={inp} placeholder="URL (auto si vide)" value={draft.url} onChange={(e) => set('url', e.target.value)} />
        <label className="flex center gap-s muted" style={{ fontSize: 13 }}>
          Teinte avatar
          <input type="range" min={0} max={360} value={draft.hue} onChange={(e) => set('hue', Number(e.target.value))} />
        </label>
        <div className="flex gap-s" style={{ gridColumn: '1 / -1' }}>
          <button className="btn btn-primary btn-sm" type="button" onClick={save}>
            {items.some((x) => x.handle === draft.handle) && draft.handle ? 'Enregistrer' : 'Ajouter'}
          </button>
          <button className="btn btn-ghost btn-sm" type="button" onClick={() => setDraft(blank())}>Nouveau</button>
        </div>
        <p className="muted" style={{ fontSize: 12, gridColumn: '1 / -1' }}>
          Le statut « en live » est détecté automatiquement (decapi) — pas besoin de le saisir.
        </p>
      </div>

      <ul className="flex" style={{ flexDirection: 'column', gap: 8 }}>
        {items.map((f) => (
          <li key={f.handle} className="card flex center" style={{ gap: 10, padding: '10px 14px' }}>
            <div style={{ flex: 1, minWidth: 0 }}>
              <b className="truncate" style={{ display: 'block' }}>{f.name}</b>
              <span className="muted" style={{ fontSize: 12 }}>{f.channel || channelFromUrl(f.url) || '—'}</span>
            </div>
            <button className="btn btn-ghost btn-sm" type="button" onClick={() => setDraft({ ...f })}>Éditer</button>
            <button className="btn btn-ghost btn-sm" type="button" onClick={() => persist(items.filter((x) => x.handle !== f.handle))}>✕</button>
          </li>
        ))}
      </ul>

      <div className="flex gap-s">
        <button className="btn btn-primary btn-sm" type="button" onClick={() => downloadJson('friends.json', items)}>
          Exporter friends.json
        </button>
        <button className="btn btn-ghost btn-sm" type="button" onClick={() => persist(seed)}>Réinitialiser</button>
      </div>
      <p className="muted" style={{ fontSize: 12 }}>
        Édité localement. Exporte le JSON puis remplace <code>content/friends.json</code> et rebuild.
        (Persistance directe à venir avec le backend.)
      </p>
    </div>
  );
}
