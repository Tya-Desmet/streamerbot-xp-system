# Architecture — Référence technique

Ce document décrit l'architecture telle qu'elle est **réellement implémentée**. Il permet de reprendre le développement sans contexte externe.

---

## Vue d'ensemble

```
Twitch
  ├─ Chat Message ──────────────────→ XP_Add
  │                                      └─ sub-action: USER_GetOrCreate
  ├─ Chat Command (!rank) ──────────→ RANK_ShowCommand
  ├─ Channel Point Redemption ──────→ CARD_ShowProfile
  └─ Present Viewers (5 min) ───────→ XP_WatchTime_V2

Streamer.bot Timer (5 min) ───────→ LEADERBOARD_Update

Toutes les actions ────────────────→ data/users/*.json  (lecture/écriture)
LEADERBOARD_Update, CARD_ShowProfile → WebSocket → OBS Browser Sources
RANK_ShowCommand ──────────────────→ CPH.SendMessage() → Twitch Chat
```

---

## Actions Streamer.bot

| Action | Trigger | Rôle |
|---|---|---|
| `USER_GetOrCreate` | Sub-action de XP_Add | Charger ou créer le profil viewer. Vérifie l'exclusion bots avant tout. |
| `XP_Add` | Twitch Chat Message | Valider message, attribuer XP, incrémenter Messages + timestamp. |
| `LEADERBOARD_Update` | Timer (5 min) | Lire tous les profils, trier, envoyer Top N via WebSocket. |
| `CARD_ShowProfile` | Channel Point Redemption | Charger profil, calculer rang live, envoyer card via WebSocket. |
| `RANK_ShowCommand` | Chat Command `!rank` | Répondre avec rang, niveau, XP, titre, messages, watchtime. |
| `XP_WatchTime_V2` | Present Viewers (5 min, Live Update) | Distribuer XP watchtime aux viewers éligibles. |

> `XP_WatchTime.cs` (Timer-based) est **désactivé** — remplacé par `XP_WatchTime_V2`.

---

## Services C# — Référence

Chaque action embarque ses propres copies (contrainte SB : pas de classes partagées entre actions). Les fichiers `scripts/` sont les références canoniques.

| Service | Utilisé par | Rôle |
|---|---|---|
| `ConfigService` | Toutes les actions | Charger config.json, appliquer les défauts par section. |
| `UserRepository` | Toutes les actions | CRUD profils JSON (LoadUser, SaveUser, GetAllUsers, CreateUser). |
| `XpService` | XP_Add, LEADERBOARD, CARD, WatchTime | AddXp, AddWatchTimeXp, GetProgress, CalculateLevel, PrepareLeaderboard. |
| `ValidationService` | XP_Add | Anti-spam : cooldown, longueur minimale, filtre commandes. |
| `WatchTimeService` | XP_WatchTime_V2 | Vérifier éligibilité watchtime (LastWatchTimestamp + tolérance). |
| `RankService` | RANK_ShowCommand | Calcul rang live, formatage message !rank, formatage watchtime. |
| `TitleService` | CARD_ShowProfile, RANK_ShowCommand | Résolution titre par niveau (4 niveaux de priorité). |
| `BotExclusionService` | Toutes les actions sauf XP_Add | Charger excluded-users.json + broadcaster optionnel. |

---

## UserProfile — Structure JSON

Chaque viewer → un fichier `data/users/{username}.json`.

```json
{
  "Username":             "viewerlogin",
  "DisplayName":          "ViewerDisplayName",
  "Xp":                   1250,
  "Level":                4,
  "Messages":             87,
  "WatchTime":            120,
  "Rank":                 0,
  "LastMessageTimestamp": 1748000000,
  "LastWatchTimestamp":   1747999700,
  "WatchStreak":          6
}
```

