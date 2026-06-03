'use client';

import { createContext, useContext, useEffect, useState } from 'react';
import { adminApiBase, adminLogin, adminSave } from '@/lib/adminApi';

type Ctx = { token: string | null; save: (kind: string, data: unknown) => Promise<boolean> };
const AdminCtx = createContext<Ctx>({ token: null, save: async () => false });
export const useAdmin = () => useContext(AdminCtx);

const TOKEN_KEY = 'sl-admin-token';
const inp: React.CSSProperties = {
  height: 44,
  padding: '0 14px',
  borderRadius: 10,
  background: 'var(--surface)',
  border: '1px solid var(--line-strong)',
  color: 'var(--text)',
  width: '100%',
};

export default function AdminAuth({ children }: { children: React.ReactNode }) {
  const hasApi = !!adminApiBase();
  const [token, setToken] = useState<string | null>(null);
  const [pw, setPw] = useState('');
  const [err, setErr] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    try {
      const t = sessionStorage.getItem(TOKEN_KEY);
      if (t) setToken(t);
    } catch {
      /* ignore */
    }
  }, []);

  async function login(e: React.FormEvent) {
    e.preventDefault();
    setErr('');
    setBusy(true);
    const t = await adminLogin(pw);
    setBusy(false);
    if (t) {
      setToken(t);
      try {
        sessionStorage.setItem(TOKEN_KEY, t);
      } catch {
        /* ignore */
      }
      setPw('');
    } else {
      setErr('Mot de passe invalide ou backend injoignable.');
    }
  }

  function logout() {
    setToken(null);
    try {
      sessionStorage.removeItem(TOKEN_KEY);
    } catch {
      /* ignore */
    }
  }

  async function save(kind: string, data: unknown): Promise<boolean> {
    if (!token) return false;
    const ok = await adminSave(kind, data, token);
    if (!ok) logout(); // probable 401 → reconnexion
    return ok;
  }

  // Pas de backend configuré : éditeurs en mode local (export JSON), pas de publication.
  if (!hasApi) {
    return (
      <AdminCtx.Provider value={{ token: null, save: async () => false }}>
        <div className="card" style={{ padding: '14px 18px', marginBottom: 16, borderColor: 'var(--line-strong)' }}>
          <b>Mode local</b>
          <p className="muted" style={{ fontSize: 13 }}>
            Backend non configuré (NEXT_PUBLIC_API_URL). Édite et <b>exporte les JSON</b> ; la
            publication directe sera dispo une fois le backend en ligne.
          </p>
        </div>
        {children}
      </AdminCtx.Provider>
    );
  }

  // Backend présent mais pas connecté → login.
  if (!token) {
    return (
      <form className="card" onSubmit={login} style={{ padding: 24, maxWidth: 420, display: 'flex', flexDirection: 'column', gap: 12 }}>
        <h3>Connexion admin</h3>
        <input type="password" style={inp} value={pw} onChange={(e) => setPw(e.target.value)} placeholder="Mot de passe" autoFocus />
        <button className="btn btn-primary btn-sm" type="submit" disabled={busy}>
          {busy ? '…' : 'Se connecter'}
        </button>
        {err ? <p style={{ color: 'oklch(0.7 0.2 25)', fontSize: 13 }}>{err}</p> : null}
      </form>
    );
  }

  // Connecté → fournit save() aux éditeurs.
  return (
    <AdminCtx.Provider value={{ token, save }}>
      <div className="flex" style={{ justifyContent: 'flex-end', marginBottom: 12 }}>
        <button className="btn btn-ghost btn-sm" type="button" onClick={logout}>
          Déconnexion
        </button>
      </div>
      {children}
    </AdminCtx.Provider>
  );
}
