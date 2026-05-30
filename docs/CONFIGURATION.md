# Configuration — Référence complète

## Le fichier config.json

**Chemin :** `configs/config.json`

Ce fichier centralise tous les paramètres du système. Il est chargé au démarrage de chaque action via `ConfigService.cs`, avec des valeurs par défaut pour les paramètres optionnels. La seule valeur sans défaut est `dataPath` — elle est obligatoire.

---

## Paramètres

### dataPath

```json
"dataPath": "C:\\StreamerTools\\streamerbot-xp-system\\data\\users"
```

Chemin absolu vers le dossier contenant les profils viewers (fichiers JSON).

| Propriété | Valeur |
|---|---|
| Type | string |
| Défaut | **aucun — paramètre obligatoire** |
| Format | Chemin absolu Windows |

Le dossier doit exister physiquement avant le premier lancement. Les fichiers `{username}.json` y sont créés automatiquement.

```json
// Formats acceptés (les deux fonctionnent)
"dataPath": "C:\\StreamerTools\\streamerbot-xp-system\\data\\users"
"dataPath": "C:/StreamerTools/streamerbot-xp-system/data/users"
```

---

### xpPerMessage

```json
"xpPerMessage": 10
```

Nombre d'XP gagnés par message valide en chat.

| Propriété | Valeur |
|---|---|
| Type | integer |
| Défaut | `10` |
| Recommandé | 5–15 selon la taille du stream |

---

### xpPerWatch

```json
"xpPerWatch": 1
```

XP gagnés par minute de watch. Réservé pour la V2 — non utilisé en V1.

| Propriété | Valeur |
|---|---|
| Type | integer |
| Défaut | `1` |
| Statut | Réservé V2 |

---

### cooldownSeconds

```json
"cooldownSeconds": 30
```

Délai minimum en secondes entre deux messages XP pour le même viewer. Un message envoyé pendant ce délai est ignoré silencieusement (aucun log, aucune erreur).

| Propriété | Valeur |
|---|---|
| Type | integer |
| Défaut | `30` |
| Recommandé | 20–60 selon l'activité du chat |

---

### minMessageLength

```json
"minMessageLength": 2
```

Longueur minimum d'un message en caractères pour valider l'attribution d'XP. Les messages trop courts (smash de touches, emotes seules) sont ignorés silencieusement.

| Propriété | Valeur |
|---|---|
| Type | integer |
| Défaut | `2` |
| Recommandé | 2–5 caractères |

---

### leaderboardIntervalMinutes

```json
"leaderboardIntervalMinutes": 5
```

Fréquence de mise à jour automatique du leaderboard dans OBS, en minutes. Ce paramètre doit correspondre à l'intervalle du Timer configuré dans Streamer.bot pour l'action `LEADERBOARD_Update`.

| Propriété | Valeur |
|---|---|
| Type | integer |
| Défaut | `5` |
| Recommandé | 5–10 minutes |

---

### obsLeaderboardSource

```json
"obsLeaderboardSource": "Leaderboard"
```

Nom exact de la Browser Source OBS affichant le leaderboard. Doit correspondre **exactement** au nom configuré dans OBS — sensible à la casse et aux espaces.

| Propriété | Valeur |
|---|---|
| Type | string |
| Défaut | `"Leaderboard"` |

---

### obsCardSource

```json
"obsCardSource": "ProfileCard"
```

Nom exact de la Browser Source OBS affichant la carte de profil.

| Propriété | Valeur |
|---|---|
| Type | string |
| Défaut | `"ProfileCard"` |

---

## Exemples complets

### Configuration standard

Bon point de départ pour la plupart des streams.

```json
{
  "dataPath":                   "C:\\StreamerTools\\streamerbot-xp-system\\data\\users",
  "xpPerMessage":               10,
  "xpPerWatch":                 1,
  "cooldownSeconds":            30,
  "minMessageLength":           2,
  "leaderboardIntervalMinutes": 5,
  "obsLeaderboardSource":       "Leaderboard",
  "obsCardSource":              "ProfileCard"
}
```

---

### Configuration grand stream (chat actif)

Cooldown et longueur minimum augmentés pour réduire l'impact du spam et des spammeurs sur les gros chats.

```json
{
  "dataPath":                   "C:\\StreamerTools\\streamerbot-xp-system\\data\\users",
  "xpPerMessage":               5,
  "xpPerWatch":                 1,
  "cooldownSeconds":            60,
  "minMessageLength":           5,
  "leaderboardIntervalMinutes": 10,
  "obsLeaderboardSource":       "Leaderboard",
  "obsCardSource":              "ProfileCard"
}
```

---

### Configuration petit stream (chat intimiste)

XP plus généreux et cooldown réduit pour encourager la participation.

```json
{
  "dataPath":                   "C:\\StreamerTools\\streamerbot-xp-system\\data\\users",
  "xpPerMessage":               15,
  "xpPerWatch":                 1,
  "cooldownSeconds":            15,
  "minMessageLength":           2,
  "leaderboardIntervalMinutes": 5,
  "obsLeaderboardSource":       "Leaderboard",
  "obsCardSource":              "ProfileCard"
}
```

---

## Bonnes pratiques

**Chemins Windows**

Utilise toujours des doubles antislashs `\\` dans les valeurs de chemin JSON, ou des slashs `/`. Un antislash simple `\` est un caractère d'échappement JSON — il provoque une erreur de parsing.

```json
"dataPath": "C:\\StreamerTools\\...\\data\\users"   ← correct
"dataPath": "C:/StreamerTools/.../data/users"       ← correct
"dataPath": "C:\StreamerTools\...\data\users"       ← ERREUR
```

**Noms de sources OBS**

Les valeurs `obsLeaderboardSource` et `obsCardSource` sont comparées au nom exact de la Browser Source dans OBS. Un espace de trop ou une majuscule manquante suffit à casser la communication.

**dataPath pointe vers `data\users\`**

Le chemin doit pointer vers le sous-dossier `users\`, pas vers `data\` :
```json
"dataPath": "C:\\...\\data\\users"   ← correct
"dataPath": "C:\\...\\data"          ← ERREUR — un niveau trop haut
```

**Modifications à chaud**

Les modifications de `config.json` sont prises en compte au prochain déclenchement d'une action (pas besoin de redémarrer Streamer.bot).

**Référence de niveaux**

| Niveau → | XP requis | XP total cumulé |
|---|---|---|
| 1 → 2 | 100 | 100 |
| 2 → 3 | 283 | 383 |
| 3 → 4 | 520 | 903 |
| 5 → 6 | 1 118 | ~3 400 |
| 10 → 11 | 3 162 | ~17 000 |

Formule : `XP requis pour niveau N = 100 × N^1.5`
