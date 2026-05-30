# Changelog

Toutes les modifications notables de ce projet sont documentées ici.

Format basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/).

---

## [1.0.0] — 2025-05-30

### Ajouté

**Système XP**
- Attribution d'XP par message chat valide
- Validation anti-spam : cooldown configurable, longueur minimum, filtre de commandes
- Formule de niveaux progressive : `100 × level^1.5`
- Détection automatique de level up avec log Streamer.bot

**Leaderboard**
- Top 10 viewers par XP
- Podium animé (rangs 1-3) avec couronne, badges et colonnes
- Classement animé (rangs 4-10) avec reveal staggeré
- Mise à jour automatique via Timer Streamer.bot

**Carte de profil**
- Affichage déclenché par Channel Point Twitch
- Affiche : pseudo, niveau, barre XP, rang en temps réel
- Animation entrée/sortie (slide depuis la gauche)
- Disparition automatique après 8 secondes

**Thèmes visuels**
- 6 thèmes disponibles : Default, RPG, Cyber, Minimal, Tokyo, Sakura
- Système de variables CSS par thème
- Thèmes séparés par overlay (leaderboard + card)
- Changement à chaud via `window.setTheme()`

**Architecture technique**
- 4 actions Streamer.bot : USER_GetOrCreate, XP_Add, LEADERBOARD_Update, CARD_ShowProfile
- 5 services C# : ConfigService, UserRepository, ValidationService, XpService, OBSService
- Communication WebSocket temps réel (ws://127.0.0.1:8080)
- Stockage local JSON par viewer — aucun serveur requis
- Animations GPU exclusivement (transform + opacity)

**Documentation**
- README principal avec badges et roadmap
- Guide d'installation pas-à-pas (< 15 minutes)
- Référence complète de config.json
- Guide des thèmes et création de thème personnalisé
- Guide de troubleshooting (7 problèmes couverts)
- Documentation architecture avec schémas ASCII
- Guide développeur avec pipelines et conventions

---

## À venir

### [1.1.0] — V2
- XP watchtime (minutes de présence en stream)
- Commande `!rank` dans le chat
- Récompenses Twitch avancées

### [2.0.0] — V3
- Export web des données
- Template site streamer personnel

### [3.0.0] — V4
- Succès et badges
- Quêtes
- Saisons avec remise à zéro
- Marketplace de thèmes
