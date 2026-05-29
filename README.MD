    # README 1 — Streamer.bot XP System

## README Principal

# Présentation

Streamer.bot XP System est un système de gamification Twitch modulaire basé sur Streamer.bot.

Le projet permet à un streamer d’ajouter à son stream :

* système XP
* niveaux
* leaderboard
* overlays OBS
* profile cards
* rewards Twitch interactives
* export web optionnel

Le projet est conçu pour :

* fonctionner entièrement en local
* être facilement installable
* être maintenable avec IA
* être extensible
* être modulaire
* être utilisable par des streamers débutants

---

# Philosophie du projet

Le projet n’est PAS :

* une plateforme SaaS
* un service cloud global
* une base de données centralisée
* un système multi-streamers partagé

Chaque streamer possède :

* ses données
* ses viewers
* ses overlays
* son leaderboard
* son export web

---

# Technologies utilisées

## Core

* Streamer.bot
* C#
* JSON local
* HTML/CSS/JavaScript
* OBS Studio

---

# Architecture générale

```text
Twitch
↓
Streamer.bot
   ├── Events
   ├── Commands
   ├── Timers
   ├── Rewards
   ├── C# Scripts
   └── Local JSON Database
            ├── OBS Overlays
            ├── Backups
            └── Optional Web Export
```

---

# Objectifs principaux

# V1

## Fonctionnalités MVP

* XP via chat
* XP via watchtime
* système de niveaux
* anti-spam
* leaderboard
* commande !rank
* overlay OBS
* sauvegardes JSON

---

# V2

* profile cards
* thèmes overlays
* rewards Twitch avancées
* animations améliorées

---

# V3

* export web
* website template
* profils publics
* statistiques stream

---

# V4

* achievements
* quêtes
* saisons
* marketplace thèmes
* statistiques avancées

---

# Architecture dossiers

```text
/actions
/scripts
/config
/data
/export
/backup
/themes
/overlays
/modules
/docs
```

---

# Structure des données

## Utilisateurs

```text
/data/users/{username}.json
```

---

# Exemple utilisateur

```json
{
  "username": "Mystya",
  "xp": 1450,
  "level": 12,
  "messages": 592,
  "watchTime": 320,
  "rank": 4,
  "lastMessageTimestamp": 174843999
}
```

---

# Conventions importantes

# RÈGLE 1

Toute modification XP DOIT passer par :

```csharp
AddXp(user, amount, reason)
```

---

# RÈGLE 2

Un fichier = une responsabilité.

---

# RÈGLE 3

Pas de scripts géants.

Maximum recommandé :

```text
300 lignes
```

---

# RÈGLE 4

Toutes les actions doivent suivre une convention claire.

## Exemples

```text
XP_Add
XP_LevelUp
USER_GetRank
OVERLAY_ShowLeaderboard
CARD_ShowProfile
```

---

# Overlay OBS

Les overlays utilisent des Browser Sources OBS.

## Exemples

```text
leaderboard.html
card.html
top3.html
```

---

# Leaderboard

## Modes

### Popup automatique

Toutes les 10 minutes.

### Overlay permanent

Affichage Top 3.

---

# Système de niveaux

## Formule

XP nécessaire :

```text
100 × level^1.5
```

---

# Anti-spam

## Conditions minimales

* cooldown utilisateur
* minimum caractères
* ignore commandes
* ignore répétitions

---

# Sauvegardes

Le système doit effectuer des backups automatiques.

## Structure

```text
/backup
```

---

# Export Web

## IMPORTANT

Le module web est OPTIONNEL.

Aucune donnée globale.
Aucun classement mondial.

Chaque streamer exporte uniquement ses données.

---

# Export JSON

## Exemples

```text
/export/leaderboard.json
/export/stats.json
/export/users/*.json
```

---

# Website Template

Le site web lit uniquement les fichiers exportés.

Stack recommandée :

* Next.js
* TailwindCSS

---

# Installation utilisateur

## Workflow cible

```text
1. Installer Streamer.bot
2. Importer le pack
3. Ajouter overlays OBS
4. Configurer rewards Twitch
5. Start
```

---

# Configuration globale

## config.json

```json
{
  "xpPerMessage": 5,
  "xpPerWatch": 15,
  "watchIntervalMinutes": 5,
  "leaderboardIntervalMinutes": 10,
  "theme": "default",
  "enableTop3Overlay": true,
  "enableWebExport": false
}
```

---

# Documentation obligatoire

```text
/docs
   ARCHITECTURE.md
   EVENTS.md
   DATA.md
   OVERLAYS.md
   THEMES.md
   INSTALLATION.md
```

---

# Contraintes IA

Le projet sera principalement développé avec IA.

Donc :

* petits fichiers
* conventions strictes
* documentation permanente
* responsabilités isolées
* aucun code dupliqué

---

