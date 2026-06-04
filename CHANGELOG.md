# Changelog

Toutes les modifications notables de ce projet sont documentées ici.

Format basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/).

---

## [3.10.0] — 2026-06-04

### Ajouté
- **Sections optionnelles (feature flags)** dans `website/content/site.json` →
  `features` : `planning`, `ressources`, `friends`, `socials`. Flag absent = activé
  (défaut, non-régression). Le cœur (classement + profils viewer) reste toujours présent.
- Pages désactivées → `notFound()` + lien retiré de la NavBar ; sections d'accueil non rendues.
- **Podium de l'accueil en live** (`LivePodium`) : polling `/api/leaderboard` comme la
  page classement et l'embed (avant : figé au build).
- **Page 404 personnalisée** (`NotFoundView` + `app/not-found.tsx`) thémée et navigable.
- `docs/TEMPLATE.md` — guide de réutilisation du hub comme template.

### Modifié
- `lib/content.ts` — type `SiteFeatures` + helper `isFeatureOn()`.

---

## [3.9.0] — 2026-06-04

### Ajouté
- **Carte viewer : total de check-in** — champ public additif `totalCheckIns`
  (`schemaVersion` inchangé) ; la « série watchtime » n'est plus affichée (`watchStreak`
  conservé dans l'export).
- **Identité & SEO pilotés par config** — `content/site.json` étendu (`siteName`,
  `tagline`, `description`, `keywords`, `alternateNames`, `ogImage`). Plus aucun
  « Mystya » en dur dans le code (fallback neutre). Non-régression vérifiée (diff de build vide).
- **Leaderboard intégrable (iframe)** — route SSG `/embed/leaderboard`, classement seul
  (sans lien profil), polling live + fallback, paramétrable `theme/limit/bg/accent/title`.
  Doc `docs/EMBED.md`.

### Modifié
- **Route groups** : hub déplacé sous `app/(hub)/`, embed isolé sous `app/(embed)/`
  (layout racine minimal, sans NavBar/Footer/JSON-LD). URLs inchangées.
- Contrat d'export + `ExportService` + `EXPORT_Snapshot` régénéré (totalCheckIns).

---

## [3.8.0] — 2026-06

### Ajouté
- **Pipeline push live** : `EXPORT_Snapshot` → `push-to-backend.ps1` (tâche horaire) →
  backend Node `/api/push` → site (classement + profils viewer à jour sans rebuild).
- Configuration de déploiement externalisée (`tools/deploy.local.ps1`, gitignoré).

> Versions V3.0–3.7 : mise en place du hub web (export fichier, site Next.js statique,
> classement + profils + planning + ressources). Voir `README.md` pour l'état des versions.

---

## [2.6.0] — 2026-06-02

### Ajouté

**Récompenses Channel Points (REWARD_BonusXp / REWARD_GrantXp)**
- `REWARD_BonusXp` — active un multiplicateur d'XP temporaire (configurable : facteur × durée en minutes) via Channel Point. Affiché en badge ⚡ sur la carte de profil.
- `REWARD_GrantXp` — octroie un montant fixe d'XP à un viewer via Channel Point.
- `RewardService` — service dédié au cycle de vie du multiplicateur (activation, lecture, expiration paresseuse). Aucune écriture disque directe.

**Check-in quotidien (DAILY_CheckIn)**
- Carte de fidélité à 10 cases : 1 case/jour par Channel Point "Check-in".
- XP par case (`xpPerCheckin`) + bonus carte complète (`xpCardComplete`).
- Anti-rebond quotidien — refus silencieux si déjà fait aujourd'hui.
- `TotalCheckIns` exposé pour les futures mécaniques (coffres V4, etc.).
- Overlay check-in animé déclenché à chaque validation.

**Overlay check-in**
- Nouveau overlay `overlays/checkin/` avec 6 thèmes (default, rpg, cyber, minimal, tokyo, sakura + myastya-samourai).
- Animation entrée spring-pop, icône ✓ stamp, cases échelonnées, état carte complète.

**Badge bonus XP sur la carte de profil**
- Badge `⚡ ×2 · 30 min` visible sur la card quand un multiplicateur est actif.
- Masqué automatiquement quand le bonus est expiré.

**Thème myastya-samourai (card / leaderboard / check-in)**
- Palette nuit indigo profonde + magenta cerisier, polices `Shippori Mincho` + `Noto Sans JP`.
- Animations contextuelles (pas de fond permanent) :
  - Card : slash samourai lumineux au `cardIn` + burst d'étincelles.
  - Check-in : grand pétale SVG qui se pose à l'apparition, bloom par case cochée, burst sur carte complète.
  - Leaderboard : burst au reveal, pétale prize sur le n°1, micro-bursts sur les rangs.
- Effets sonores : synthèse Web Audio par défaut, remplaçables par fichiers via `sounds.json`.
- Actif par défaut (`?mute` pour couper) ; aucun `requestAnimationFrame` hors animation.

### Modifié

- `config.json` — nouvelles sections `rewards` et `checkIn` avec valeurs par défaut.
- `ConfigService` — parsing des sections rewards et checkIn.
- `UserRepository` / `UserProfile` — champs `BonusExpiryTimestamp`, `ActiveBonusMultiplier`, `LastCheckInDate`, `CheckInCount`, `TotalCheckIns`.
- `XpService` — applique le multiplicateur actif lors du calcul XP.
- `CARD_ShowProfile` — envoie `bonusActive`, `bonusMultiplier`, `bonusMinutesLeft` à l'overlay.
- Actions générées (`actions/generated/`) — reconstruites via `build-actions.ps1`.
- `docs/CONFIGURATION.md` — sections `rewards` et `checkIn` documentées.
- `docs/DEVELOPER.md` — nouvelles actions et `RewardService` listés.

### Supprimé

- `PLAN_REMEDIATION.md` — document de planification interne, remédiation P01-P15 terminée.

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
