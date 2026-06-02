# Guide développeur

Documentation destinée aux développeurs souhaitant comprendre, étendre ou contribuer au système.

---

## Architecture globale

```
Twitch Event
     │
     ▼
Action Streamer.bot   ← orchestration
     │
     ▼
Services C#           ← logique métier
     │
     ▼
JSON local            ← persistance
     │
     ▼
WebSocket → OBS       ← présentation
```

---

## Actions (dossier `actions/`)

Les actions sont des **orchestrateurs**. Elles reçoivent les événements Streamer.bot et appellent les services dans le bon ordre. Elles ne contiennent pas de logique métier directe.

| Action | Déclencheur | Rôle |
|---|---|---|
| `USER_GetOrCreate` | Appelée en première sub-action | Charge ou crée un profil viewer depuis JSON |
| `XP_Add` | Twitch Chat Message | Pipeline complet : validation → XP → sauvegarde |
| `XP_WatchTime_V2` | Present Viewers (5 min) | Distribue l'XP watchtime — système hybride présence + activité |
| `LEADERBOARD_Update` | Timer (5 min) | Construit le Top N, l'envoie à OBS, met à jour le cache rang |
| `CARD_ShowProfile` | Channel Point Redemption | Affiche la carte de profil d'un viewer |
| `RANK_ShowCommand` | Chat Command `!rank` | Répond avec le rang, le niveau et les stats du viewer |
| `REWARD_BonusXp` | Channel Point Redemption | Active le multiplicateur XP temporaire pour un viewer |
| `REWARD_GrantXp` | Channel Point Redemption | Octroie un montant fixe d'XP via channel point |
| `DAILY_CheckIn` | Channel Point Redemption | Check-in quotidien — coche une case sur la carte de fidélité (10 cases) |

---

## Services (dossier `scripts/`)

Les services contiennent la logique métier. Ils sont assemblés dans chaque action via `tools/build-actions.ps1` (Streamer.bot ne supporte pas les imports entre scripts).

| Service | Responsabilité |
|---|---|
| `ConfigService` | Charge `config.json` avec valeurs par défaut |
| `UserRepository` | CRUD profils JSON — aucune logique métier |
| `ValidationService` | Anti-spam : cooldown, longueur minimum, filtre commandes |
| `XpService` | Calcul XP, niveaux, leaderboard — **gateway unique** |
| `WatchTimeService` | Vérification éligibilité watchtime (LastWatchTimestamp + tolérance 10s) |
| `RankService` | Rang live, format message `!rank`, format watchtime |
| `TitleService` | Résolution titre par niveau (cascade 4 sources) |
| `BotExclusionService` | Charge `excluded-users.json`, vérifie les exclusions bots |
| `RewardService` | Cycle de vie du multiplicateur — activation, lecture, expiration paresseuse |

> Les fichiers dans `scripts/` sont les sources canoniques. Modifier un service, puis lancer `.\tools\build-actions.ps1` pour régénérer `actions/generated/`.

---

## Contraintes Streamer.bot 1.0.4

Ces contraintes s'appliquent à tout code dans `actions/` et `actions/generated/` :

| Contrainte | Raison | Solution |
|---|---|---|
| Pas de `System.Linq` | Assembly non chargé dans SB | `foreach` + `List.Sort()` |
| Pas de `HashSet<T>` | Assembly non chargé dans SB | `Dictionary<string, bool>` |
| Pas de `$"..."` | Risque selon contexte SB | Concaténation `+` |
| Pas de classes partagées | Compilation isolée par action | Services copiés via build script |

**Important :** Les fichiers dans `scripts/` sont des références canoniques. Ils sont SB-compatibles (pas de Linq, pas de HashSet, pas de `$"..."`). Les fichiers `actions/generated/` sont le résultat du build script.

---

## Structure des fichiers

```
actions/
├── _headers/     ← documentation + using statements (une action = un fichier)
├── _bodies/      ← uniquement CPHInline + Execute()
└── generated/    ← fichiers à copier dans SB (générés par build-actions.ps1)

scripts/          ← sources canoniques des services
tools/
└── build-actions.ps1  ← assemble actions/generated/ depuis scripts/ + _headers/ + _bodies/
```

**Workflow de modification d'un service :**
1. Modifier le service dans `scripts/`
2. Lancer `.\tools\build-actions.ps1`
3. Copier `actions/generated/*.cs` dans Streamer.bot
4. Recompiler dans SB

