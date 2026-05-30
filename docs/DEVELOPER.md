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
| `USER_GetOrCreate` | Appelée par les autres actions | Charge ou crée un profil viewer depuis JSON |
| `XP_Add` | Twitch Chat Message | Pipeline complet : validation → XP → sauvegarde |
| `LEADERBOARD_Update` | Timer (5 min) | Construit le Top 10 et l'envoie à OBS |
| `CARD_ShowProfile` | Channel Point Redemption | Affiche la carte de profil d'un viewer |

---

## Services (dossier `scripts/`)

Les services contiennent la logique métier. Ils sont copiés dans chaque action C# (Streamer.bot ne supporte pas les imports de classes partagées entre scripts).

| Service | Responsabilité |
|---|---|
| `ConfigService` | Charge `config.json` avec valeurs par défaut pour chaque champ manquant |
| `UserRepository` | Lecture / écriture des profils JSON — aucune logique métier |
| `ValidationService` | Anti-spam : cooldown, longueur minimum, filtre de commandes |
| `XpService` | Calcul XP, niveaux, classement — **gateway unique pour modifier l'XP** |
| `OBSService` | Transport WebSocket vers les Browser Sources OBS |

> Toute modification d'un service doit être reportée manuellement dans toutes les actions qui l'embarquent.

---

## Pipeline XP — Message chat

```
Message Twitch reçu
        │
        ▼
XP_Add.Execute()
        │
        ▼
USER_GetOrCreate
  data/users/{user}.json existe ?
    OUI → LoadUser()
    NON → CreateUser() + SaveUser()
        │
        ▼
ValidationService.Validate()
  IsCommand()    → SKIP si commence par !, /, .
  IsTooShort()   → SKIP si < minMessageLength caractères
  IsOnCooldown() → SKIP si dans la fenêtre cooldown
        │ (message valide)
        ▼
XpService.AddXp(user, xpPerMessage)
  user.Xp += amount
  Recalcule user.Level via 100 × level^1.5
  Détecte level up
  Met à jour Messages, LastMessageTimestamp
        │
        ▼
UserRepository.SaveUser(user)
  Écrit data/users/{username}.json
        │
        ▼
CPH.SetArgument()
  xp_added · xp_total · xp_level
  xp_isLevelUp · xp_percentage
```

---

## Pipeline Leaderboard

```
Timer déclenche LEADERBOARD_Update
        │
        ▼
UserRepository.GetAllUsers()
  Lit tous data/users/*.json
  Retourne List<UserProfile>
        │
        ▼
XpService.PrepareLeaderboard(users)
  Trie par Xp décroissant
  Prend les 10 premiers
  Attribue Rank 1-10
        │
        ▼
OBSService.SendLeaderboard(top10)
  CPH.WebsocketBroadcastJson()
  Payload : { "players": [...] }
        │
        ▼ WebSocket
leaderboard.html
  podium.js  → rangs 1-3
  top10.js   → rangs 4-10
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
USER_GetOrCreate
  Charge UserProfile
  Introuvable → exit silencieux
        │
        ▼
UserRepository.GetAllUsers() → tri XP → rang réel
        │
        ▼
XpService.GetProgress(user)
  Retourne : xpIntoLevel, xpForNext, percentage
        │
        ▼
OBSService.SendProfileCard(payload)
  { username, displayName, level, xpCurrent, xpForNext, rank, percentage }
        │
        ▼ WebSocket
card.html
  renderer.js  → peuple DOM
  animations.js → animateIn()
  Après 8s     → animateOut()
```

---

## Formule de niveaux

