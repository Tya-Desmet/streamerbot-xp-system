# Guide d'installation

Streamer.bot XP System — de zéro à fonctionnel en moins de 15 minutes.

---

## Sommaire

1. [Prérequis](#1-prérequis)
2. [Télécharger le projet](#2-télécharger-le-projet)
3. [Configurer config.json](#3-configurer-configjson)
4. [Configurer Streamer.bot](#4-configurer-streamerbot)
5. [Configurer OBS](#5-configurer-obs)
6. [Tester le système](#6-tester-le-système)

---

## 1. Prérequis

| Logiciel | Version minimum | Lien |
|---|---|---|
| **Streamer.bot** | 0.2.0+ | [streamer.bot](https://streamer.bot) |
| **OBS Studio** | 29.0+ | [obsproject.com](https://obsproject.com) |
| Compte Twitch connecté à Streamer.bot | — | — |

**Connaissances requises :**
- Savoir créer une action dans Streamer.bot
- Savoir ajouter une Browser Source dans OBS
- Savoir créer une récompense de channel points sur Twitch

---

## 2. Télécharger le projet

Télécharge le projet et extrais-le dans un dossier **fixe et permanent**. Ce dossier ne doit jamais être déplacé après la configuration — les chemins sont absolus.

**Exemple de chemin recommandé :**
```
C:\StreamerTools\streamerbot-xp-system\
```

> Évite les dossiers `Téléchargements` ou `Bureau` — ils peuvent être déplacés accidentellement.

Crée ensuite manuellement le dossier de données (il sera rempli automatiquement au premier lancement) :
```
C:\StreamerTools\streamerbot-xp-system\data\users\
```

---

## 3. Configurer config.json

Ouvre le fichier `configs/config.json` avec n'importe quel éditeur texte (Notepad, VS Code, etc.).

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

**La seule valeur obligatoire à modifier :**

Remplace `dataPath` par le chemin absolu vers ton dossier `data\users\` :
```json
"dataPath": "C:\\StreamerTools\\streamerbot-xp-system\\data\\users"
```

> Dans les chemins JSON Windows, utilise des doubles antislashs `\\` ou des slashs simples `/`. Un seul antislash `\` provoquera une erreur.

Pour le détail de tous les paramètres, consulte [CONFIGURATION.md](CONFIGURATION.md).

---

## 4. Configurer Streamer.bot

### 4.1 Activer le serveur WebSocket

Les overlays OBS communiquent avec Streamer.bot via WebSocket. Ce serveur doit être activé manuellement.

1. Dans Streamer.bot : **Servers/Clients → WebSocket Server**
2. Cocher **Enabled**
3. Port : `8080` (laisser par défaut)
4. Cliquer **Save**

> Le serveur doit être actif **avant** d'ouvrir les overlays dans OBS. Les overlays se reconnectent automatiquement s'ils perdent la connexion.

---

### 4.2 Créer la variable globale

Le système utilise une seule variable globale pour localiser la configuration.

1. Dans Streamer.bot : **Settings → Global Variables → Add**
2. Renseigner :

| Nom | Valeur | Type |
|---|---|---|
| `xp_configPath` | `C:\StreamerTools\streamerbot-xp-system\configs\config.json` | String |

3. Cocher **Persisted** — la variable doit survivre aux redémarrages de Streamer.bot

> Le chemin doit pointer vers le fichier `config.json`, pas vers le dossier `configs\`.

---

### 4.3 Importer les actions C#

Tu dois créer 4 actions dans Streamer.bot. Pour chaque action, la procédure est la même :

**Procédure d'import :**
1. Dans Streamer.bot : **Actions → clic droit → Add Action**
2. Donner le nom indiqué ci-dessous
3. Ajouter une sous-action : **Core → C# → Execute C# Code**
4. Ouvrir le fichier `.cs` correspondant dans un éditeur texte
5. Sélectionner tout (Ctrl+A), copier (Ctrl+C)
6. Coller dans l'éditeur C# de Streamer.bot
7. Cliquer **Compile** — aucune erreur ne doit apparaître
8. Cliquer **Save**

**Les 4 actions à créer :**

---

#### Action : USER_GetOrCreate

- Fichier : `actions/USER_GetOrCreate.cs`
- Déclencheur : **aucun** — elle est appelée par les autres actions
- Rôle : charge ou crée le profil JSON d'un viewer

---

#### Action : XP_Add

- Fichier : `actions/XP_Add.cs`
- Déclencheur : **Twitch → Chat Message**
- Rôle : valide le message, attribue l'XP, sauvegarde le profil

Structure de l'action dans Streamer.bot :
```
Action : XP_Add
  [Trigger]       Twitch > Chat Message
  [Sous-action 1] Run Action > USER_GetOrCreate
  [Sous-action 2] Execute C# Code > [contenu XP_Add.cs]
```

> `USER_GetOrCreate` **doit** être la première sous-action.

---

#### Action : LEADERBOARD_Update

- Fichier : `actions/LEADERBOARD_Update.cs`
- Déclencheur : **Core → Timers** — intervalle recommandé : 5 minutes
- Rôle : lit tous les profils, construit le Top 10, l'envoie à OBS

---

#### Action : CARD_ShowProfile

- Fichier : `actions/CARD_ShowProfile.cs`
- Déclencheur : **Twitch → Channel Point Redemption** (sélectionner la récompense voulue)
- Rôle : affiche la carte de profil du viewer qui a racheté la récompense

> Crée la récompense Channel Points sur Twitch d'abord si elle n'existe pas encore.

---

## 5. Configurer OBS

### 5.1 Overlay Leaderboard

1. Dans OBS, ouvre la scène souhaitée
2. Ajoute une source : **+** → **Browser Source**
3. Paramètres :

| Paramètre | Valeur |
|---|---|
| **Nom** | `Leaderboard` (doit correspondre exactement à `obsLeaderboardSource` dans config.json) |
| **Local file** | Coché — pointer vers `overlays/leaderboard/leaderboard.html` |
| **Largeur** | `800` |
| **Hauteur** | `400` |
| **Shutdown source when not visible** | Coché |

4. Valider — la source doit charger l'overlay

**Vérification :** clic droit sur la source → **Interact** → ouvre F12 (console). Tu dois voir :
```
[Leaderboard] WebSocket connecté à Streamer.bot
```

---

### 5.2 Overlay Profile Card

1. Ajoute une autre **Browser Source**
2. Paramètres :

| Paramètre | Valeur |
|---|---|
| **Nom** | `ProfileCard` (doit correspondre exactement à `obsCardSource` dans config.json) |
| **Local file** | Coché — pointer vers `overlays/card/card.html` |
| **Largeur** | `500` |
| **Hauteur** | `150` |

3. Positionne la source à l'endroit voulu sur ta scène (recommandé : bas gauche ou bas droit)

**Vérification :** même procédure — la console doit afficher :
```
[Card] WebSocket connecté à Streamer.bot
```

---

## 6. Tester le système

### Test 1 — Créer un profil viewer

1. Dans Streamer.bot : **Actions → XP_Add → clic droit → Test**
2. Renseigner les champs du test :
   - `userName` : `testviewer`
   - `userDisplayName` : `TestViewer`
   - `rawInput` : `Bonjour le stream !`
3. Cliquer **Run**
4. Vérifier que le fichier `data/users/testviewer.json` a été créé

Contenu attendu :
```json
{
  "Username": "testviewer",
  "DisplayName": "TestViewer",
  "Xp": 10,
  "Level": 1,
  "Messages": 1,
  "WatchTime": 0,
  "Rank": 0,
  "LastMessageTimestamp": 1748000000
}
```

---

### Test 2 — Afficher le leaderboard

1. Crée 2-3 profils supplémentaires via la méthode ci-dessus avec des noms différents
2. Modifie les fichiers JSON manuellement pour leur donner des XP différents (pour avoir un classement visible)
3. Dans Streamer.bot : **Actions → LEADERBOARD_Update → clic droit → Run**
4. L'overlay OBS doit s'animer et afficher le podium

---

### Test 3 — Afficher la carte de profil

1. Dans Streamer.bot : **Actions → CARD_ShowProfile → clic droit → Test**
2. Renseigner `userName` avec un nom de viewer existant dans `data/users/`
3. La carte doit apparaître dans OBS pendant environ 8 secondes, puis disparaître automatiquement

---

### L'installation est terminée

Le système est opérationnel. Les viewers accumulent de l'XP à chaque message chat, le leaderboard se met à jour automatiquement, et la carte de profil s'affiche sur redemption de Channel Point.

**Prochaines étapes recommandées :**
- Personnaliser le thème visuel : [THEMES.md](THEMES.md)
- Ajuster les paramètres XP : [CONFIGURATION.md](CONFIGURATION.md)
- Résoudre un problème : [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
