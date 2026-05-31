# V2 Architecture Plan — Streamer.bot XP System

> Agents : ARCHITECTURE_AGENT · SERVICE_CSHARP_AGENT · DOCUMENTATION_AGENT
> Statut : Plan d'implémentation pré-code
> Date : 2026-05-31

---

## Résumé exécutif

La V2 introduit trois fonctionnalités majeures sur une base V1 solide :

| Fonctionnalité | Complexité | Impact |
|---|---|---|
| XP Watchtime | Moyenne | Nouveau timer, nouveau service, nouveaux champs |
| Commande !rank | Faible | Nouvelle action, chat output |
| Récompenses avancées | Haute | Nouveau service, nouveaux champs, XP_Add modifié |

---

## 1. Nouveaux Services requis

### 1.1 WatchTimeService.cs

**Responsabilité unique :** Calculer et distribuer l'XP de watchtime.

**Méthodes :**

```
IsEligibleForWatchXp(UserProfile user, long now)  →  bool
    — Vérifie que le user n'a pas déjà reçu de watchXP dans cet intervalle

CalculateWatchXp(Config config, UserProfile user)  →  int
    — Retourne xpPerWatch × bonusMultiplier actif

BuildWatchTimeResult(UserProfile user, int xpAdded)  →  WatchTimeResult
    — DTO de résultat pour logging/debug
```

**Modèles :**

```
WatchTimeResult {
    Username         : string
    XpAwarded        : int
    WasEligible      : bool
    SkipReason       : string  // vide si eligible
}
```

**Contrainte :** Aucune écriture JSON. Délègue la persistance à UserRepository.

---

### 1.2 RankService.cs

**Responsabilité unique :** Calcul du rang live et formatage du message !rank.

**Méthodes :**

```
GetLiveRank(string username, List<UserProfile> allUsers)  →  int
    — Tri par XP décroissant, retourne position 1-based

FormatRankMessage(UserProfile user, int rank, Config config)  →  string
    — Retourne le texte à envoyer dans le chat
    — Exemple : "@Mystya — Rang #4 | Niveau 12 | 1450 XP"

GetRankTitle(int level)  →  string  // optionnel V2
    — Titre associé au niveau (Novice, Régulier, Vétéran, Légende)
```

**Note :** La logique de rang live existe déjà dans `CARD_ShowProfile.cs`. RankService extrait cette logique en service réutilisable partagé par la carte et la commande.

---

### 1.3 RewardService.cs

**Responsabilité unique :** Gérer les bonus actifs et les récompenses directes.

**Méthodes :**

```
ApplyBonusMultiplier(UserProfile user, float multiplier, int durationMinutes, long now)
    →  BonusResult
    — Écrit BonusMultiplier et BonusExpiryTimestamp sur le profil

GetCurrentMultiplier(UserProfile user, long now)  →  float
    — Retourne 1.0 si pas de bonus actif ou expiré

IsBonusActive(UserProfile user, long now)  →  bool
    — Vérifie BonusExpiryTimestamp > now

ExpireBonus(UserProfile user)  →  void
    — Remet Multiplier à 1.0, Expiry à 0
```

**Modèles :**

```
BonusResult {
    Username         : string
    MultiplierApplied: float
    ExpiresAt        : long   // Unix timestamp
    WasAlreadyActive : bool   // vrai si bonus remplacé
}
```

---

## 2. Nouvelles Actions Streamer.bot

### 2.1 XP_WatchTime.cs

| Propriété | Valeur |
|---|---|
| Déclencheur | Timer — intervalle configurable (`watchIntervalMinutes`) |
| Responsabilité | Distribuer l'XP watchtime à tous les viewers actifs |
| Lit | `CPH.GetActiveViewers()` |
| Écrit | UserProfile via UserRepository |
| Overlay | Aucun (action silencieuse) |
| Level-up | Délègue à XP_Add (sub-action) ou détecte en interne |

**Variables exposées en sortie :**

```
%watch_usersProcessed%   — nombre de viewers traités
%watch_xpDistributed%    — XP total distribué ce cycle
%watch_levelUps%         — nombre de level-ups ce cycle
```

**Flux interne :**