| Champ | Type | Description |
|---|---|---|
| `Username` | string | Login Twitch (minuscules). Clé primaire. |
| `DisplayName` | string | Pseudo affiché (avec majuscules). |
| `Xp` | int | XP total cumulé (chat + watchtime). |
| `Level` | int | Niveau calculé depuis Xp. |
| `Messages` | int | Messages validés (hors spam). |
| `WatchTime` | int | Watchtime total en minutes. |
| `Rank` | int | Legacy — ne pas lire. Le rang est calculé dynamiquement. |
| `LastMessageTimestamp` | long | Unix timestamp (sec) du dernier message XP. Utilisé pour le watchtime hybride. |
| `LastWatchTimestamp` | long | Unix timestamp du dernier cycle watchtime. Anti-double attribution. |
| `WatchStreak` | int | Cycles watchtime consécutifs. Reset si absence > 2 cycles. |

---

## Formule de niveau

```
XP requis : Niveau N → N+1  =  floor(100 × N^1.5)

Niv. 1 →  2 :   100 XP
Niv. 2 →  3 :   283 XP
Niv. 5 →  6 : 1 118 XP
Niv.10 → 11 : 3 162 XP
```

---

## Tri V2 (Leaderboard, CARD, RANK)

```
Level DESC → XP DESC → WatchTime DESC
```

Identique dans toutes les actions qui calculent un rang.

---

## Titres — Priorité de résolution

```
1. configs/titles.json                (override utilisateur)
2. themes/{theme}/titles.json         (thème actif, si non-default)
3. themes/default/titles.json         (fallback défaut)
4. "Viewer"                           (hardcode si aucun fichier valide)
```

Format : `[{ "minLevel": N, "title": "..." }, ...]`
Sélection : titre avec le `minLevel` le plus élevé ≤ niveau du viewer.

---

## Bot exclusion — Priorité de chargement

```
1. configs/excluded-users.json        (liste JSON configurable)
   Si absent ou vide :
2. Liste hardcodée (nightbot, streamelements, moobot, fossabot,
                    streamlabs, wizebot, mixitupbot, streamerbot)
+  config.Bots.BroadcasterName        (si ExcludeBroadcaster: true)
```

---

## Watchtime hybride — Logique cycle

```
Trigger: Present Viewers (SB, 5 min, Live Update)
  args["users"] = List<Dictionary<string,object>>  (chatters Twitch via EventSub)
  args["isLive"] = bool

Pour chaque profil JSON existant :
  1. Bot?                                          → skip
  2. Cooldown (LastWatchTimestamp + interval)?      → skip
  3. Dans la liste SB chatters?    → éligible ✓    (source 1 : présence)
  4. Chat récent (< 2× interval)?  → éligible ✓    (source 2 : activité)
  5. Ni 3 ni 4                                     → skip
  6. WatchStreak recalculé
  7. XP = xp.perWatchInterval + StreakBonus(streak)
  8. WatchTime += interval, LastWatchTimestamp = now
  9. SaveUser()
```

**Limitation** : Les vrais lurkers (sans chat ouvert) sont invisibles pour Twitch. La liste SB provient de Twitch EventSub (chatters ayant le chat ouvert). Pas de watchtime Twitch officiel — c'est la meilleure approximation disponible sans API externe.

---

## Flux XP par message

```
Viewer → message
  XP_Add (Chat Message)
    │ sub-action: USER_GetOrCreate
    │   BotExclusionService → exclu? → user_excluded = true, return
    │   LoadUser() OU CreateUser() → expose profil dans args
    │
    Execute()
      user_excluded = true?           → skip
      ValidationService.Validate()
        IsCommand?                    → skip (raison: "command")
        IsTooShort?                   → skip (raison: "too_short")
        IsOnCooldown?                 → skip (raison: "cooldown")
        → SetCooldown()
      XpService.AddXp(username, config.Xp.PerMessage)
        LoadUser → Xp += amount → Level = CalculateLevel(Xp) → SaveUser
      user.Messages++ + LastMessageTimestamp = now → SaveUser
      → expose xp_added, xp_total, xp_isLevelUp, xp_xpIntoLevel, ...
```

---

## Flux Leaderboard

