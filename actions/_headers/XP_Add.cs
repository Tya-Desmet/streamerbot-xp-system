// ============================================================
// ACTION : XP_Add
// ============================================================
// RÔLE : Orchestrateur du pipeline XP chat Twitch.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "XP_Add"
//   2. Déclencheur : Twitch → Chat Message
//   3. Sub-Action 1 : Run Action → USER_GetOrCreate
//   4. Sub-Action 2 : Execute C# Code → coller ce fichier
//   5. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  ex: C:\...\streamerbot-xp-system\configs\config.json
//   Persistante : oui
//
// ARGUMENTS ENTRANTS :
//   args["user_username"]  — login Twitch (posé par USER_GetOrCreate)
//   args["user_excluded"]  — bool (posé par USER_GetOrCreate)
//   args["rawInput"]       — message brut du chat
//
// ARGUMENTS SORTANTS :
//   %xp_skipped%     bool   — true si ignoré
//   %xp_skipReason%  string — "excluded" | "command" | "too_short" | "cooldown" | ""
//   %xp_added%       int    — XP ajouté
//   %xp_total%       int    — XP total après ajout
//   %xp_oldLevel%    int    — niveau avant
//   %xp_newLevel%    int    — niveau après
//   %xp_isLevelUp%   bool   — true si montée de niveau
//   %xp_xpIntoLevel% int    — XP dans le niveau actuel
//   %xp_xpForNext%   int    — XP requis pour ce niveau
//   %xp_percentage%  float  — % de progression
//
// AUCUNE logique overlay — AUCUNE logique leaderboard
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
