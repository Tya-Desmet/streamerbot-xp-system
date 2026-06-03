'use client';

import { useEffect, useState } from 'react';
import type { Friend } from '@/lib/content';
import { fetchContent } from '@/lib/api';
import { checkLive, channelFromUrl } from '@/lib/live';
import FriendCard from './FriendCard';

// Liste des copains : seed build-time, rafraîchie depuis l'API si dispo.
// Puis statut live réel détecté via decapi, re-tri (live d'abord).
export default function FriendsLive({ friends: seed }: { friends: Friend[] }) {
  const [friends, setFriends] = useState<Friend[]>(seed);
  const [live, setLive] = useState<Record<string, boolean> | null>(null);

  // 1) rafraîchir la liste depuis l'API (sinon garder le seed)
  useEffect(() => {
    let cancelled = false;
    fetchContent<Friend[]>('friends', seed).then((list) => {
      // Garde le seed build-time si l'API ne renvoie rien (backend pas encore peuplé).
      if (!cancelled && Array.isArray(list) && list.length > 0) setFriends(list);
    });
    return () => {
      cancelled = true;
    };
  }, [seed]);

  // 2) détecter le statut live (decapi) pour la liste courante
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