```
Timer fired
  → CPH.GetActiveViewers()
  → Pour chaque viewer :
      → USER_GetOrCreate (sub-action)
      → WatchTimeService.IsEligibleForWatchXp()
      → Si eligible :
          → WatchTimeService.CalculateWatchXp()  (intègre bonus actif)
          → XpService.AddXp(user, amount, "watchtime")
          → Incrémenter user.WatchTime
          → Mettre à jour user.LastWatchTimestamp
          → UserRepository.SaveUser()
          → Détecter level-up → exposer %xp_isLevelUp%
```

---

### 2.2 RANK_ShowCommand.cs

| Propriété | Valeur |
|---|---|
| Déclencheur | Chat Command `!rank` |
| Responsabilité | Afficher le rang du viewer dans le chat |
| Lit | Tous les profils (pour rang live) |
| Écrit | Rien |
| Output | `CPH.SendMessage()` |

**Variables exposées en sortie :**

```
%rank_username%     — username du viewer
%rank_position%     — rang live (int)
%rank_level%        — niveau actuel
%rank_xp%           — XP total
%rank_message%      — texte complet envoyé au chat
%rank_skipped%      — true si cooldown actif
```

**Flux interne :**

```
Chat !rank
  → Vérifier cooldown commande (RankCommandCooldownSeconds)
  → USER_GetOrCreate
  → UserRepository.GetAllUsers()
  → RankService.GetLiveRank()
  → RankService.FormatRankMessage()
  → CPH.SendMessage(rankMessage)
```

---

### 2.3 REWARD_BonusXp.cs

| Propriété | Valeur |
|---|---|
| Déclencheur | Twitch Channel Point Redemption (`rewardDoubleXpName`) |
| Responsabilité | Activer le multiplicateur XP temporaire |
| Lit | UserProfile |
| Écrit | UserProfile (BonusMultiplier, BonusExpiry) |
| Output | `CPH.SendMessage()` confirmation |

**Variables exposées en sortie :**

```
%reward_username%       — viewer qui a activé
%reward_bonusActive%    — true si bonus appliqué
%reward_multiplier%     — multiplicateur (ex: 2.0)
%reward_expiresIn%      — minutes restantes
%reward_message%        — message de confirmation chat
```

**Flux interne :**

```
Channel Point "Double XP" redeemed
  → USER_GetOrCreate
  → RewardService.IsBonusActive()
  → RewardService.ApplyBonusMultiplier(user, 2.0, config.DoubleXpDuration)
  → UserRepository.SaveUser()
  → CPH.SendMessage("@{user} — Double XP activé pour 30 minutes !")
```

---

### 2.4 REWARD_GrantXp.cs

| Propriété | Valeur |
|---|---|
| Déclencheur | Twitch Channel Point Redemption (`rewardGrantXpName`) |
| Responsabilité | Octroyer un montant d'XP fixe via reward |
| Lit | UserProfile |
| Écrit | UserProfile (XP + source XpFromRewards) |
| Output | `CPH.SendMessage()` confirmation |

**Variables exposées en sortie :**

```
%reward_username%   — viewer concerné
%reward_xpGranted%  — XP octroyé
%reward_newLevel%   — niveau après octroi
%reward_isLevelUp%  — true si level-up déclenché
```

**Flux interne :**

```
Channel Point "Bonus XP" redeemed
  → USER_GetOrCreate
  → XpService.AddXp(user, config.RewardGrantXpAmount, "reward")
  → Détecter level-up
  → UserRepository.SaveUser()
  → CPH.SendMessage("@{user} — +{amount} XP reçus !")
```

---

## 3. Nouveaux champs UserProfile

**Champs existants V1 (inchangés) :**

```json
"Username"             : string
"DisplayName"          : string
"Xp"                   : int
"Level"                : int
"Messages"             : int
"WatchTime"            : int   (existe mais = 0 en V1)
"Rank"                 : int
"LastMessageTimestamp" : long
```

**Nouveaux champs V2 :**

```json
"XpFromChat"              : int    // XP source : messages chat
"XpFromWatch"             : int    // XP source : watchtime
"XpFromRewards"           : int    // XP source : channel point rewards
"LastWatchTimestamp"      : long   // Unix — dernier cycle watchtime reçu
"ActiveBonusMultiplier"   : float  // 1.0 si aucun bonus actif
"BonusExpiryTimestamp"    : long   // 0 si aucun bonus actif
```

**Migration :** Les profils V1 existants fonctionneront sans modification. Les nouveaux champs se créent à la première mise à jour. `XpService.AddXp()` doit répartir l'XP dans la source correspondante via le paramètre `reason`.

**Exemple profil V2 complet :**

