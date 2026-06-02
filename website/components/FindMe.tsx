'use client';

import { useState } from 'react';

// Saisie du pseudo Twitch du visiteur (pour se surligner dans le classement).
// Aucun compte, aucun serveur : la valeur est mémorisée en localStorage par le parent.
export default function FindMe({ value, onSave }: { value: string; onSave: (name: string) => void }) {
  const [text, setText] = useState(value);

  const inputStyle: React.CSSProperties = {
    height: 40,
    padding: '0 14px',
    borderRadius: 999,
    background: 'var(--surface)',
    border: '1px solid var(--line-strong)',
    color: 'var(--text)',
    minWidth: 180,
  };

  return (
    <form
      className="flex center gap-s"
      style={{ flexWrap: 'wrap' }}
      onSubmit={(e) => {
        e.preventDefault();
        onSave(text);
      }}
    >
      <input
        style={inputStyle}
        placeholder="Ton pseudo Twitch…"
        value={text}
        onChange={(e) => setText(e.target.value)}
        aria-label="Ton pseudo Twitch"
      />
      <button type="submit" className="btn btn-ghost btn-sm">
        Trouve-toi
      </button>
      {value ? (
        <button
          type="button"
          className="btn btn-ghost btn-sm"
          title="Oublier"
          onClick={() => {
            setText('');
            onSave('');
          }}
        >
          ✕
        </button>
      ) : null}
    </form>
  );
}
