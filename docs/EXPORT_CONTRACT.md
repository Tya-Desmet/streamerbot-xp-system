# Contrat d'export V3 — Bot ⇄ Hub web

> **schemaVersion : 1**
> Source de vérité : le bot Streamer.bot. Le hub web est **lecture seule**.
> Action génératrice : `EXPORT_Snapshot` (déclencheur Timer, lecture seule).

---

## Mode de transport

**Mode fichier (toujours actif)** : JSON dans `exports/`, générés par `EXPORT_Snapshot`
quand `config.export.enabled = true`. C'est ce que le **build du site** consomme.

**Mode HTTP / push (opt-in, implémenté)** : quand `config.export.pushEnabled = true`,
la tâche `tools/push-to-backend.ps1` assemble ces mêmes fichiers et les POST sur
`/api/push`. Le backend les ressert **à l'identique** :

| endpoint backend | source | consommé par |
|---|---|---|
| `GET /api/leaderboard` | dernier `leaderboard` poussé | classement du site (polling 45 s) |
| `GET /api/users/:id` | dernier `users[:id]` poussé | profil viewer du site |

Le site ne distingue pas les deux modes — **schéma identique**. Sans backend
(`NEXT_PUBLIC_API_URL` vide), il lit les fichiers figés au build.

```
Streamer.bot ─(lecture seule)─▶ EXPORT_Snapshot ─▶ exports/*.json ─┬─▶ build du site (figé)
                                                                    └─▶ push-to-backend.ps1
                                                                          └─▶ POST /api/push ─▶ backend ─▶ site (live)
```

---

## Fichiers générés

```
exports/
├── meta.json                 métadonnées (schéma, date, saison, branding)
├── leaderboard.json          classement Top N
└── users/
    └── {username}.json        profil public (un par viewer non-bot)
```

> `exports/` est gitignoré (données dérivées, régénérables).
> Le chemin est `config.export.path`, ou `<racineProjet>/exports` si vide.

---

### exports/meta.json

| champ | type | description |
|---|---|---|
| `schemaVersion` | int | version du contrat (actuellement `1`) |
| `generatedAt` | int (unix s) | date de génération du snapshot |
| `season` | string | libellé de saison courant (`config.export.season`) |
| `streamer.name` | string | nom d'affichage du streamer (`config.export.streamerName`) |

```json
{
  "schemaVersion": 1,
  "generatedAt": 1780369858,
  "season": "all-time",
  "streamer": { "name": "Mystya" }
}
```

---

### exports/leaderboard.json

Tri identique au reste du système : **Level DESC → XP DESC → WatchTime DESC**.
`players` contient au plus `config.export.topCount` entrées, bots exclus.

| champ | type | description |
|---|---|---|
| `generatedAt` | int | date de génération |
| `season` | string | saison |
| `players[]` | array | classement |
| `players[].rank` | int | rang 1-based |
| `players[].username` | string | login Twitch minuscule (clé) |
| `players[].displayName` | string | nom affiché |
| `players[].level` | int | niveau |
| `players[].xp` | int | XP total |
| `players[].watchTime` | int | minutes cumulées |
| `players[].title` | string | titre du niveau |

```json
{
  "generatedAt": 1780369858,
  "season": "all-time",
  "players": [
    { "rank": 1, "username": "payettes_", "displayName": "payettes_",
      "level": 3, "xp": 540, "watchTime": 0, "title": "Nouveau venu" }
  ]
}
```

---

### exports/users/{username}.json

Un fichier par viewer non-bot. **Champs d'affichage uniquement.**

| champ | type | description |
|---|---|---|
| `username` | string | login minuscule |
| `displayName` | string | nom affiché |
| `level` | int | niveau |
| `xp` | int | XP total |
| `xpIntoLevel` | int | XP acquis dans le niveau courant |
| `xpForNext` | int | XP requis pour le niveau suivant |
| `percentage` | int | progression 0-100 |
| `rank` | int | rang dans le classement **complet** (pas seulement le Top N) |
| `title` | string | titre du niveau |
| `messages` | int | messages validés |
| `watchTime` | int | minutes cumulées |
| `watchStreak` | int | cycles watchtime consécutifs (conservé, non affiché) |
| `totalCheckIns` | int | total cumulé de check-in (lifetime) |
| `sources.chat` | int | XP issu du chat |
| `sources.watch` | int | XP issu du watchtime |
| `sources.rewards` | int | XP issu des rewards (channel points) |

```json
{
  "username": "mystya",
  "displayName": "mystya",
  "level": 2,
  "xp": 180,
  "xpIntoLevel": 80,
  "xpForNext": 282,
  "percentage": 28,
  "rank": 10,
  "title": "Nouveau venu",
  "messages": 4,
  "watchTime": 0,
  "watchStreak": 0,
  "totalCheckIns": 0,
  "sources": { "chat": 0, "watch": 0, "rewards": 140 }
}
```

> Note : `sources.chat + sources.watch + sources.rewards` peut être < `xp` pour les
> profils créés avant le suivi par source (V2.6). L'export reflète fidèlement le
> profil stocké — il ne recalcule rien.

---

## Champs JAMAIS exportés (privés)

Ces champs internes ne quittent jamais le bot :

```
LastMessageTimestamp · LastWatchTimestamp · BonusExpiryTimestamp
ActiveBonusMultiplier (valeur brute) · CheckInCount · LastCheckInDay
chemins disque
```

---

## Règles de compatibilité

- **Ajout** d'un champ → rétrocompatible (le site ignore l'inconnu). Pas d'incrément
  de `schemaVersion`.
- **Suppression / renommage** d'un champ → incrément de `schemaVersion` + note de
  migration ci-dessous.
- Le consommateur **doit tolérer** un champ absent (valeur par défaut à l'affichage).

---

## Historique des versions

| schemaVersion | date | changements |
|---|---|---|
| 1 | 2026-06 | Version initiale : `meta` + `leaderboard` + `users`. Mode fichier. |
| 1 | 2026-06 | V3.9 — ajout **additif** `users.totalCheckIns` (total cumulé check-in). `schemaVersion` inchangé ; `watchStreak` conservé mais plus affiché côté site. |
