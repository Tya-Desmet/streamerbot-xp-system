# Architecture technique

Schémas des flux de données, des composants et du cycle de vie des overlays.

---

## Vue d'ensemble du système

```
┌──────────────────────────────────────────────────────────────────┐
│  TWITCH                                                          │
│  Messages chat · Channel Points · (Timers)                       │
└────────────────────────────┬─────────────────────────────────────┘
                             │ Événements
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│  STREAMER.BOT                                                    │
│                                                                  │
│  ┌──────────────┐  ┌────────────────────┐  ┌─────────────────┐  │
│  │  XP_Add      │  │  LEADERBOARD_      │  │  CARD_          │  │
│  │  Chat Message│  │  Update (Timer)    │  │  ShowProfile    │  │
│  └──────┬───────┘  └────────┬───────────┘  └────────┬────────┘  │
│         │                  │                        │           │
│         ▼                  ▼                        ▼           │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Services C#                                             │   │
│  │  ConfigService · UserRepository · ValidationService     │   │
│  │  XpService · OBSService                                 │   │
│  └────────────────────────┬─────────────────────────────────┘   │
│                           │                                      │
│            ┌──────────────┼──────────────────┐                  │
│            ▼              ▼                  ▼                  │
│       JSON local   WebSocket Server     Logs Streamer.bot       │
│       data/users/  ws://127.0.0.1:8080                          │
└───────────────────────────┬──────────────────────────────────────┘
                            │ WebSocket Messages
                            ▼
┌──────────────────────────────────────────────────────────────────┐
│  OBS STUDIO                                                      │
│                                                                  │
│  ┌─────────────────────────┐  ┌─────────────────────────────┐   │
│  │  Browser Source         │  │  Browser Source             │   │
│  │  "Leaderboard"          │  │  "ProfileCard"              │   │
│  │  overlays/leaderboard/  │  │  overlays/card/             │   │
│  │  leaderboard.html       │  │  card.html                  │   │
│  └─────────────────────────┘  └─────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────┘
```

---

## Flow XP — Message chat

```
Viewer envoie un message Twitch
              │
              ▼
    XP_Add.Execute()
              │
              ▼
    USER_GetOrCreate
    ┌──────────────────────────────┐
    │  data/users/{user}.json      │
    │  existe ?                    │
    │    OUI → LoadUser()          │
    │    NON → CreateUser() + Save │
    └──────────────┬───────────────┘
                  │ UserProfile
                  ▼
    ValidationService.Validate()
    ┌──────────────────────────────┐
    │  IsCommand()    → SKIP !,/,. │
    │  IsTooShort()   → SKIP < N   │
    │  IsOnCooldown() → SKIP < Xs  │
    └──────────────┬───────────────┘
                  │ (valide)
                  ▼
    XpService.AddXp(user, xpPerMessage)
    ┌──────────────────────────────┐
    │  user.Xp += amount           │
    │  Recalcule Level             │
    │  LevelUp détecté ?           │
    │  Met à jour Messages, TS     │
    └──────────────┬───────────────┘
                  │
                  ▼
    UserRepository.SaveUser(user)
    → Écrit data/users/{user}.json
              │
              ▼
    CPH.SetArgument()
    → xp_added · xp_total · xp_level
      xp_isLevelUp · xp_percentage
```

---

## Flow Leaderboard

```
Timer (toutes les N minutes)
              │
              ▼
    LEADERBOARD_Update.Execute()
              │
              ▼
    UserRepository.GetAllUsers()
    → Lit tous data/users/*.json
    → Retourne List<UserProfile>
              │
              ▼
    XpService.PrepareLeaderboard(users)
    → Trie par Xp décroissant
    → Prend les 10 premiers
    → Attribue Rank 1-10
              │
              ▼
    OBSService.SendLeaderboard(top10)
    → CPH.WebsocketBroadcastJson()
    → { "players": [...] }
              │
              ▼  WebSocket
    leaderboard.html
              │
        ┌─────┴─────┐
        ▼           ▼
    podium.js    top10.js
    Rangs 1-3    Rangs 4-10
        │           │
        └─────┬─────┘
              ▼
    animation.js
    → Reveal staggeré par slot
    → GPU : transform + opacity
```

---

## Flow Carte de profil