```json
{
  "Username": "mystya",
  "DisplayName": "Mystya",
  "Xp": 1800,
  "Level": 13,
  "Messages": 620,
  "WatchTime": 480,
  "Rank": 3,
  "LastMessageTimestamp": 174843999,
  "XpFromChat": 1300,
  "XpFromWatch": 400,
  "XpFromRewards": 100,
  "LastWatchTimestamp": 174844500,
  "ActiveBonusMultiplier": 2.0,
  "BonusExpiryTimestamp": 174846300
}
```

---

## 4. Modifications XpService.cs

### 4.1 Signature AddXp mise à jour

```
// V1
AddXp(username, amount)  →  XpResult

// V2
AddXp(user, amount, reason, multiplier)  →  XpResult
    reason     : "chat" | "watchtime" | "reward"
    multiplier : float (1.0 par défaut, ignoré si reason="reward")
```

**Comportement ajouté :**
- Incrémenter `XpFromChat` si reason = "chat"
- Incrémenter `XpFromWatch` si reason = "watchtime"
- Incrémenter `XpFromRewards` si reason = "reward"
- Appliquer le multiplier avant d'ajouter (sauf rewards)

### 4.2 Compatibilité ascendante

Les anciens appels `AddXp(user, amount, "chat")` fonctionnent. Le multiplier est optionnel (défaut 1.0).

---

## 5. Impact sur le Leaderboard

### 5.1 Fréquence de mise à jour

Le watchtime distribue de l'XP à de nombreux utilisateurs simultanément. Le timer du leaderboard doit s'aligner ou être légèrement décalé par rapport au timer watchtime pour éviter les lectures en cours d'écriture.

**Recommandation :** WatchTime timer = 5 min. Leaderboard timer = 5 min + 30 secondes de décalage.

### 5.2 Affichage optionnel de la source XP

Le leaderboard actuel affiche uniquement l'XP total. En V2, la payload peut inclure optionnellement les sources :

```json
// Payload leaderboard V2 (extension compatible)
{
  "rank": 1,
  "username": "Mystya",
  "displayName": "Mystya",
  "xp": 1800,
  "level": 13,
  "xpFromChat": 1300,      // nouveau
  "xpFromWatch": 400,      // nouveau
  "xpFromRewards": 100     // nouveau
}
```

L'overlay leaderboard.js peut ignorer ces champs si non utilisés — compatibilité descendante garantie.

### 5.3 Classement non affecté

L'XP total reste l'unique critère de tri. Les sources servent à l'affichage informatif uniquement.

---

## 6. Impact sur la Profile Card

### 6.1 Nouveaux champs à afficher

La card V2 peut afficher :

```
Niveau 13
XP : 1800 / 2100
#3 au classement
Messages : 620  |  Watch : 8h00
Bonus actif : ×2 (encore 22 min)   ← si bonus actif
```

### 6.2 Payload CardPayload V2

```json
{
  "username": "mystya",
  "displayName": "Mystya",
  "level": 13,
  "xp": 1800,
  "xpIntoLevel": 682,
  "xpForNext": 900,
  "percentage": 75,
  "rank": 3,
  "messages": 620,
  "watchTimeMinutes": 480,
  "xpFromChat": 1300,
  "xpFromWatch": 400,
  "xpFromRewards": 100,
  "bonusActive": true,
  "bonusMultiplier": 2.0,
  "bonusExpiresInMinutes": 22
}
```

### 6.3 Overlay card.js — modifications minimales

Les nouveaux champs sont additifs. La card V1 continue de fonctionner si elle ignore les champs inconnus. L'overlay V2 lit conditionnellement les champs bonus et watchtime.

---

## 7. Modifications config.json

```json
{
  // ── EXISTANT (inchangés) ──────────────────────────────────
  "dataPath": "C:\\Stream\\streamerbot-xp-system\\data\\users",
  "xpPerMessage": 10,
  "xpPerWatch": 1,
  "cooldownSeconds": 30,
  "minMessageLength": 2,
  "leaderboardIntervalMinutes": 5,
  "obsLeaderboardSource": "Leaderboard",
  "obsCardSource": "ProfileCard",

  // ── NOUVEAU V2 — Watchtime ────────────────────────────────
  "watchIntervalMinutes": 5,

  // ── NOUVEAU V2 — Commande !rank ───────────────────────────
  "rankCommandEnabled": true,
  "rankCommandCooldownSeconds": 30,
  "rankMessageTemplate": "@{username} — Rang #{rank} | Niv.{level} | {xp} XP",

  // ── NOUVEAU V2 — Récompenses ──────────────────────────────
  "enableRewards": true,
  "rewardDoubleXpName": "Double XP",
  "rewardDoubleXpMultiplier": 2.0,
  "rewardDoubleXpDurationMinutes": 30,
  "rewardGrantXpName": "Bonus XP",
  "rewardGrantXpAmount": 100
}
```

