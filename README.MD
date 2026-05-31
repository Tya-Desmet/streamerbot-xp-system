# Streamer.bot XP System

Système de progression XP pour streamers Twitch — intégration Streamer.bot + OBS, stockage local JSON, aucune base de données.

---

## Fonctionnalités

| Fonctionnalité | État |
|---|---|
| XP automatique par message chat | ✅ V1 |
| Niveaux calculés dynamiquement (`100 × N^1.5`) | ✅ V1 |
| Leaderboard Top 10 OBS animé | ✅ V1 |
| Profile Card OBS (Channel Point) | ✅ V1 |
| Anti-spam (cooldown, longueur min, filtre commandes) | ✅ V1 |
| 6 thèmes visuels (default, rpg, cyber, minimal, tokyo, sakura) | ✅ V1 |
| Commande `!rank` en chat | ✅ V2 |
| Titres par niveau (configurable par thème) | ✅ V2 |
| Exclusion bots centralisée (`excluded-users.json`) | ✅ V2 |
| Watchtime hybride (présence SB + activité chat) | ✅ V2 |
| WatchStreak — bonus de fidélité progressif | ✅ V2 |
| Configuration centralisée (un seul `config.json`) | ✅ V2 |

---

## Architecture

```
Twitch Chat ──→ Streamer.bot (6 actions C#) ──→ data/users/*.json
                                             ──→ WebSocket ──→ OBS Browser Sources
```

**Une seule variable globale dans Streamer.bot :** `xp_configPath` → chemin vers `configs/config.json`

---

## Installation rapide

1. Placer le projet dans un dossier stable et créer `data/users/`
2. Éditer `configs/config.json` — renseigner `dataPath`
3. Dans Streamer.bot : créer `xp_configPath`, activer WebSocket, importer les 6 actions
4. Dans OBS : ajouter les 2 Browser Sources

→ Guide complet : [docs/INSTALLATION.md](docs/INSTALLATION.md)

---

## Structure du projet

```
streamerbot-xp-system/
├── actions/              # Actions Streamer.bot (C# inline)
│   ├── USER_GetOrCreate.cs
│   ├── XP_Add.cs
│   ├── LEADERBOARD_Update.cs
│   ├── CARD_ShowProfile.cs
│   ├── RANK_ShowCommand.cs
│   └── XP_WatchTime_V2.cs
├── scripts/              # Services C# (référence canonique)
├── overlays/             # HTML/CSS/JS pour OBS
│   ├── leaderboard/
│   └── card/
├── configs/
│   ├── config.json       # Unique source de configuration
│   ├── titles.json       # Titres personnalisés
│   └── excluded-users.json
├── themes/               # Thèmes visuels
└── data/users/           # Profils viewers (JSON, gitignorés)
```

---

## Documentation

| Document | Contenu |
|---|---|
| [INSTALLATION.md](docs/INSTALLATION.md) | Setup complet étape par étape (~20 min) |
| [CONFIGURATION.md](docs/CONFIGURATION.md) | Référence complète de config.json |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Flux de données, services, UserProfile |
| [THEMES.md](docs/THEMES.md) | Changer et créer des thèmes |
| [TITLES.md](docs/TITLES.md) | Configurer les titres par niveau |
| [BOT_EXCLUSION.md](docs/BOT_EXCLUSION.md) | Gérer la liste des bots exclus |
| [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) | Diagnostic et résolution de problèmes |

---

## État du projet

| Version | État | Contenu |
|---|---|---|
| V1 | ✅ Fonctionnelle | XP chat, levels, leaderboard, profile card, 6 thèmes |
| V2 | ✅ Implémentée | Watchtime hybride, !rank, titres, bot exclusion, config centralisée |
| V3 | 🔲 Planifiée | Export web, statistiques avancées |
| V4 | 🔲 Planifiée | Boss system, récompenses, saisons, achievements |

---

## Licence

[MIT](LICENSE) — Desmet Tya