```
Channel Point racheté par un viewer
              │
              ▼
    CARD_ShowProfile.Execute()
              │
              ▼
    USER_GetOrCreate
    → Charge UserProfile
    → Introuvable → exit silencieux
              │
              ▼
    UserRepository.GetAllUsers()
    → Calcule rang réel en temps réel
              │
              ▼
    XpService.GetProgress(user)
    → xpIntoLevel · xpForNext · percentage
              │
              ▼
    OBSService.SendProfileCard(payload)
    → { username, level, rank, percentage... }
              │
              ▼  WebSocket
    card.html
              │
              ▼
    renderer.js → peuple DOM
    (username, level, barre XP, rang)
              │
              ▼
    animations.js
    → animateIn()  — slide depuis la gauche
    → setTimeout(8 000ms)
    → animateOut() — slide vers la gauche
```

---

## Cycle de vie d'un overlay

```
OBS charge la Browser Source
              │
              ▼
    HTML parsé → CSS et thème appliqués
              │
              ▼
    Modules JS chargés (ordre défini dans HTML)

    Leaderboard : animation.js → state.js → podium.js
                  → top10.js → leaderboard.js → socket.js

    Card        : animations.js → renderer.js
                  → main.js → socket.js
              │
              ▼
    socket.js démarre
    → Connexion ws://127.0.0.1:8080
    → Échec → retry toutes les 3s
              │
              ▼
    Connexion établie
    → "[Overlay] WebSocket connecté"
    → Overlay en attente de messages
              │
              ▼  (réception message WebSocket)
    Message reçu et parsé en JSON
    → Dispatch vers leaderboard.js ou main.js
    → Mise à jour DOM
    → Déclenchement des animations
```

---

## Structure des fichiers overlays

```
overlays/
├── leaderboard/
│   ├── leaderboard.html     Point d'entrée — structure HTML du podium
│   ├── leaderboard.css      Styles et animations GPU
│   ├── leaderboard.js       Contrôleur — updateLeaderboard()
│   ├── socket.js            WebSocket — connexion et parsing
│   ├── podium.js            Rendu des rangs 1-3
│   ├── top10.js             Rendu des rangs 4-10
│   ├── state.js             Machine d'états (idle / running / visible / hiding)
│   ├── animation.js         Helpers — revealEl(), cancelAll()
│   └── themes/
│       ├── default/theme.css
│       ├── rpg/theme.css
│       ├── cyber/theme.css
│       ├── minimal/theme.css
│       ├── tokyo/theme.css
│       └── sakura/theme.css
│
└── card/
    ├── card.html            Point d'entrée — structure HTML de la card
    ├── card.css             Styles, barre XP scaleX(), animations
    ├── main.js              API publique — showCard(), hideCard(), setTheme()
    ├── socket.js            WebSocket — connexion et parsing
    ├── renderer.js          Population DOM — username, level, barre XP
    ├── animations.js        animateIn() / animateOut()
    └── themes/
        ├── default/theme.css
        ├── rpg/theme.css
        ├── cyber/theme.css
        ├── minimal/theme.css
        ├── tokyo/theme.css
        └── sakura/theme.css
```

---

## Schéma du dossier complet

```
streamerbot-xp-system/
│
├── actions/                    Scripts C# à importer dans Streamer.bot
│   ├── USER_GetOrCreate.cs     Charge ou crée un profil viewer
│   ├── XP_Add.cs               Pipeline XP complet
│   ├── LEADERBOARD_Update.cs   Construit et envoie le Top 10
│   └── CARD_ShowProfile.cs     Affiche la carte de profil
│
├── scripts/                    Services de référence (logique métier)
│   ├── ConfigService.cs        Chargement config.json avec fallbacks
│   ├── UserRepository.cs       I/O JSON — aucune logique métier
│   ├── ValidationService.cs    Anti-spam, cooldown, filtres
│   ├── XpService.cs            XP, niveaux, classement
│   └── OBSService.cs           Transport vers OBS via WebSocket
│
├── overlays/                   Browser Sources OBS
│   ├── leaderboard/            Podium Top 10 animé (800×400)
│   └── card/                   Carte de profil viewer (500×150)
│
├── configs/
│   └── config.json             Paramètres centralisés
│
├── data/
│   └── users/                  Profils viewers JSON — créé automatiquement
│
├── docs/                       Documentation
├── backup/                     Sauvegardes manuelles
└── README.md
```

---

## Format des données viewer

**Chemin :** `data/users/{username}.json`

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
| `Level` | int | Niveau actuel, calculé depuis Xp |
| `Messages` | int | Nombre de messages valides |
| `WatchTime` | int | Minutes de watch (réservé V2) |
| `Rank` | int | Calculé à la volée — non persisté |
| `LastMessageTimestamp` | long | Unix timestamp pour le cooldown |
