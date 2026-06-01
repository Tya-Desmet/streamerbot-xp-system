# PLAN DE REMÉDIATION — STREAMERBOT-XP-SYSTEM V2

> Généré après audit technique complet du 2026-05-31.
> Chaque tâche correspond à un fichier prompt dans `ai/prompt/remediation/`.
> Exécuter les prompts dans l'ordre numérique — les dépendances sont respectées.

---

## CONTRAINTES GLOBALES SB 1.0.4 — À RAPPELER DANS CHAQUE PROMPT

```
✗ System.Linq          → utiliser foreach + List.Sort()
✗ HashSet<T>           → utiliser Dictionary<string, bool>
✗ $"..." interpolation → utiliser la concaténation "a" + b + "c"
✗ Classes partagées    → chaque action embarque ses propres copies
✓ Newtonsoft.Json      → disponible (JsonConvert.SerializeObject/Deserialize)
✓ List<T>              → disponible
✓ Dictionary<K,V>      → disponible
✓ File, Directory, Path → disponibles (System.IO)
✓ DateTimeOffset.UtcNow.ToUnixTimeSeconds() → disponible
✓ IInlineInvokeProxy   → interface CPH de Streamer.bot
```

---

## STATUT GLOBAL

| Sprint | Tâches | Statut |
|--------|--------|--------|
| 0 — Diagnostic leaderboard | P00 | [ ] |
| 1 — Critiques | P01 → P04 | [ ] |
| 2 — Tooling | P05 → P06 | [ ] |
| 3 — Fonctionnel | P07 → P09 | [ ] |
| 4 — Qualité code | P10 → P14 | [ ] |
| 5 — Architecture | P15 → P16 | [ ] |

---

## SPRINT 0 — DIAGNOSTIC LEADERBOARD (URGENT)

### P00 — Diagnostiquer et corriger le leaderboard
- **Fichier prompt** : `ai/prompt/remediation/P00-leaderboard-debug.md`
- **Guide dédié** : `docs/LEADERBOARD_DEBUG.md`
- **Statut** : [ ]
- **Dépendances** : aucune
- **Durée estimée** : 1-2 heures
- **Priorité** : IMMÉDIATE — le leaderboard est cassé

---

## SPRINT 1 — CORRECTIONS CRITIQUES

### P01 — Corriger le double I/O dans XP_Add
- **Fichier prompt** : `ai/prompt/remediation/P01-fix-double-io.md`
- **Fichier concerné** : `actions/XP_Add.cs`
- **Statut** : [ ]
- **Dépendances** : aucune
- **Durée estimée** : 30 min
- **Impact** : -50% I/O disque sur chaque message chat

### P02 — Écriture atomique JSON (protection corruption)
- **Fichier prompt** : `ai/prompt/remediation/P02-fix-atomic-write.md`
- **Fichiers concernés** : `UserRepository` dans toutes les actions + `scripts/UserRepository.cs`
- **Statut** : [ ]
- **Dépendances** : aucune
- **Durée estimée** : 45 min
- **Impact** : élimination du risque de corruption de profil

### P03 — Logger toutes les exceptions silencieuses
- **Fichier prompt** : `ai/prompt/remediation/P03-fix-silent-catch.md`
- **Fichiers concernés** : toutes les actions (ConfigService, BotExclusionService, UserRepository embarqués)
- **Statut** : [ ]
- **Dépendances** : aucune
- **Durée estimée** : 45 min
- **Impact** : débogage possible en production