---

## Pipeline XP — Message chat

```
Message Twitch reçu
        │
        ▼
XP_Add.Execute()
        │
        ▼
USER_GetOrCreate (sub-action précédente)
  data/users/{user}.json existe ?
    OUI → LoadUser()
    NON → CreateUser() + SaveUser()
        │
        ▼
ValidationService.ValidateMessage()
  IsCommand()    → SKIP si commence par !, /, .
  IsTooShort()   → SKIP si < minMessageLength caractères
  IsOnCooldown() → SKIP si dans la fenêtre cooldown
        │ (message valide)
        ▼
UserRepository.LoadUser(username) → UserProfile
        │
        ▼
XpService.AddXp(user, xpPerMessage)
  user.Xp += amount
  Recalcule user.Level via 100 × level^1.5
  Détecte level up (oldLevel vs newLevel)
  user.Messages++
  user.LastMessageTimestamp = now
  UserRepository.SaveUser(user)   ← écriture atomique (.tmp → .json)
        │
        ▼
CPH.SetArgument()
  xp_added · xp_total · xp_newLevel
  xp_isLevelUp · xp_xpIntoLevel · xp_xpForNext · xp_percentage
```

---

## Pipeline Leaderboard

```
Timer déclenche LEADERBOARD_Update
        │
        ▼
UserRepository.GetAllUsers()
  Lit tous data/users/*.json  (ignore *.json.tmp)
  Retourne List<UserProfile>
        │
        ▼
BotExclusionService.IsExcluded() → filtre les bots avant le tri
        │
        ▼
XpService.PrepareLeaderboard(filtered)
  Tri V2 : Level DESC → XP DESC → WatchTime DESC
  Attribue Rank 1-N
        │
        ▼
CPH.WebsocketBroadcastJson()
  Payload : { "event": "updateLeaderboard", "players": [...] }
        │
        ├── CPH.SetGlobalVar("xp_leaderboard_cache", json, false)
        │    ← cache lu par RANK_ShowCommand et CARD_ShowProfile
        │
        ▼ WebSocket
leaderboard.html
  podium.js    → rangs 1-3
  top10.js     → rangs 4-10
  animation.js → révèle avec stagger
```

---

## Pipeline Carte de profil

```
Channel Point racheté
        │
        ▼
CARD_ShowProfile.Execute()
        │
        ▼
BotExclusionService → exclu ? → exit silencieux
        │
        ▼
UserRepository.LoadUser()
  Introuvable → exit silencieux
        │
        ▼
GlobalVar "xp_leaderboard_cache" → rang depuis cache (si < 1.5× intervalle)
  Cache absent/expiré → GetAllUsers() → filtre bots → tri V2 → rang live
        │
        ▼
XpService.GetProgress(user) → xpCurrent, xpForNext
        │
        ▼
TitleService.GetTitle(level, projectPath, theme)
  Cascade : configs/titles.json → themes/{theme}/titles.json → themes/default/ → "Viewer"
        │
        ▼
CPH.WebsocketBroadcastJson({ "event": "showCard", "card": {...} })
        │
        ▼ WebSocket
card.html → socket.js → window.showCard(data) → populate() → animateIn()
→ setTimeout 8000ms → hideCard() → animateOut()
```

---

## Formule de niveaux

```
XP requis pour passer du niveau N au niveau N+1 = 100 × N^1.5
```

| Niveau → | XP requis | XP total cumulé |
|---|---|---|
| 1 → 2 | 100 | 100 |
| 2 → 3 | 283 | 383 |
| 3 → 4 | 520 | 903 |
| 5 → 6 | 1 118 | ~3 400 |
| 10 → 11 | 3 162 | ~17 000 |

---

## Communication WebSocket

Les overlays se connectent au serveur WebSocket intégré de Streamer.bot :

```
ws://127.0.0.1:8080
```

Les overlays se reconnectent automatiquement toutes les **2 secondes** en cas de déconnexion.

**Payload Leaderboard :**
```json
{
  "event": "updateLeaderboard",
  "players": [
    { "rank": 1, "username": "Mystya",  "level": 12, "xp": 8450, "avatar": "" },
    { "rank": 2, "username": "Viewer2", "level": 8,  "xp": 4200, "avatar": "" }
  ]
}
```

