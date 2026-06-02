# Configuration — Référence complète

Le fichier `configs/config.json` est **l'unique source de configuration** du système. Il est pointé par la variable globale `xp_configPath` dans Streamer.bot. Aucune autre variable globale n'est nécessaire.

---

## Structure complète

```json
{
  "dataPath": "C:\\Stream\\streamerbot-xp-system\\data\\users",
  "theme":    "default",

  "xp": {
    "perMessage":       10,
    "perWatchInterval": 5,
    "cooldownSeconds":  30,
    "minMessageLength": 2
  },

  "watchtime": {
    "enabled":         true,
    "intervalMinutes": 5,
    "streakEnabled":   true,
    "countOffline":    false
  },

  "leaderboard": {
    "intervalMinutes": 5,
    "topCount":        10
  },

  "obs": {
    "leaderboardSource": "Leaderboard",
    "cardSource":        "ProfileCard"
  },

  "rank": {
    "cooldownSeconds": 30
  },

  "bots": {
    "excludeBroadcaster": false,
    "broadcasterName":    ""
  },

  "debug": {
    "verbose": false
  }
}
```

---

## Champs racine

| Champ | Type | Défaut | Description |
|---|---|---|---|
| `dataPath` | string | `""` | **Obligatoire.** Chemin absolu vers `data/users/`. Doubles backslashs sous Windows. |
| `theme` | string | `"default"` | Thème visuel. Doit correspondre à un dossier dans `themes/`. |

---

## Section `xp`

| Champ | Type | Défaut | Description |
|---|---|---|---|
| `perMessage` | int | `10` | XP par message chat valide. |
| `perWatchInterval` | int | `5` | XP par cycle de watchtime. |
| `cooldownSeconds` | int | `30` | Délai minimum (secondes) entre deux gains d'XP par message pour un même viewer. |
| `minMessageLength` | int | `2` | Longueur minimale du message. Les commandes (`!`, `/`, `.`) sont toujours ignorées. |

---

## Section `watchtime`

| Champ | Type | Défaut | Description |
|---|---|---|---|
| `enabled` | bool | `true` | Active le watchtime. `false` = aucun XP watchtime distribué. |
| `intervalMinutes` | int | `5` | Fréquence du trigger Present Viewers. Doit correspondre à la configuration du trigger dans Streamer.bot. |
| `streakEnabled` | bool | `true` | Active le bonus WatchStreak (fidélité consécutive). |
| `countOffline` | bool | `false` | Si `true`, distribue du watchtime même hors live. Utile pour les tests. |

**Bonus WatchStreak :**

| Streak | Durée consécutive | XP bonus |
|---|---|---|
| 3–5 cycles | 15 min+ | +1 XP |
| 6–11 cycles | 30 min+ | +2 XP |
| 12–23 cycles | 1h+ | +3 XP |
| 24+ cycles | 2h+ | +5 XP |

---

## Section `leaderboard`

| Champ | Type | Défaut | Description |
|---|---|---|---|
| `intervalMinutes` | int | `5` | Fréquence de mise à jour du leaderboard OBS (Timer SB). |
| `topCount` | int | `10` | Nombre de viewers affichés. |

---

## Section `obs`

Les noms doivent correspondre **exactement** aux noms des Browser Sources dans OBS.

| Champ | Type | Défaut | Description |
|---|---|---|---|
| `leaderboardSource` | string | `"Leaderboard"` | Nom de la Browser Source OBS leaderboard. |
| `cardSource` | string | `"ProfileCard"` | Nom de la Browser Source OBS profile card. |

---

## Section `rank`

| Champ | Type | Défaut | Description |
|---|---|---|---|
| `cooldownSeconds` | int | `30` | Délai minimum entre deux `!rank` pour un même viewer. |

---

## Section `bots`

Les bots standards sont dans `configs/excluded-users.json`. Cette section gère uniquement le broadcaster.

| Champ | Type | Défaut | Description |
|---|---|---|---|
| `excludeBroadcaster` | bool | `false` | Exclure le broadcaster du système. |
| `broadcasterName` | string | `""` | Login Twitch du broadcaster (minuscules). Requis si `excludeBroadcaster: true`. |

---

