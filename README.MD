# Streamer.bot XP System

[![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)](CHANGELOG.md)
[![Licence](https://img.shields.io/badge/licence-MIT-green.svg)](LICENSE)
[![Streamer.bot](https://img.shields.io/badge/Streamer.bot-0.2.0%2B-9147ff.svg)](https://streamer.bot)
[![OBS Studio](https://img.shields.io/badge/OBS_Studio-29.0%2B-222222.svg)](https://obsproject.com)

Système de progression XP pour streamers Twitch. Récompensez vos viewers avec des niveaux, un leaderboard animé et des cartes de profil — sans serveur, sans base de données, 100% local.

---

## Aperçu

<!-- TODO: Screenshot leaderboard (thème default) -->
<!-- TODO: GIF animation entrée/sortie profile card -->
<!-- TODO: Grille des 6 thèmes disponibles -->

---

## Fonctionnalités

| Fonctionnalité | Description |
|---|---|
| XP automatique | Chaque message valide en chat rapporte de l'XP |
| Niveaux | Progression exponentielle — formule `100 × level^1.5` |
| Leaderboard Top 10 | Podium animé (rangs 1-3) + classement (rangs 4-10) |
| Carte de profil | Déclenchée par Channel Point Twitch |
| 6 thèmes visuels | Default, RPG, Cyber, Minimal, Tokyo, Sakura |
| Anti-spam | Cooldown configurable + filtre de commandes |
| Stockage local | Fichiers JSON — aucun serveur, aucune base de données |
| Temps réel | Communication WebSocket entre Streamer.bot et OBS |

---

## Installation rapide

> Guide complet : [docs/INSTALLATION.md](docs/INSTALLATION.md) — environ 15 minutes

**Prérequis :** Streamer.bot 0.2.0+ · OBS Studio 29.0+ · Windows

1. Télécharger et extraire dans un dossier **permanent** (ex: `C:\StreamerTools\streamerbot-xp-system`)
2. Modifier `configs/config.json` — renseigner `dataPath` avec le chemin absolu vers `data\users\`
3. Dans Streamer.bot : créer la variable globale `xp_configPath`, importer les 4 actions C#, activer le serveur WebSocket
4. Dans OBS : ajouter deux Browser Sources pointant vers les fichiers HTML des overlays

---

## Documentation

| Document | Description |
|---|---|
| [Installation](docs/INSTALLATION.md) | Guide pas-à-pas complet |
| [Configuration](docs/CONFIGURATION.md) | Référence complète de config.json |
| [Thèmes](docs/THEMES.md) | Changer de thème et créer un thème personnalisé |
| [Troubleshooting](docs/TROUBLESHOOTING.md) | Résoudre les problèmes courants |
| [Architecture](docs/ARCHITECTURE.md) | Schémas techniques et flux de données |
| [Développeur](docs/DEVELOPER.md) | Contribuer, étendre et maintenir le système |

---

## Structure du projet

```
streamerbot-xp-system/
├── actions/           Scripts C# à importer dans Streamer.bot
├── scripts/           Services de référence (logique métier)
├── overlays/          Overlays HTML/CSS/JS pour OBS
│   ├── leaderboard/   Podium Top 10 animé
│   └── card/          Carte de profil viewer
├── configs/           config.json — paramètres centralisés
├── data/users/        Profils viewers — créé automatiquement
└── docs/              Documentation complète
```

---

## Roadmap

**V1 — Disponible**
XP par message · Niveaux · Leaderboard Top 10 · Carte de profil · 6 thèmes · Anti-spam

**V2 — Prochainement**
XP watchtime · Commande `!rank` · Récompenses avancées

**V3 — Futur**
Export web · Template site streamer personnel

**V4 — Vision**
Succès · Quêtes · Saisons · Marketplace de thèmes

---

## Licence

[MIT](LICENSE)
