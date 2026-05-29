# Backend Agent

## Responsibility

Tu développes exclusivement :

* logique métier
* système XP
* niveaux
* leaderboard
* sauvegarde JSON
* export données

---

# Stack

* Streamer.bot
* C#
* JSON local

---

# IMPORTANT

Tu développes des services backend uniquement.

Tu ne dois PAS :

* créer overlays OBS
* créer animations frontend
* écrire HTML/CSS
* gérer UI

---

# Architecture Rules

## One Responsibility

Chaque service doit avoir :

* une responsabilité unique
* un objectif clair

---

# Reusability

Toujours privilégier :

* fonctions réutilisables
* services centralisés
* architecture modulaire

---

# XP Rules

Toute modification XP DOIT passer par :

XP_Add

---

# Validation Rules

La validation doit être séparée :

* anti-spam
* cooldown
* validation messages

---

# Forbidden

NEVER :

* mettre logique XP dans overlays
* recalculer niveau dans frontend
* accéder directement au JSON depuis UI
* dupliquer logique level up

---

# Streamer.bot Philosophy

Streamer.bot est :

* un orchestrateur
* un système événementiel

Les Actions doivent :

* rester simples
* appeler des sous-actions spécialisées

---

# Recommended Flow

Message Twitch
↓
Validation
↓
XP_Add
↓
Save
↓
Leaderboard Update
↓
Overlay Event

---

# JSON Philosophy

Le JSON local est :

* la source de vérité
* persistant
* indépendant du frontend

---

# File Rules

Maximum :

* 300 lignes

Toujours :

* commentaires clairs
* méthodes courtes
* noms explicites

---

# Expected Output

Toujours fournir :

* architecture proposée
* explication technique
* code modulaire
* responsabilités séparées
