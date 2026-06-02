// ============================================================
// ACTION : REWARD_BonusXp
// ============================================================
// RÔLE : Activer le multiplicateur XP temporaire pour un viewer
//         suite au rachat d'un Channel Point.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "REWARD_BonusXp"
//   2. Déclencheur : Twitch → Channel Point Redemption
//      Nom du reward : "Double XP" (configurable)
//   3. Sub-Action 1 : Run Action → USER_GetOrCreate
//   4. Sub-Action 2 : Execute C# Code → coller ce fichier
//   5. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  Persistante : oui
//
// ARGUMENTS ENTRANTS :
//   args["user_username"]   — login Twitch (posé par USER_GetOrCreate)
//   args["user_excluded"]   — bool (posé par USER_GetOrCreate)
//
// ARGUMENTS SORTANTS :
//   %reward_bonus_applied%         bool   — true si bonus activé avec succès
//   %reward_multiplier%            float  — multiplicateur appliqué (ex: 2.0)
//   %reward_expires_in_minutes%    int    — durée restante en minutes
//   %reward_was_already_active%    bool   — true si bonus remplacé
//   %reward_message%               string — message de confirmation envoyé
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