### P04 — Supprimer les 3 fichiers JS morts du leaderboard
- **Fichier prompt** : `ai/prompt/remediation/P04-cleanup-dead-js.md`
- **Fichiers à supprimer** : `overlays/leaderboard/js/animations.js`, `renderer.js`, `main.js`
- **Statut** : [ ]
- **Dépendances** : P00 (vérifier que le leaderboard fonctionne d'abord)
- **Durée estimée** : 5 min
- **Impact** : clarté codebase, suppression risque de confusion

---

## SPRINT 2 — TOOLING ET DOCUMENTATION

### P05 — Créer le script d'assemblage des actions
- **Fichier prompt** : `ai/prompt/remediation/P05-build-script.md`
- **Fichiers à créer** : `tools/build-actions.ps1`, restructuration `actions/`
- **Statut** : [ ]
- **Dépendances** : P01, P02, P03 (les actions doivent être à jour)
- **Durée estimée** : 4 heures
- **Impact** : fin des divergences manuelles entre scripts/ et actions/

### P06 — Réécrire DEVELOPER.md pour V2
- **Fichier prompt** : `ai/prompt/remediation/P06-update-developer-doc.md`
- **Fichier concerné** : `docs/DEVELOPER.md`
- **Statut** : [ ]
- **Dépendances** : aucune
- **Durée estimée** : 2 heures
- **Impact** : documentation correcte pour tout nouveau contributeur

---

## SPRINT 3 — FONCTIONNEL

### P07 — Cache leaderboard en GlobalVar SB
- **Fichier prompt** : `ai/prompt/remediation/P07-leaderboard-cache.md`
- **Fichiers concernés** : `actions/LEADERBOARD_Update.cs`, `actions/RANK_ShowCommand.cs`, `actions/CARD_ShowProfile.cs`
- **Statut** : [ ]
- **Dépendances** : P00, P02, P03
- **Durée estimée** : 3 heures
- **Impact** : scalabilité ×4 — repousse la limite de 200 à 800+ viewers

### P08 — Externaliser les paramètres hardcodés (WS port, durées)
- **Fichier prompt** : `ai/prompt/remediation/P08-externalize-params.md`
- **Fichiers concernés** : `overlays/card/js/socket.js`, `overlays/card/js/main.js`, `overlays/leaderboard/js/socket.js`, `overlays/leaderboard/js/leaderboard.js`, les deux HTML
- **Statut** : [ ]
- **Dépendances** : P00
- **Durée estimée** : 2 heures
- **Impact** : configurabilité sans modifier le code JS

### P09 — Implémenter debug.verbose
- **Fichier prompt** : `ai/prompt/remediation/P09-implement-debug.md`
- **Fichiers concernés** : toutes les actions
- **Statut** : [ ]
- **Dépendances** : P03 (les catch sont déjà loggés)
- **Durée estimée** : 1 heure
- **Impact** : cohérence config — le paramètre fait enfin quelque chose

---

## SPRINT 4 — QUALITÉ CODE

### P10 — Migrer et supprimer UserProfile.Rank
- **Fichier prompt** : `ai/prompt/remediation/P10-remove-rank-field.md`
- **Fichiers concernés** : `scripts/UserRepository.cs`, toutes les actions, + script migration JSON
- **Statut** : [ ]
- **Dépendances** : aucune
- **Durée estimée** : 1 heure
- **Impact** : clarté modèle de données

### P11 — Corriger void offsetWidth → double-rAF dans card
- **Fichier prompt** : `ai/prompt/remediation/P11-fix-double-raf.md`
- **Fichier concerné** : `overlays/card/js/renderer.js`
- **Statut** : [ ]
- **Dépendances** : aucune
- **Durée estimée** : 20 min
- **Impact** : cohérence technique avec le leaderboard

### P12 — Corriger !important excessif dans thème RPG
- **Fichier prompt** : `ai/prompt/remediation/P12-fix-css-important.md`
- **Fichiers concernés** : `overlays/card/card.css`, `overlays/card/themes/rpg/theme.css`
- **Statut** : [ ]
- **Dépendances** : aucune
- **Durée estimée** : 1 heure
- **Impact** : qualité CSS, extensibilité des thèmes

### P13 — Corriger configs/titles.json vide et dead CSS
- **Fichier prompt** : `ai/prompt/remediation/P13-fix-titles-and-css.md`
- **Fichiers concernés** : `configs/titles.json`, `overlays/card/card.css`, thèmes card
- **Statut** : [ ]
- **Dépendances** : aucune
- **Durée estimée** : 30 min
- **Impact** : clarté configuration

### P14 — Documenter BotExclusionService (comportement remplacement total)
- **Fichier prompt** : `ai/prompt/remediation/P14-document-bot-exclusion.md`
- **Fichiers concernés** : `docs/BOT_EXCLUSION.md`, `configs/excluded-users.json`
- **Statut** : [ ]
- **Dépendances** : aucune
- **Durée estimée** : 30 min
- **Impact** : évite la pollution du leaderboard par les bots

---

## SPRINT 5 — ARCHITECTURE

### P15 — Créer l'action SYSTEM_Validate
- **Fichier prompt** : `ai/prompt/remediation/P15-system-validate.md`
- **Fichier à créer** : `actions/SYSTEM_Validate.cs`
- **Statut** : [ ]
- **Dépendances** : P02, P03
- **Durée estimée** : 2 heures
- **Impact** : diagnostic d'installation en 1 clic

### P16 — Préparer l'interface IUserRepository (future scalabilité SQLite)
- **Fichier prompt** : `ai/prompt/remediation/P16-iuser-repository.md`
- **Fichiers à créer/modifier** : `scripts/IUserRepository.cs`, `scripts/JsonUserRepository.cs`
- **Statut** : [ ]
- **Dépendances** : P05 (script d'assemblage)
- **Durée estimée** : 3 heures
- **Impact** : changement de stockage possible sans modifier les actions

---

## ORDRE D'EXÉCUTION RECOMMANDÉ

```
P00 (diagnostic leaderboard)
 ↓
P01 + P02 + P03 (en parallèle si possible)
 ↓
P04 (après confirmation leaderboard OK)
 ↓
P05 + P06 (en parallèle)
 ↓
P07 (cache leaderboard)
 ↓
P08 + P09 + P10 + P11 + P12 + P13 + P14 (en parallèle)
 ↓
P15 + P16
```

---

## NOTE SUR LA COMPATIBILITÉ SB 1.0.4

Les fichiers dans `scripts/` sont des **références canoniques** — ils ne doivent PAS être copiés directement dans Streamer.bot car ils utilisent `System.Linq` et `HashSet<T>` incompatibles.

Les fichiers dans `actions/` sont les **versions SB-compatibles** — ce sont eux qui sont copiés dans SB.

Le script P05 (`build-actions.ps1`) automatisera la synchronisation entre les deux.
