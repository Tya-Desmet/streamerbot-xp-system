// ============================================================
// ACTION : USER_GetOrCreate
// ============================================================
// DÉCLENCHEMENT :
//   Appelée en première sub-action de toute action Twitch
//   qui a besoin d'un profil utilisateur (XP_Add, etc.)
//
// PRÉREQUIS — une seule variable globale dans Streamer.bot :
//   xp_configPath  string  ex: C:\...\streamerbot-xp-system\configs\config.json
//   Persistante : oui
//
// ARGUMENTS ENTRANTS :
//   args["userName"]        — login Twitch (toujours minuscule)
//   args["userDisplayName"] — pseudo affiché (avec majuscules)
//
// ARGUMENTS SORTANTS :
//   %user_excluded%      bool   — true si le compte est dans la liste d'exclusion
//   %user_username%      string — login Twitch
//   %user_displayName%   string — pseudo affiché
//   %user_xp%            int    — XP total accumulé
//   %user_level%         int    — niveau actuel
//   %user_messages%      int    — messages validés
//   %user_watchtime%     int    — watchtime en minutes
//   %user_isNew%         bool   — true si profil créé ce cycle
//
// AUCUNE logique XP — AUCUNE logique leaderboard — AUCUN overlay
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