```
Timer (5 min)
  LEADERBOARD_Update
    GetAllUsers() → List<UserProfile>
    BotExclusionService → filtre bots
    PrepareLeaderboard() → tri Level↓ XP↓ WatchTime↓
    GetRange(0, config.Leaderboard.TopCount)
    WebsocketBroadcastJson({ event: "updateLeaderboard", players: [...] })
      → OBS leaderboard.html → WebSocket → animation → affichage
```

---

## WebSocket — Payloads

### Leaderboard
```json
{
  "event": "updateLeaderboard",
  "players": [
    { "rank": 1, "username": "ViewerA", "level": 12, "xp": 8450, "avatar": "" }
  ]
}
```

### Profile Card
```json
{
  "event": "showCard",
  "card": {
    "username": "ViewerA", "avatar": "", "level": 12,
    "xpCurrent": 2450, "xpForNext": 3100,
    "rank": 1, "title": "Vétéran", "messages": 342, "watchTime": 480
  }
}
```

---

## Chargement config dans les actions

```csharp
var configPath  = CPH.GetGlobalVar<string>("xp_configPath", true);
var config      = new ConfigService().LoadConfig(configPath);
var configDir   = Path.GetDirectoryName(configPath ?? "");
var projectPath = Path.GetDirectoryName(configDir ?? "");
// projectPath → racine du projet → utilisé par TitleService et BotExclusionService
```

---

## Contraintes d'environnement SB 1.0.4

| Contrainte | Raison | Solution |
|---|---|---|
| Pas de `System.Linq` | Assembly non chargé | `foreach` + `List.Sort()` |
| Pas de `HashSet<T>` | Assembly non chargé | `Dictionary<string, bool>` |
| Pas de `$"..."` | Risque selon contexte | Concaténation `+` |
| Pas de classes partagées | Compilation isolée | Services copiés par action |
| Pas de `CPH.GetActiveViewers()` | Non implémenté en 1.0.4 | Present Viewers trigger |

---

## Extension future recommandée

| Feature | Fichier à créer | Service |
|---|---|---|
| Double XP Channel Point | `actions/REWARD_BonusXp.cs` | `RewardService.cs` |
| Boss system | `actions/BOSS_Spawn.cs` | `BossService.cs` |
| Achievements | `actions/ACHIEVEMENT_Check.cs` | `AchievementService.cs` |

---

## Hub web — architecture front (V3.x)

Site Next.js 16 en **export statique** (`output: 'export'`, App Router, TS strict).

```
website/app/
├── (hub)/          route group : site complet (layout racine avec NavBar/Footer/JSON-LD)
│   ├── layout.tsx  identité + SEO depuis content/site.json (P03)
│   ├── page.tsx    accueil (Hero, LivePodium live, sections optionnelles)
│   ├── leaderboard/ planning/ ressources/ privacy/ admin/ viewer/[username]/
│   └── not-found.tsx (non utilisé : Next route vers la 404 globale)
├── (embed)/        route group : layout racine MINIMAL (sans chrome)
│   └── embed/leaderboard/page.tsx  classement seul, polling, paramétrable
├── not-found.tsx   404 globale autonome (<html> propre) → out/404.html
├── manifest.ts · sitemap.ts · robots.ts · icon.svg · globals.css
```

- **Données** : build-time via `lib/data.ts` (lit `public/data/*.json`, copiés des
  `exports/` par `scripts/copy-exports.mjs`). En live, les composants client
  (`Leaderboard`, `LivePodium`, `EmbedLeaderboard`) pollent `leaderboardUrl()` →
  `/api/leaderboard` si `NEXT_PUBLIC_API_URL`, sinon le JSON statique.
- **Identité & sections** : `content/site.json` via `lib/content.ts`
  (`getSite`, `isFeatureOn`). Aucun branding en dur. Cf. `EXPORT_CONTRACT.md`,
  `CONFIGURATION.md`, `EMBED.md`, `TEMPLATE.md`.
- **Deux layouts racine** (route groups) : permettent à l'embed d'être servi sans le
  chrome du hub. URLs inchangées par les groups.
