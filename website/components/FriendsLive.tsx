'use client';

import { useEffect, useState } from 'react';
import type { Friend } from '@/lib/content';
import { fetchContent } from '@/lib/api';
import { checkLive, checkViewers, channelFromUrl } from '@/lib/live';
import FriendCard from './FriendCard';

type LiveInfo = { live: boolean; viewers: number };

// Liste des copains : seed build-time, rafraîchie depuis l'API si dispo.
// Puis statut live réel + viewer count détectés via decapi, re-tri (live d'abord).
export default function FriendsLive({ friends: seed }: { friends: Friend[] }) {
  const [friends, setFriends] = useState<Friend[]>(seed);
  const [liveData, setLiveData] = useState<Record<string, LiveInfo> | null>(null);

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

  // 2) détecter le statut live + viewer count (decapi) pour la liste courante
  useEffect(() => {
    let cancelled = false;
    (async () => {
      const entries = await Promise.all(
        friends.map(async (f) => {
          const ch = f.channel || channelFromUrl(f.url);
          const isLive = await checkLive(ch);
          const viewers = isLive ? await checkViewers(ch) : 0;
          return [f.handle, { live: isLive, viewers }] as const;
        }),
      );
      if (!cancelled) setLiveData(Object.fromEntries(entries));
    })();
    return () => {
      cancelled = true;
    };
  }, [friends]);

  const enriched = friends.map((f) => ({
    ...f,
    live: liveData ? liveData[f.handle]?.live ?? f.live : f.live,
    viewers: liveData ? (liveData[f.handle]?.viewers ?? f.viewers) : f.viewers,
  }));
  const sorted = [...enriched].sort((a, b) => Number(b.live) - Number(a.live));

  return (
    <div className="friends">
      {sorted.map((f) => (
        <FriendCard key={f.handle} friend={f} />
      ))}
    </div>
  );
}