## Section `debug`

| Champ | Type | Défaut | Description |
|---|---|---|---|
| `verbose` | bool | `false` | Réservé — pas d'effet actuellement. |

---

## Section `rewards`

| Champ | Type | Défaut | Description |
|---|---|---|---|
| `bonusXpEnabled` | bool | `true` | Active la feature Double XP Channel Point. |
| `bonusXpMultiplier` | float | `2.0` | Multiplicateur appliqué (ex: 2.0 = double XP). |
| `bonusXpDurationMinutes` | int | `30` | Durée du bonus en minutes. |
| `grantXpEnabled` | bool | `true` | Active la feature Bonus XP Channel Point. |
| `grantXpAmount` | int | `100` | Montant d'XP octroyé par rachat. |

---

## Section `checkIn`

| Champ | Type | Défaut | Description |
|---|---|---|---|
| `enabled` | bool | `false` | Active le système de check-in quotidien. Doit être `true` pour fonctionner. |
| `channelPointName` | string | `"Check-in"` | Nom du Channel Point Twitch (doit correspondre exactement). |
| `xpPerCheckin` | int | `10` | XP par case cochée. |
| `xpCardComplete` | int | `100` | XP bonus quand la carte est complète (cardSize cases). |
| `cardSize` | int | `10` | Nombre de cases sur la carte de fidélité. |
| `animationDurationMs` | int | `5000` | Durée de l'animation overlay check-in (ms). |

---

## Comportement des valeurs manquantes

Si un champ est absent du JSON, `ConfigService` applique automatiquement la valeur par défaut. Si une section entière est absente (`"watchtime"` manquant par exemple), elle est recréée avec tous ses défauts. **La configuration n'a pas besoin d'être complète pour fonctionner.**

---

## Configuration minimale fonctionnelle

```json
{
  "dataPath": "C:\\Stream\\streamerbot-xp-system\\data\\users"
}
```

Tous les autres champs utilisent leurs valeurs par défaut.

---

## Chemins Windows

```json
"dataPath": "C:\\Users\\TonNom\\streamerbot-xp-system\\data\\users"
```

Les backslashs **doivent être doublés** dans JSON. Les chemins relatifs ne sont pas supportés.

---

## Prise d'effet

Modifier `config.json` prend effet au **prochain déclenchement** de chaque action — aucune recompilation nécessaire. Chaque action embarque sa propre copie de `ConfigService` qui relit le fichier à chaque exécution.

---

## Paramètres URL des overlays

### card.html

| Paramètre | Défaut | Description |
|-----------|--------|-------------|
| `?theme=` | `default` | Thème visuel (default, rpg, cyber, minimal, tokyo, sakura) |
| `?wsport=` | `8080` | Port WebSocket de Streamer.bot |
| `?display=` | `8000` | Durée d'affichage de la card en millisecondes |

Exemple : `card.html?theme=rpg&wsport=8080&display=10000`

### leaderboard.html

| Paramètre | Défaut | Description |
|-----------|--------|-------------|
| `?theme=` | `default` | Thème visuel |
| `?wsport=` | `8080` | Port WebSocket de Streamer.bot |
| `?display=` | `10000` | Durée d'affichage du leaderboard en millisecondes |
| `?dismiss=` | `460` | Durée de l'animation de fermeture en millisecondes |
| `?dev` | absent | Mode développement avec données fictives |

Exemple : `leaderboard.html?theme=cyber&wsport=9090&display=15000`

**Dans OBS — Source Browser → URL :**
```
file:///C:/Stream/streamerbot-xp-system/overlays/card/card.html?wsport=8080&display=8000
```

---

## Paramètre debug.verbose

`"debug": { "verbose": true }` active les logs détaillés dans la console Streamer.bot.

Logs ajoutés en mode verbose :
- Raison de chaque skip de validation (cooldown, commande, trop court) — `XP_Add`
- Skip du cycle watchtime (stream offline, watchtime désactivé) — `XP_WatchTime_V2`
- Skip card pour compte bot exclu — `CARD_ShowProfile`
- Skip cooldown `!rank` — `RANK_ShowCommand`

Laisser `false` en production pour réduire le bruit dans les logs SB.
