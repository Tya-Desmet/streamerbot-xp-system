'use client';

import { useEffect, useState } from 'react';
import type { Friend } from '@/lib/content';
import { checkLive, channelFromUrl } from '@/lib/live';
import FriendCard from './FriendCard';

// Détecte le statut live réel des copains (decapi) côté client, puis re-trie (live d'abord).
export default function FriendsLive({ friends }: { friends: Friend[] }) {
  const [live, setLive] = useState<Record<string, boolean> | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const entries = await Promise.all(
        friends.map(async (f) => {
          const ch = f.channel || channelFromUrl(f.url);
          return [f.handle, await checkLive(ch)] as const;
        }),
      );
      if (!cancelled) setLive(Object.fromEntries(entries));
    })();
    return () => {
      cancelled = true;
    };
  }, [friends]);

  // Avant la réponse decapi : on garde le statut du fichier (évite un flash vide).
  const enriched = friends.map((f) => ({ ...f, live: live ? !!live[f.handle] : f.live }));
  const sorted = [...enriched].sort((a, b) => Number(b.live) - Number(a.live));

  return (
    <div className="friends">
      {sorted.map((f) => (
        <FriendCard key={f.handle} friend={f} />
      ))}
    </div>
  );
}