**Règle de compatibilité :** ConfigService.cs doit retourner des valeurs par défaut sensées pour chaque nouveau champ si absent du fichier JSON. Les streamers V1 upgradant vers V2 n'ont pas à modifier leur config si les valeurs par défaut conviennent.

---

## 8. Diagrammes de flux

### 8.1 Flux XP Watchtime

```
Timer (toutes les watchIntervalMinutes)
│
├── CPH.GetActiveViewers()
│       └── [viewer1, viewer2, viewer3, ...]
│
├── Pour chaque viewer :
│   │
│   ├── USER_GetOrCreate(viewer)
│   │       └── UserProfile chargé ou créé
│   │
│   ├── WatchTimeService.IsEligibleForWatchXp(user, now)
│   │       ├── NON → skip (log raison)
│   │       └── OUI →
│   │               ├── RewardService.GetCurrentMultiplier(user, now)
│   │               ├── WatchTimeService.CalculateWatchXp(config, multiplier)
│   │               ├── XpService.AddXp(user, amount, "watchtime", multiplier)
│   │               │       ├── user.Xp += amount
│   │               │       ├── user.XpFromWatch += amount
│   │               │       ├── Recalculer user.Level
│   │               │       └── → XpResult {isLevelUp, newLevel}
│   │               ├── user.WatchTime += watchIntervalMinutes
│   │               ├── user.LastWatchTimestamp = now
│   │               └── UserRepository.SaveUser(user)
│   │
│   └── Si XpResult.IsLevelUp → exposer %xp_isLevelUp% (pour alertes futures)
│
└── Exposer %watch_usersProcessed%, %watch_xpDistributed%
```

---

### 8.2 Flux Commande !rank

```
Chat : "!rank"
│
├── Vérifier cooldown commande (RankCommandCooldownSeconds)
│       ├── En cooldown → ignorer silencieusement
│       └── OK →
│               ├── USER_GetOrCreate(user)
│               ├── UserRepository.GetAllUsers()
│               ├── RankService.GetLiveRank(username, allUsers)
│               ├── RankService.FormatRankMessage(user, rank, config)
│               └── CPH.SendMessage(rankMessage)
│                       └── "@Mystya — Rang #4 | Niv.12 | 1450 XP"
│
└── SetCooldown(username, RankCommandCooldownSeconds)
```

---

### 8.3 Flux Récompense Double XP

```
Channel Point "Double XP" redeemed by viewer
│
├── USER_GetOrCreate(viewer)
│
├── RewardService.IsBonusActive(user, now)
│       ├── OUI → Notifier "bonus déjà actif, prolongé"
│       └── NON ou OUI (selon config) →
│               ├── RewardService.ApplyBonusMultiplier(
│               │       user, config.DoubleXpMultiplier,
│               │       config.DoubleXpDurationMinutes, now)
│               ├── user.ActiveBonusMultiplier = 2.0
│               ├── user.BonusExpiryTimestamp = now + duration
│               └── UserRepository.SaveUser(user)
│
└── CPH.SendMessage("@Mystya — Double XP activé pour 30 min !")
```

---

### 8.4 Flux XP_Add avec bonus (modification V2)

```
Message chat valide reçu
│
├── ValidationService.ValidateMessage()  (inchangé)
│
├── USER_GetOrCreate()  (inchangé)
│
├── RewardService.GetCurrentMultiplier(user, now)  ← NOUVEAU
│       ├── Bonus expiré → ExpireBonus(user) → retourner 1.0
│       └── Bonus actif → retourner BonusMultiplier (ex: 2.0)
│
├── XpService.AddXp(user, config.XpPerMessage, "chat", multiplier)  ← MODIFIÉ
│       ├── effectiveXp = XpPerMessage × multiplier
│       ├── user.Xp += effectiveXp
│       ├── user.XpFromChat += effectiveXp  ← NOUVEAU
│       ├── Recalculer level
│       └── → XpResult
│
└── (suite inchangée : save, detect level-up, overlay)
```

