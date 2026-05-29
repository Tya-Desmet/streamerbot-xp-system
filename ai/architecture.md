# Architecture Agent

## Project

Streamer.bot XP System

Projet modulaire de gamification Twitch utilisant :

* Streamer.bot
* C#
* JSON local
* OBS Overlays HTML/CSS/JS

---

# Global Goals

Construire un système :

* modulaire
* maintenable
* scalable
* IA-friendly
* simple pour streamers débutants

---

# Architecture Rules

## IMPORTANT

Une responsabilité logique par :

* action
* service
* module

---

# Forbidden

## NEVER

* créer des scripts géants
* mélanger frontend et backend
* mélanger OBS et logique métier
* ajouter des dépendances circulaires
* dupliquer logique XP
* refactor global sans demande

---

# Maximum File Size

Maximum recommandé :

* 300 lignes par fichier

---

# Naming Conventions

## Actions

TWITCH_MessageReceived
XP_Add
XP_ValidateMessage
USER_Save
LEADERBOARD_Update
USER_LevelUp

---

## Services

XpService
LeaderboardService
UserRepository
BackupService

---

# Core Principles

## XP Centralization

Toute modification XP DOIT passer par :

XP_Add

---

## Separation

Le backend :

* ne gère pas OBS
* ne gère pas les animations

Les overlays :

* ne gèrent pas les calculs XP
* ne gèrent pas les sauvegardes

---

# Project Structure

/actions
/scripts
/overlays
/themes
/modules
/config
/data
/export
/docs

---

# Workflow Rules

Toujours :

1. Expliquer architecture
2. Générer petit module
3. Tester
4. Commit Git

---

# Never Do

NEVER :

* “fais tout le projet”
* “refactor toute l’architecture”
* “ajoute plein de fonctionnalités”

---

# Expected Behavior

Toujours :

* privilégier simplicité
* privilégier lisibilité
* expliquer choix techniques
* proposer architecture claire
* découper les responsabilités

---

# Development Philosophy

Petit module
→ testé
→ validé
→ commit

Jamais :
gros système généré d’un coup
