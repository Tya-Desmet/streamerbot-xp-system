// ============================================================
// ACTION : REWARD_GrantXp
// ============================================================
// RÔLE : Octroyer un montant fixe d'XP à un viewer
//         suite au rachat d'un Channel Point.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "REWARD_GrantXp"
//   2. Déclencheur : Twitch → Channel Point Redemption → "Bonus XP"
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
//   %reward_xp_granted%   int    — XP octroyé
//   %reward_xp_total%     int    — XP total après ajout
//   %reward_isLevelUp%    bool   — true si montée de niveau
//   %reward_message%      string — message de confirmation envoyé
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