---

### 8.5 Vue d'ensemble des déclencheurs V2

```
Twitch Events
│
├── Chat Message ────────────────────────→ XP_Add.cs
│                                              └── WatchTimeService (bonus check)
│
├── Chat Command "!rank" ────────────────→ RANK_ShowCommand.cs
│                                              └── RankService
│
├── Channel Point "Double XP" ──────────→ REWARD_BonusXp.cs
│                                              └── RewardService
│
├── Channel Point "Bonus XP" ───────────→ REWARD_GrantXp.cs
│                                              └── XpService
│
└── Channel Point "Voir ma carte" ──────→ CARD_ShowProfile.cs  (V1, enrichi V2)

Timers
│
├── watchIntervalMinutes ───────────────→ XP_WatchTime.cs
│                                              └── WatchTimeService
│
└── leaderboardIntervalMinutes ─────────→ LEADERBOARD_Update.cs  (V1, inchangé)
```

---

## 9. Roadmap technique V2

### Phase 1 — Fondations (sans casser V1)

Prérequis de tout le reste. Doit être fait en premier et validé avant de continuer.

| # | Tâche | Fichier(s) concerné(s) |
|---|---|---|
| 1.1 | Étendre UserProfile avec les 6 nouveaux champs | scripts/UserRepository.cs |
| 1.2 | Mettre à jour XpService.AddXp() — ajouter `reason` et `multiplier` | scripts/XpService.cs |
| 1.3 | Mettre à jour config.json avec les nouveaux paramètres | configs/config.json |
| 1.4 | Mettre à jour ConfigService.cs avec les nouvelles valeurs et défauts | scripts/ConfigService.cs |

**Critère de validation Phase 1 :** Les 4 actions V1 fonctionnent à l'identique après ces modifications.

---

### Phase 2 — XP Watchtime

| # | Tâche | Fichier(s) concerné(s) |
|---|---|---|
| 2.1 | Créer WatchTimeService.cs | scripts/WatchTimeService.cs |
| 2.2 | Créer l'action XP_WatchTime.cs | actions/XP_WatchTime.cs |
| 2.3 | Configurer le Timer Streamer.bot | Streamer.bot UI |

**Critère de validation Phase 2 :** Les viewers actifs reçoivent de l'XP watchtime toutes les N minutes. `XpFromWatch` s'incrémente.

---

### Phase 3 — Commande !rank

| # | Tâche | Fichier(s) concerné(s) |
|---|---|---|
| 3.1 | Créer RankService.cs | scripts/RankService.cs |
| 3.2 | Créer l'action RANK_ShowCommand.cs | actions/RANK_ShowCommand.cs |
| 3.3 | Configurer la commande !rank Streamer.bot | Streamer.bot UI |

**Critère de validation Phase 3 :** `!rank` en chat retourne le rang correct dans les 2 secondes.

---

### Phase 4 — Récompenses avancées

| # | Tâche | Fichier(s) concerné(s) |
|---|---|---|
| 4.1 | Créer RewardService.cs | scripts/RewardService.cs |
| 4.2 | Modifier XP_Add.cs pour intégrer le bonus multiplier | actions/XP_Add.cs |
| 4.3 | Créer REWARD_BonusXp.cs | actions/REWARD_BonusXp.cs |
| 4.4 | Créer REWARD_GrantXp.cs | actions/REWARD_GrantXp.cs |
| 4.5 | Configurer les Channel Points Twitch | Twitch Dashboard |

**Critère de validation Phase 4 :** Double XP actif multiplie correctement les gains. Bonus expire après la durée configurée.

---

### Phase 5 — Enrichissement des overlays

| # | Tâche | Fichier(s) concerné(s) |
|---|---|---|
| 5.1 | Mettre à jour CARD_ShowProfile.cs — nouveaux champs payload | actions/CARD_ShowProfile.cs |
| 5.2 | Mettre à jour card.js — afficher watchtime et bonus | overlays/card/card.js |
| 5.3 | Mettre à jour card.html — nouveaux éléments DOM | overlays/card/card.html |
| 5.4 | Mettre à jour card.css — styles nouveaux éléments | overlays/card/card.css |

**Critère de validation Phase 5 :** La profile card affiche le watchtime et l'indicateur de bonus actif.

---

### Phase 6 — Documentation

