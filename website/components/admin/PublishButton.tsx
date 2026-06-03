'use client';

import { useState } from 'react';
import { useAdmin } from './AdminAuth';

// Bouton « Enregistrer (publier) » — visible uniquement connecté à l'API.
// Persiste le contenu via le backend (POST /api/admin/content/:kind).
export default function PublishButton({ kind, data }: { kind: string; data: unknown }) {
  const { token, save } = useAdmin();
  const [msg, setMsg] = useState('');

  if (!token) return null;

  return (
    <span className="flex center gap-s">
      <button
        className="btn btn-primary btn-sm"
        type="button"
        onClick={async () => {
          setMsg('…');
          const ok = await save(kind, data);
          setMsg(ok ? 'Publié ✓' : 'Échec');
          setTimeout(() => setMsg(''), 3000);
        }}
      >
        Enregistrer (publier)
      </button>
      {msg ? <span className="muted" style={{ fontSize: 12 }}>{msg}</span> : null}
    </span>
  );
}
