'use client';

import { useEffect, useState } from 'react';
import type { Download } from '@/lib/content';
import { downloadJson } from '@/lib/exportJson';
import { adminUpload } from '@/lib/adminApi';
import PublishButton from './PublishButton';
import { useAdmin } from './AdminAuth';

const KEY = 'sl-admin-downloads';
const ICONS = ['layers', 'image', 'code', 'package', 'music', 'download'];

const blank = (): Download => ({
  id: 'dl' + Date.now().toString(36),
  title: '',
  cat: '',
  desc: '',
  icon: 'package',
  ext: 'ZIP',
  size: '',
  version: '1.0',
  date: '',
  dls: 0,
  featured: false,
  downloadable: false,
  tags: [],
  url: '#',
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

export default function DownloadsEditor({ seed }: { seed: Download[] }) {
  const [items, setItems] = useState<Download[]>(seed);
  const [draft, setDraft] = useState<Download>(blank());

  useEffect(() => {
    try {
      const s = localStorage.getItem(KEY);
      if (s) setItems(JSON.parse(s));
    } catch {
      /* ignore */
    }
  }, []);

  function persist(next: Download[]) {
    setItems(next);
    try {
      localStorage.setItem(KEY, JSON.stringify(next));
    } catch {
      /* ignore */
    }
  }

  function save() {
    if (!draft.title.trim()) return;
    const exists = items.some((x) => x.id === draft.id);
    persist(exists ? items.map((x) => (x.id === draft.id ? draft : x)) : [...items, draft]);
    setDraft(blank());
  }

  const set = <K extends keyof Download>(k: K, v: Download[K]) =>
    setDraft((d) => ({ ...d, [k]: v }));

  const { token } = useAdmin();
  const [upMsg, setUpMsg] = useState('');

  async function handleUpload(file?: File) {
    if (!file || !token) return;
    setUpMsg('Envoi…');
    const r = await adminUpload(file, token);
    if (r) {
      const mb = r.size / (1024 * 1024);
      const sizeStr = mb >= 1 ? `${mb.toFixed(1)} Mo` : `${Math.max(1, Math.round(r.size / 1024))} Ko`;
      const ext = (file.name.split('.').pop() || '').toUpperCase();
      setDraft((d) => ({ ...d, url: r.url, downloadable: true, ext: ext || d.ext, size: sizeStr }));
      setUpMsg('Fichier déposé ✓');
    } else {
      setUpMsg("Échec de l'upload");
    }
    setTimeout(() => setUpMsg(''), 4000);
  }

  return (
    <div className="flex" style={{ flexDirection: 'column', gap: 16 }}>
      {/* Formulaire */}
      <div className="card" style={{ padding: '18px 20px', display: 'grid', gap: 10, gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))' }}>
        <input style={inp} placeholder="Titre" value={draft.title} onChange={(e) => set('title', e.target.value)} />
        <input style={inp} placeholder="Catégorie" value={draft.cat} onChange={(e) => set('cat', e.target.value)} />
        <select style={inp} value={draft.icon} onChange={(e) => set('icon', e.target.value)}>
          {ICONS.map((i) => (
            <option key={i} value={i}>{i}</option>
          ))}
        </select>
        <input style={inp} placeholder="Ext (ZIP)" value={draft.ext} onChange={(e) => set('ext', e.target.value)} />
        <input style={inp} placeholder="Taille (48 Mo)" value={draft.size ?? ''} onChange={(e) => set('size', e.target.value)} />
        <input style={inp} placeholder="Version" value={draft.version ?? ''} onChange={(e) => set('version', e.target.value)} />
        <input style={inp} placeholder="Date (Mai 2026)" value={draft.date ?? ''} onChange={(e) => set('date', e.target.value)} />
        <input style={inp} placeholder="URL de téléchargement" value={draft.url ?? ''} onChange={(e) => set('url', e.target.value)} />
        <input style={{ ...inp, gridColumn: '1 / -1' }} placeholder="Tags (séparés par des virgules)" value={draft.tags.join(', ')} onChange={(e) => set('tags', e.target.value.split(',').map((t) => t.trim()).filter(Boolean))} />
        <textarea style={{ ...inp, height: 64, padding: '8px 12px', gridColumn: '1 / -1' }} placeholder="Description" value={draft.desc} onChange={(e) => set('desc', e.target.value)} />
        <label className="flex center gap-s" style={{ fontSize: 14 }}>
          <input type="checkbox" checked={!!draft.featured} onChange={(e) => set('featured', e.target.checked)} /> En avant
        </label>
        <label className="flex center gap-s" style={{ fontSize: 14 }}>
          <input type="checkbox" checked={!!draft.downloadable} onChange={(e) => set('downloadable', e.target.checked)} /> Téléchargeable
        </label>
        {token ? (
          <div className="flex center gap-s" style={{ gridColumn: '1 / -1', flexWrap: 'wrap' }}>
            <label className="btn btn-ghost btn-sm" style={{ cursor: 'pointer' }}>
              Déposer un fichier…
              <input type="file" style={{ display: 'none' }} onChange={(e) => handleUpload(e.target.files?.[0])} />
            </label>
            {upMsg ? <span className="muted" style={{ fontSize: 12 }}>{upMsg}</span> : null}
            {draft.url && draft.url !== '#' ? <span className="muted truncate" style={{ fontSize: 12, maxWidth: 240 }}>{draft.url}</span> : null}
          </div>
        ) : (
          <p className="muted" style={{ fontSize: 12, gridColumn: '1 / -1' }}>
            Connecte-toi au backend pour déposer un fichier, ou colle une URL externe dans « URL de téléchargement ».
          </p>
        )}
        <div className="flex gap-s" style={{ gridColumn: '1 / -1' }}>
          <button className="btn btn-primary btn-sm" type="button" onClick={save}>
            {items.some((x) => x.id === draft.id) ? 'Enregistrer' : 'Ajouter'}
          </button>
          <button className="btn btn-ghost btn-sm" type="button" onClick={() => setDraft(blank())}>Nouveau</button>
        </div>
      </div>

      {/* Liste */}
      <ul className="flex" style={{ flexDirection: 'column', gap: 8 }}>
        {items.map((d) => (
          <li key={d.id} className="card flex center" style={{ gap: 10, padding: '10px 14px' }}>
            <div style={{ flex: 1, minWidth: 0 }}>
              <b className="truncate" style={{ display: 'block' }}>{d.title || '(sans titre)'} {d.featured ? '★' : ''}</b>
              <span className="muted" style={{ fontSize: 12 }}>{d.cat} · {d.ext}</span>
            </div>
            <button className="btn btn-ghost btn-sm" type="button" onClick={() => setDraft({ ...d })}>Éditer</button>
            <button className="btn btn-ghost btn-sm" type="button" onClick={() => persist(items.map((x) => ({ ...x, featured: x.id === d.id })))}>★</button>
            <button className="btn btn-ghost btn-sm" type="button" onClick={() => persist(items.filter((x) => x.id !== d.id))}>✕</button>
          </li>
        ))}
      </ul>

      <div className="flex center gap-s" style={{ flexWrap: 'wrap' }}>
        <PublishButton kind="resources" data={items} />
        <button className="btn btn-ghost btn-sm" type="button" onClick={() => downloadJson('downloads.json', items)}>
          Exporter downloads.json
        </button>
        <button className="btn btn-ghost btn-sm" type="button" onClick={() => persist(seed)}>Réinitialiser</button>
      </div>
      <p className="muted" style={{ fontSize: 12 }}>
        Édité localement (localStorage). Exporte le JSON puis remplace <code>content/downloads.json</code> et rebuild.
      </p>
    </div>
  );
}