| # | Tâche | Fichier(s) concerné(s) |
|---|---|---|
| 6.1 | Mettre à jour ARCHITECTURE.md | docs/ARCHITECTURE.md |
| 6.2 | Créer EVENTS.md avec tous les déclencheurs V2 | docs/EVENTS.md |
| 6.3 | Créer DATA.md avec le schéma UserProfile V2 | docs/DATA.md |
| 6.4 | Mettre à jour INSTALLATION.md | INSTALLATION.md |

---

## 10. Ordre de développement recommandé

```
Phase 1 : Fondations
    1. UserProfile (6 nouveaux champs)
    2. XpService (reason + multiplier)
    3. config.json + ConfigService

        ↓  Validation : V1 toujours fonctionnelle

Phase 2 : Watchtime
    4. WatchTimeService
    5. XP_WatchTime action

        ↓  Validation : watchtime distribue XP

Phase 3 : !rank
    6. RankService
    7. RANK_ShowCommand action

        ↓  Validation : !rank répond en chat

Phase 4 : Récompenses
    8. RewardService
    9. XP_Add (intégration bonus)
    10. REWARD_BonusXp action
    11. REWARD_GrantXp action

        ↓  Validation : double XP fonctionne

Phase 5 : Overlays
    12. CARD_ShowProfile (nouveaux champs)
    13. card.js / card.html / card.css

        ↓  Validation : card affiche nouvelles stats

Phase 6 : Documentation
    14. Docs mis à jour
```

**Principe directeur :** Chaque phase est autonome et livrable. Il est possible de s'arrêter après la Phase 3 et d'avoir un système V2 partiel mais fonctionnel.

---

## 11. Risques et points d'attention

### Risque 1 — Lecture/écriture concurrente

Le timer watchtime et le timer leaderboard peuvent tourner simultanément. Streamer.bot est mono-threaded par action, mais deux actions peuvent se chevaucher.

**Mitigation :** Décaler les timers (ex: watchtime à H:00, leaderboard à H:00:30).

---

### Risque 2 — CPH.GetActiveViewers() fiabilité

Streamer.bot peut retourner une liste incomplète ou vide selon la connexion Twitch.

**Mitigation :** Vérifier que la liste n'est pas nulle avant d'itérer. Logger le nombre de viewers traités via `%watch_usersProcessed%`.

---

### Risque 3 — Migration des profils V1

Les profils JSON existants n'ont pas les nouveaux champs (`XpFromChat`, `BonusMultiplier`, etc.).

**Mitigation :** UserRepository doit utiliser des valeurs par défaut lors de la désérialisation si les champs sont absents. Comportement géré par `JsonConvert.DeserializeObject` avec `DefaultValueHandling`.

---

### Risque 4 — Bonus multiplier expiré non nettoyé

Si un viewer n'envoie pas de message après expiration du bonus, son profil garde `ActiveBonusMultiplier = 2.0` en JSON indéfiniment.

**Mitigation :** `RewardService.GetCurrentMultiplier()` vérifie toujours le timestamp avant de retourner le multiplier. Si expiré, nettoie le profil et retourne 1.0. Le nettoyage est paresseux (lazy cleanup).

---

## 12. Nouveaux fichiers à créer

```
scripts/
    WatchTimeService.cs        ← Phase 2
    RankService.cs             ← Phase 3
    RewardService.cs           ← Phase 4

actions/
    XP_WatchTime.cs            ← Phase 2
    RANK_ShowCommand.cs        ← Phase 3
    REWARD_BonusXp.cs          ← Phase 4
    REWARD_GrantXp.cs          ← Phase 4

docs/
    V2_ARCHITECTURE.md         ← ce fichier
    EVENTS.md                  ← Phase 6
    DATA.md                    ← Phase 6
```

## 13. Fichiers V1 à modifier

```
scripts/
    UserRepository.cs          ← +6 champs UserProfile
    XpService.cs               ← AddXp signature + sources XP
    ConfigService.cs           ← +nouveaux paramètres config

actions/
    XP_Add.cs                  ← intégration RewardService (bonus check)
    CARD_ShowProfile.cs        ← +nouveaux champs payload

configs/
    config.json                ← +nouveaux paramètres

overlays/card/
    card.html                  ← +nouveaux éléments DOM
    card.js                    ← +affichage watchtime + bonus
    card.css                   ← +styles nouveaux éléments
```

---

*Plan d'architecture V2 — aucun code. Prêt pour implémentation phase par phase.*