```
XP requis pour passer au niveau N = 100 × N^1.5
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

Les overlays se reconnectent automatiquement toutes les 3 secondes en cas de déconnexion.

**Payload Leaderboard :**
```json
{
  "players": [
    { "rank": 1, "username": "mystya", "displayName": "Mystya", "level": 12, "xp": 1450 },
    { "rank": 2, "username": "viewer2", "displayName": "Viewer2", "level": 8,  "xp": 980  }
  ]
}
```

**Payload Profile Card :**
```json
{
  "username":    "mystya",
  "displayName": "Mystya",
  "level":       12,
  "xpCurrent":   1450,
  "xpForNext":   1800,
  "rank":        1,
  "percentage":  0.805
}
```

---

## Conventions de nommage

**Actions Streamer.bot**
```
DOMAINE_Verbe
XP_Add · USER_GetOrCreate · LEADERBOARD_Update · CARD_ShowProfile
```

**Services C#**
```
NomService.cs
ConfigService · UserRepository · XpService · ValidationService · OBSService
```

**Fichiers overlays**
```
overlays/{nom}/{nom}.html
overlays/{nom}/{nom}.css
overlays/{nom}/{nom}.js
overlays/{nom}/themes/{theme}/theme.css
```

**Données viewers**
```
data/users/{username_en_minuscules}.json
```

**Variables globales Streamer.bot**
```
Préfixe obligatoire : xp_
Exemples : xp_configPath · xp_total · xp_isLevelUp
```

---

## Règles d'architecture

**1. Gateway XP unique**
Toute modification d'XP **doit** passer par `XpService.AddXp()`. Ne jamais modifier `user.Xp` directement dans une action.

**2. Un fichier = une responsabilité**
`UserRepository` ne calcule pas. `XpService` ne fait pas d'I/O. `OBSService` ne connaît pas les règles métier.

**3. Pas de logique dans les overlays**
Les fichiers HTML/JS reçoivent des données prêtes à l'affichage. Ils ne calculent pas le rang, ne filtrent pas les utilisateurs, ne font pas d'arithmétique XP.

**4. Animations GPU uniquement**
Toutes les animations CSS utilisent exclusivement `transform` et `opacity`. Jamais `width`, `height`, `top`, `left` — ces propriétés provoquent des reflows qui cassent les performances.

**5. Taille des fichiers**
Maximum recommandé : 300 lignes par fichier. Au-delà, découper en sous-modules.

---

## Ajouter un nouveau module

**Exemple : XP watchtime (V2)**

1. Créer `actions/WATCHTIME_Add.cs`
2. Déclencheur : Timer toutes les 5 minutes
3. Première sous-action : `USER_GetOrCreate`
4. Appeler `XpService.AddXp(user, config.xpPerWatch, "watchtime")`
5. Incrémenter `user.WatchTime` via `UserRepository.SaveUser()`

**Exemple : commande `!rank` (V2)**

1. Créer `actions/RANK_Get.cs`
2. Déclencheur : Twitch → Chat Command → `!rank`
3. Charger le profil via `USER_GetOrCreate`
4. Calculer le rang : `GetAllUsers()` + tri par XP + `IndexOf(user) + 1`
5. Répondre via `CPH.SendMessage($"@{user.DisplayName} — Rang #{rank}, Niveau {user.Level}")`

**Exemple : système de badges (V4)**

1. Ajouter un champ `Badges` (List\<string\>) dans `UserProfile`
2. Créer `scripts/BadgeService.cs`
3. Ce service vérifie les conditions après chaque `AddXp()` (niveau atteint, messages envoyés, etc.)
4. Il ne modifie jamais l'XP — uniquement les métadonnées badges
5. Appeler `BadgeService.CheckBadges(user)` depuis `XP_Add` après la sauvegarde

---

## Structure des données viewer

**Fichier :** `data/users/{username}.json`

```json
{
  "Username":             "mystya",
  "DisplayName":          "Mystya",
  "Xp":                   1450,
  "Level":                12,
  "Messages":             592,
  "WatchTime":            0,
  "Rank":                 0,
  "LastMessageTimestamp": 1748000000
}
```

| Champ | Type | Description |
|---|---|---|
| `Username` | string | Nom en minuscules — utilisé comme nom de fichier |
| `DisplayName` | string | Nom affiché dans les overlays |
| `Xp` | int | XP total accumulé |
| `Level` | int | Niveau actuel — calculé depuis Xp |
| `Messages` | int | Nombre de messages valides |
| `WatchTime` | int | Minutes de watch (réservé V2) |
| `Rank` | int | Calculé à la volée — non persisté de façon fiable |
| `LastMessageTimestamp` | long | Unix timestamp — utilisé pour le cooldown |
