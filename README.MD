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
| Export web (`EXPORT_Snapshot` → JSON meta/leaderboard/profils) | ✅ V3 |
| Hub web public : classement + profils viewer + planning | ✅ V3 |
| Leaderboard & profils **live** (push bot → backend → site) | ✅ V3 |

---

## Architecture

```
Twitch Chat ─→ Streamer.bot (actions C#) ─→ data/users/*.json
                                         ├→ WebSocket ─→ OBS Browser Sources
                                         └→ EXPORT_Snapshot ─→ exports/*.json
                                                                  │
                  push-to-backend.ps1 (tâche planifiée) ◀─────────┘
                            └→ POST /api/push ─→ backend Node ─→ site public (live)
```

**Une seule variable globale dans Streamer.bot :** `xp_configPath` → chemin vers `configs/config.json`

---

## Installation rapide

1. Placer le projet dans un dossier stable et créer `data/users/`
2. Éditer `configs/config.json` — renseigner `dataPath`
3. Dans Streamer.bot : créer `xp_configPath`, activer WebSocket, importer les actions
4. Dans OBS : ajouter les 2 Browser Sources
5. *(Optionnel)* Hub web public : activer l'export + push, déployer le site → [INSTALLATION.md §8](docs/INSTALLATION.md)

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
├── data/users/           # Profils viewers (JSON, gitignorés)
│
│   # ── Hub web (V3) ──
├── website/              # Site public Next.js (export statique)
├── backend/              # API Node/Express (leaderboard + profils live)
├── tools/                # Scripts : build-actions, push-to-backend, deploy-*
└── exports/              # JSON générés par EXPORT_Snapshot (gitignorés)
```

---

## Documentation

| Document | Contenu |
|---|---|
| [INSTALLATION.md](docs/INSTALLATION.md) | Setup complet étape par étape (~20 min) + section 8 « Hub web » |
| [CONFIGURATION.md](docs/CONFIGURATION.md) | Référence complète de config.json |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Flux de données, services, UserProfile |
| [DEPLOY.md](docs/DEPLOY.md) | Mise en ligne du site + backend (Infomaniak) |
| [EXPORT_CONTRACT.md](docs/EXPORT_CONTRACT.md) | Schéma des exports JSON (bot ⇄ hub web) |
| [THEMES.md](docs/THEMES.md) | Changer et créer des thèmes |
| [TITLES.md](docs/TITLES.md) | Configurer les titres par niveau |
| [BOT_EXCLUSION.md](docs/BOT_EXCLUSION.md) | Gérer la liste des bots exclus |
| [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) | Diagnostic et résolution de problèmes |
| [tools/README-tools.md](tools/README-tools.md) | Scripts de build, push et déploiement |

---

## État du projet

| Version | État | Contenu |
|---|---|---|
| V1 | ✅ Fonctionnelle | XP chat, levels, leaderboard, profile card, 6 thèmes |
| V2 | ✅ Implémentée | Watchtime hybride, !rank, titres, bot exclusion, config centralisée |
| V3 | ✅ Implémentée | Hub web public : export, push live, classement + profils viewer, planning |
| V4 | 🔲 Planifiée | Boss system, récompenses, saisons, achievements |

---

## Licence

[MIT](LICENSE) — Desmet Tya
