# Streamer.bot XP System — Guide d'installation complet

Système de gamification Twitch entièrement local : XP, niveaux, leaderboard et profile cards intégrés dans OBS via Streamer.bot.

---

## Sommaire

1. [Présentation](#1-présentation)
2. [Prérequis](#2-prérequis)
3. [Installation](#3-installation)
4. [Configuration](#4-configuration)
5. [Utilisation](#5-utilisation)
6. [Structure du projet](#6-structure-du-projet)
7. [Dépannage](#7-dépannage)
8. [Étendre le système](#8-étendre-le-système)

---

## 1. Présentation

### Ce que le système fait

Chaque message de chat valide rapporte des points XP au viewer. Quand un viewer accumule assez de XP, il monte de niveau. Un leaderboard Top 3 se rafraîchit automatiquement dans OBS. En rachetant une récompense de channel points, un viewer peut afficher sa profile card en direct.

### Fonctionnalités V1 incluses

| Fonctionnalité | Description |
|---|---|
| XP par message | Chaque message valide rapporte des points |
| Anti-spam | Cooldown configurable + longueur minimum |
| Système de niveaux | Formule progressive : `100 × niveau^1.5` XP par niveau |
| Détection level up | Logs automatiques lors d'une montée de niveau |
| Leaderboard Top 3 | Podium animé mis à jour périodiquement dans OBS |
| Profile Card | Card animée déclenchée par une récompense Twitch |
| Données locales | Tout est stocké en JSON sur ta machine |

### Ce que le système ne fait pas (encore)

- Pas de XP watchtime (prévu V2)
- Pas de commande `!rank` dans le chat (prévu V2)
- Pas d'export web (prévu V3)
- Pas d'achievements ni de quêtes (prévu V4)

---

## 2. Prérequis

### Logiciels requis

| Logiciel | Version minimum | Lien |
|---|---|---|
| **Streamer.bot** | 0.2.0+ | [streamer.bot](https://streamer.bot) |
| **OBS Studio** | 29.0+ | [obsproject.com](https://obsproject.com) |
| Compte Twitch connecté à Streamer.bot | — | — |
| **Serveur WebSocket Streamer.bot activé** | intégré 0.2.0+ | voir étape 3.5 |

### Connaissances requises

- Savoir créer une action dans Streamer.bot
- Savoir ajouter une Browser Source dans OBS
- Savoir créer une récompense de channel points sur Twitch

### Espace disque

Moins de 5 MB pour le projet. Chaque profil viewer pèse environ 1 KB.

---

## 3. Installation

### 3.1 Télécharger le projet

Place le dossier du projet à un endroit fixe sur ton disque. Il ne doit **jamais être déplacé** après la configuration (les chemins sont absolus).

Exemple de chemin recommandé :
```
C:\Streamer\streamerbot-xp-system\
```

### 3.2 Créer le dossier de données

Crée manuellement le dossier suivant (il sera rempli automatiquement) :
```
C:\Streamer\streamerbot-xp-system\data\users\
```

### 3.3 Configurer la variable globale dans Streamer.bot

Dans Streamer.bot : **Settings → Global Variables → Add**

Une seule variable suffit — tout le reste est dans `configs/config.json` :

| Nom | Valeur exemple | Type |
|---|---|---|
| `xp_configPath` | `C:\Streamer\streamerbot-xp-system\configs\config.json` | String |

> **Important :** Coche **Persisted** pour que la variable survive au redémarrage de Streamer.bot.

Ensuite, ouvre `configs/config.json` et remplace `dataPath` par le chemin réel vers ton dossier `data\users\`.

### 3.4 Importer les actions C#

Pour chaque fichier dans le dossier `actions/`, tu dois créer une action dans Streamer.bot :

#### Action : USER_GetOrCreate

1. **Actions** → clic droit → **Add Action**
2. Nom : `USER_GetOrCreate`
3. Ajoute une sous-action : **Core → C# → Execute C# Code**
4. Ouvre le fichier `actions/USER_GetOrCreate.cs` dans un éditeur texte
5. Sélectionne tout (Ctrl+A), copie (Ctrl+C)
6. Colle dans l'éditeur Streamer.bot
7. Clique **Compile** — vérifie qu'il n'y a pas d'erreurs
8. Clique **Save**

> Cette action n'a pas de déclencheur — elle sera appelée par les autres actions.

#### Action : XP_Add

1. **Actions** → **Add Action** → Nom : `XP_Add`
2. Ajoute le déclencheur : **Twitch → Chat Message** (ou `+` dans la zone Triggers)
3. Ajoute une première sous-action : **Core → Actions → Run Action** → sélectionne `USER_GetOrCreate`
4. Ajoute une deuxième sous-action : **Core → C# → Execute C# Code**
5. Copie-colle le contenu de `actions/XP_Add.cs`
6. Compile et sauvegarde

```
Action : XP_Add
  [Trigger] Twitch > Chat Message
  [Sub-Action 1] Run Action > USER_GetOrCreate
  [Sub-Action 2] Execute C# Code > [contenu XP_Add.cs]
```

#### Action : LEADERBOARD_Update

1. **Actions** → **Add Action** → Nom : `LEADERBOARD_Update`
2. Ajoute le déclencheur : **Core → Timers** → Interval : `5 minutes`
3. Ajoute une sous-action : **Core → C# → Execute C# Code**
4. Copie-colle le contenu de `actions/LEADERBOARD_Update.cs`
5. Compile et sauvegarde

#### Action : CARD_ShowProfile

1. **Actions** → **Add Action** → Nom : `CARD_ShowProfile`
2. Ajoute le déclencheur : **Twitch → Channel Point Redemption**
3. Sélectionne la récompense de ton choix (crée-la sur Twitch d'abord si nécessaire)
4. Ajoute une sous-action : **Core → C# → Execute C# Code**
5. Copie-colle le contenu de `actions/CARD_ShowProfile.cs`
6. Compile et sauvegarde

### 3.5 Activer le serveur WebSocket Streamer.bot

Les overlays communiquent avec Streamer.bot via WebSocket. Ce serveur est intégré à Streamer.bot mais doit être activé manuellement.

1. Dans Streamer.bot : **Servers/Clients → WebSocket Server**
2. Cocher **Enabled**
3. Port : `8080` (laisser par défaut)
4. Cliquer **Save**

> Le serveur doit être actif **avant** d'ouvrir les Browser Sources dans OBS. Les overlays se reconnectent automatiquement si la connexion est perdue.

### 3.6 Installer les overlays OBS

#### Overlay Leaderboard

1. Dans OBS, ouvre la scène où tu veux afficher le leaderboard
2. Ajoute une source : **Browser Source**
3. Paramètres :
   - **Nom :** `Leaderboard`
   - **Local file :** coché → **Browse** → sélectionne `overlays/leaderboard/leaderboard.html`
   - **Largeur :** `800` — **Hauteur :** `400`
   - **Shutdown source when not visible :** coché
4. Ouvre la console (clic droit → **Interact** → F12) — tu dois voir :
   ```
   [Leaderboard] ✅ WebSocket connecté à Streamer.bot
   [Leaderboard] ✅ DOM OK
   ```

#### Overlay Profile Card

1. Dans OBS, ajoute une autre **Browser Source**
2. Paramètres :
   - **Nom :** `ProfileCard`
   - **Local file :** coché → **Browse** → sélectionne `overlays/card/card.html`
   - **Largeur :** `500` — **Hauteur :** `150`
3. Positionne la source en bas à gauche de ton écran
4. Ouvre la console (clic droit → **Interact** → F12) — tu dois voir :
   ```
   [Card] ✅ WebSocket connecté à Streamer.bot
   [Card] ✅ DOM OK
   ```

> **Si la console affiche `❌ WebSocket erreur`** : le serveur WebSocket Streamer.bot n'est pas activé (voir étape 3.5).

---

## 4. Configuration

### 4.1 Le fichier config.json

Situé dans `configs/config.json`. Ce fichier centralise tous les paramètres du système.

```json
{
  "dataPath":                   "C:\\Streamer\\streamerbot-xp-system\\data\\users",
  "xpPerMessage":               10,
  "xpPerWatch":                 1,
  "cooldownSeconds":            30,
  "minMessageLength":           2,
  "leaderboardIntervalMinutes": 5,
  "obsLeaderboardSource":       "Leaderboard",
  "obsCardSource":              "ProfileCard"
}
```

> **Note :** Dans les chemins Windows en JSON, utilise des doubles antislashs `\\` ou des slashs simples `/`.

### 4.2 Paramètres expliqués

| Paramètre | Valeur par défaut | Description |
|---|---|---|
| `dataPath` | — | Chemin absolu vers le dossier `data/users/` |
| `xpPerMessage` | `10` | XP gagnés par message valide |
| `xpPerWatch` | `1` | XP par minute de watch (prévu V2) |
| `cooldownSeconds` | `30` | Secondes entre deux messages XP pour le même viewer |
| `minMessageLength` | `2` | Longueur minimum du message pour gagner des XP |
| `leaderboardIntervalMinutes` | `5` | Fréquence de mise à jour du leaderboard OBS |
| `obsLeaderboardSource` | `Leaderboard` | Nom exact de la Browser Source leaderboard dans OBS |
| `obsCardSource` | `ProfileCard` | Nom exact de la Browser Source profile card dans OBS |

### 4.3 Système de niveaux — référence XP

| Niveau | XP requis pour monter | XP total depuis niveau 1 |
|---|---|---|
| 1 → 2 | 100 XP | 100 XP |
| 2 → 3 | 283 XP | 383 XP |
| 3 → 4 | 520 XP | 903 XP |
| 5 → 6 | 1 118 XP | ~3 400 XP |
| 10 → 11 | 3 162 XP | ~17 000 XP |

Formule : `XP requis = 100 × niveau^1.5`

### 4.4 Thèmes visuels

Les deux overlays supportent 4 thèmes. Pour changer le thème, modifie directement le fichier HTML :

```html
<!-- Dans leaderboard.html et card.html, ligne <body> -->
<body class="theme-blue">
```

Thèmes disponibles : `theme-default` (violet Twitch) · `theme-blue` · `theme-red` · `theme-green`

Après modification, clique **Refresh cache** sur la Browser Source OBS.

### 4.5 Anti-spam — messages ignorés

Un message est ignoré (sans XP, sans erreur) si :
- Il commence par `!`, `/` ou `.` (commande Twitch)
- Il fait moins de `minMessageLength` caractères
- Le viewer a déjà envoyé un message il y a moins de `cooldownSeconds` secondes

---

## 5. Utilisation

### 5.1 Tester le pipeline XP

1. Lance Streamer.bot et connecte ton compte Twitch
2. Ouvre **Actions** → **XP_Add** → clic droit → **Test**
3. Dans la fenêtre de test, remplis :
   - `userName` : `testviewer`
   - `userDisplayName` : `TestViewer`
   - `rawInput` : `Bonjour le stream !`
4. Clique **Run**
5. Vérifie dans `data/users/testviewer.json` que le fichier a été créé

Contenu attendu après un premier message :
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

### 5.2 Vérifier l'XP d'un viewer

Ouvre le fichier correspondant dans `data/users/` :
```
data/users/nomduviewerenminuscules.json
```

Les champs importants :
- `Xp` : XP total accumulé
- `Level` : niveau actuel
- `Messages` : nombre de messages valides

### 5.3 Tester le leaderboard OBS

1. Crée au moins 2-3 profils de test via la méthode 5.1
2. Modifie manuellement les fichiers JSON pour leur donner des XP différents
3. Dans Streamer.bot : **Actions** → **LEADERBOARD_Update** → clic droit → **Test**
4. L'overlay OBS doit afficher le podium avec les 3 premiers viewers

### 5.4 Tester la profile card

**Via test Streamer.bot :**
1. **Actions** → **CARD_ShowProfile** → clic droit → **Test**
2. Remplis `userName` avec un nom de viewer existant dans `data/users/`
3. La card doit apparaître dans OBS pendant 8 secondes

**Via Twitch en live :**
1. Un viewer rachète la récompense channel points configurée
2. La card affiche son pseudo, niveau, barre XP et rang en temps réel
3. La card disparaît automatiquement après 8 secondes

### 5.5 Forcer la mise à jour du leaderboard

Pour mettre à jour le leaderboard immédiatement sans attendre le timer :
- **Actions** → **LEADERBOARD_Update** → clic droit → **Run**

### 5.6 Lire les logs Streamer.bot

Chaque action préfixe ses messages de log avec son nom :
- `[USER_GetOrCreate]` : création / chargement de profil
- `[XP_Add]` : ajout XP, détection level up, messages ignorés
- `[LEADERBOARD_Update]` : envoi vers OBS
- `[CARD_ShowProfile]` : affichage card

Accès aux logs : **Streamer.bot → View → Log**

---

## 6. Structure du projet

```
streamerbot-xp-system/
│
├── actions/                    ← Scripts à importer dans Streamer.bot
│   ├── USER_GetOrCreate.cs     Crée ou charge un profil viewer
│   ├── XP_Add.cs               Pipeline principal : validation + XP + sauvegarde
│   ├── LEADERBOARD_Update.cs   Construit le Top 3 et l'envoie à OBS
│   └── CARD_ShowProfile.cs     Affiche la profile card via channel point reward
│
├── scripts/                    ← Services de référence (logique métier)
│   ├── UserRepository.cs       Lecture / écriture des profils JSON
│   ├── ValidationService.cs    Anti-spam, cooldown, filtres commandes
│   ├── XpService.cs            Calcul XP, niveaux, leaderboard
│   ├── ConfigService.cs        Chargement config.json avec fallbacks
│   └── OBSService.cs           Transport données → OBS Browser Sources
│
├── overlays/                   ← Fichiers HTML/CSS/JS des overlays OBS
│   ├── leaderboard/
│   │   ├── leaderboard.html    Structure du podium Top 3
│   │   ├── leaderboard.css     Styles et animations (GPU uniquement)
│   │   └── leaderboard.js      API : window.updateLeaderboard([...])
│   └── card/
│       ├── card.html           Structure de la profile card
│       ├── card.css            Styles, barre XP scaleX(), animations
│       └── card.js             API : window.showCard({...}) / hideCard()
│
├── configs/
│   └── config.json             Paramètres centralisés du système
│
├── data/
│   └── users/                  Profils viewers — créé automatiquement
│       ├── nomviewer1.json
│       └── nomviewer2.json
│
├── docs/                       Documentation technique
├── backup/                     Sauvegardes (manuel pour l'instant)
└── INSTALLATION.md             Ce fichier
```

### Rôle de chaque dossier

| Dossier | Rôle | Modifiable ? |
|---|---|---|
| `actions/` | Code C# importé dans Streamer.bot | ✅ Pour personnaliser |
| `scripts/` | Services de référence | ✅ Source de vérité |
| `overlays/` | Fichiers HTML des Browser Sources | ✅ Pour personnaliser le visuel |
| `configs/` | Configuration JSON | ✅ Paramètres à modifier |
| `data/users/` | Base de données locale | ⚠️ Ne pas modifier manuellement |

---

## 7. Dépannage

### Le fichier JSON d'un viewer n'est pas créé

**Symptôme :** après un message, aucun fichier dans `data/users/`

**Vérifications :**
1. La variable globale `xp_configPath` est-elle définie et persistante ?
2. Le fichier `configs/config.json` existe-t-il à ce chemin ?
3. Le champ `dataPath` dans `config.json` est-il renseigné et correct ?
4. Le dossier `data/users/` existe-t-il physiquement ?
5. L'action `USER_GetOrCreate` est-elle bien la **première** sous-action de `XP_Add` ?

**Log attendu :**
```
[USER_GetOrCreate] Nouveau joueur créé : NomViewer (nomviewer)
```

---

### L'XP ne s'incrémente pas

**Symptôme :** le fichier existe mais le champ `Xp` reste à 0

**Vérifications :**
1. Le message est-il une commande (`!`, `/`, `.`) → ignoré normalement
2. Le message est-il trop court ? Vérifie `xp_minMessageLength`
3. Le viewer a-t-il envoyé un message il y a moins de `xp_cooldownSeconds` secondes ?
4. Regarde les logs → `[XP_Add] xp_skipped: true, reason: cooldown` par exemple

**Log si message ignoré :** aucun log (comportement silencieux voulu)
**Log si level up :**
```
[XP_Add] LEVEL UP ! nomviewer : Niv. 1 → 2
```

---

### L'overlay leaderboard ne s'affiche pas dans OBS

**Symptôme :** la Browser Source reste noire ou vide après avoir déclenché l'action

**Vérifications :**
1. Le serveur WebSocket Streamer.bot est-il activé ? (`Servers/Clients → WebSocket Server → Enabled`)
2. Ouvre la console OBS (clic droit → Interact → F12) — vois-tu `✅ WebSocket connecté` ?
3. Si `❌ WebSocket erreur` : Streamer.bot n'est pas lancé ou le port 8080 est bloqué
4. Des profils existent-ils dans `data/users/` ? (déclenche d'abord `XP_Add` pour en créer)
5. Après chaque modification des fichiers JS : **Refresh cache** sur la Browser Source

**Test rapide :** ouvre `overlays/leaderboard/leaderboard.html?dev` dans un navigateur — un podium de test doit apparaître sans Streamer.bot.

---

### La profile card ne s'affiche pas

**Symptôme :** redemption faite mais rien dans OBS

**Vérifications :**
1. Le serveur WebSocket Streamer.bot est-il activé ? (`Servers/Clients → WebSocket Server → Enabled`)
2. Ouvre la console OBS de la card (clic droit → Interact → F12) — vois-tu `✅ WebSocket connecté` ?
3. Le viewer a-t-il un profil dans `data/users/` ? (s'il n'a jamais chatté, il n'existe pas)
4. Vérifier le log Streamer.bot :
   ```
   [CARD_ShowProfile] Profil introuvable pour 'nomviewer' — aucune card affichée
   ```
5. Après chaque modification des fichiers JS : **Refresh cache** sur la Browser Source

**Test rapide :** ouvre `overlays/card/card.html?dev` dans un navigateur — une card de test doit apparaître sans Streamer.bot.

---

### Problème de chemin dataPath

**Symptôme :** `[USER_GetOrCreate] dataPath non configuré dans configs/config.json` dans les logs

**Solution :**
1. **Settings → Global Variables** dans Streamer.bot — vérifie que `xp_configPath` existe et est persistante
2. Le chemin doit pointer vers `configs/config.json`, exemple :
   ```
   C:\Streamer\streamerbot-xp-system\configs\config.json
   ```
3. Ouvre `configs/config.json` et vérifie que `dataPath` est renseigné :
   ```json
   "dataPath": "C:\\Streamer\\streamerbot-xp-system\\data\\users"
   ```
4. Le chemin `dataPath` doit pointer vers `data/users/`, pas vers `data/`
5. En JSON, utilise des doubles antislashs `\\` ou des slashs simples `/`

---

### Streamer.bot affiche une erreur de compilation

**Symptôme :** le C# ne compile pas après copier-coller

**Vérifications :**
1. As-tu copié **tout** le contenu du fichier `.cs`, y compris les classes au-dessus de `CPHInline` ?
2. Streamer.bot nécessite .NET 6 minimum
3. Vérifie qu'il n'y a pas de caractères invisibles en début de fichier
4. Essaie de recopier depuis le fichier original sans passer par un éditeur riche (Word, etc.)

---

## 8. Étendre le système

### Architecture à respecter

Le système suit 3 règles fondamentales :

| Règle | Description |
|---|---|
| **Séparation des rôles** | Chaque fichier a une seule responsabilité |
| **Gateway XP** | Toute modification XP passe par `XpService.AddXp()` |
| **Pas de logique dans les overlays** | Les fichiers HTML/JS reçoivent des données, ils ne calculent rien |

### Ajouter un nouveau module

**Exemple : XP watchtime**

1. Crée une action `WATCHTIME_Add.cs` dans `actions/`
2. Elle doit appeler `USER_GetOrCreate` en première sous-action
3. Elle utilise `XpService.AddXp()` pour ajouter l'XP
4. Elle incrémente `user.WatchTime` via `UserRepository.SaveUser()`
5. Déclencheur : timer toutes les 5 minutes

**Exemple : commande `!rank` dans le chat**

1. Crée une action `RANK_Get.cs` dans `actions/`
2. Déclencheur : Twitch → Chat Command → `!rank`
3. Elle charge le profil via `UserRepository.LoadUser()`
4. Elle calcule le rang via `GetAllUsers()` + tri
5. Elle envoie une réponse chat via `CPH.SendMessage()`

**Exemple : système de badges**

1. Ajoute un champ `Badges` dans `UserProfile`
2. Crée un `BadgeService.cs` dans `scripts/`
3. Ce service vérifie les conditions de badge après chaque `AddXp()`
4. Il ne modifie jamais l'XP — uniquement les métadonnées badges

### Conventions de nommage

```
Actions    : DOMAINE_Verbe       → XP_Add, USER_GetOrCreate, CARD_ShowProfile
Services   : NomService.cs       → XpService, UserRepository, BadgeService
Overlays   : nom/nom.html        → leaderboard/leaderboard.html
Données    : {username}.json     → data/users/mystya.json
```

### Variables globales pour un nouveau module

Préfixe toujours avec `xp_` pour éviter les conflits avec d'autres bots :
```
xp_nomdumodule_parametre
```

---

## Récapitulatif de la configuration

**Une seule Global Variable dans Streamer.bot :**

| Variable | Valeur |
|---|---|
| `xp_configPath` | Chemin absolu vers `configs/config.json` |

**Tout le reste dans `configs/config.json` :**

| Clé | Valeur par défaut | Description |
|---|---|---|
| `dataPath` | *(à définir)* | Chemin vers `data/users/` |
| `xpPerMessage` | `10` | XP par message valide |
| `cooldownSeconds` | `30` | Anti-spam entre messages |
| `minMessageLength` | `2` | Longueur minimum message |
| `obsLeaderboardSource` | `Leaderboard` | Nom Browser Source OBS |
| `obsCardSource` | `ProfileCard` | Nom Browser Source OBS |

---

*Streamer.bot XP System — Documentation v1.0*