**Payload Profile Card :**
```json
{
  "event": "showCard",
  "card": {
    "username":  "Mystya",
    "avatar":    "",
    "level":     12,
    "xpCurrent": 2450,
    "xpForNext": 3100,
    "rank":      1,
    "title":     "Adepte",
    "messages":  342,
    "watchTime": 480
  }
}
```

---

## Structure des données viewer

**Fichier :** `data/users/{username}.json`

```json
{
  "Username":             "viewerlogin",
  "DisplayName":          "ViewerDisplayName",
  "Xp":                   1250,
  "Level":                4,
  "Messages":             87,
  "WatchTime":            120,
  "LastMessageTimestamp": 1748000000,
  "LastWatchTimestamp":   1747999700,
  "WatchStreak":          6
}
```

| Champ | Type | Description |
|---|---|---|
| `Username` | string | Login en minuscules — utilisé comme nom de fichier |
| `DisplayName` | string | Nom affiché dans les overlays |
| `Xp` | int | XP total accumulé |
| `Level` | int | Niveau actuel — recalculé depuis Xp |
| `Messages` | int | Nombre de messages valides |
| `WatchTime` | int | Minutes de watchtime cumulées |
| `LastMessageTimestamp` | long | Unix timestamp — utilisé pour le cooldown |
| `LastWatchTimestamp` | long | Unix timestamp — dernier cycle watchtime reçu |
| `WatchStreak` | int | Cycles watchtime consécutifs — base pour les bonus de fidélité |

> Le champ `Rank` a été supprimé en V2. Le rang est calculé à la volée via le cache leaderboard ou `GetAllUsers()`.

---

## Conventions de nommage

**Actions Streamer.bot**
```
DOMAINE_Verbe
XP_Add · XP_WatchTime_V2 · USER_GetOrCreate
LEADERBOARD_Update · CARD_ShowProfile · RANK_ShowCommand
```

**Services C#**
```
NomService.cs
ConfigService · UserRepository · XpService · ValidationService
WatchTimeService · RankService · TitleService · BotExclusionService
```

**Fichiers overlays**
```
overlays/{nom}/{nom}.html
overlays/{nom}/{nom}.css
overlays/{nom}/js/*.js
overlays/{nom}/themes/{theme}/theme.css
```

**Données viewers**
```
data/users/{username_en_minuscules}.json
```

**Variables globales Streamer.bot**
```
Préfixe obligatoire : xp_
Exemples : xp_configPath · xp_total · xp_isLevelUp · xp_leaderboard_cache
```

---

## Règles d'architecture

**1. Gateway XP unique**
Toute modification d'XP **doit** passer par `XpService.AddXp()`. Ne jamais modifier `user.Xp` directement dans une action.

**2. Un fichier = une responsabilité**
`UserRepository` ne calcule pas. `XpService` ne fait pas d'I/O directement. Les overlays ne calculent pas.

**3. Pas de logique dans les overlays**
Les fichiers HTML/JS reçoivent des données prêtes à l'affichage. Ils ne calculent pas le rang, ne filtrent pas les utilisateurs, ne font pas d'arithmétique XP.

**4. Animations GPU uniquement**
Toutes les animations CSS utilisent exclusivement `transform` et `opacity`. Jamais `width`, `height`, `top`, `left` — ces propriétés provoquent des reflows.

**5. Écriture atomique obligatoire**
Toute écriture de profil JSON passe par `SaveUser()` qui écrit dans `.tmp` puis renomme. Jamais de `File.WriteAllText(path, json)` direct.

---

## Ajouter un nouveau module

**Exemple : système de badges**

1. Ajouter un champ `Badges` (`List<string>`) dans `UserProfile` dans `scripts/UserRepository.cs`
2. Créer `scripts/BadgeService.cs`
3. Ce service vérifie les conditions après chaque `AddXp()` (niveau atteint, messages, etc.)
4. Il ne modifie jamais l'XP — uniquement les métadonnées badges
5. Ajouter `BadgeService` dans les dépendances de `XP_Add` dans `tools/build-actions.ps1`
6. Appeler `BadgeService.CheckBadges(user)` dans `actions/_bodies/XP_Add.cs` après `AddXp()`
7. Relancer `.\tools\build-actions.ps1` pour régénérer `actions/generated/XP_Add.cs`
