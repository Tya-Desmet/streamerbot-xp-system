// ============================================================
// ACTION : DAILY_CheckIn
// ============================================================
// RÔLE : Check-in quotidien du viewer via Channel Point.
//         1 fois par jour, coche une case sur la carte de fidélité (10 cases).
//         Donne 10 XP par case, 100 XP quand la carte est complète.
//         Déclenche une animation sur l'overlay dédié check-in.
//         TotalCheckIns sert de hook pour les coffres V4.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "DAILY_CheckIn"
//   2. Déclencheur : Twitch → Channel Point Redemption → "Check-in"
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  Variable globale persistante
//
// ARGUMENTS ENTRANTS (fournis par SB automatiquement) :
//   args["userName"]        — login Twitch (minuscules)
//   args["userDisplayName"] — pseudo affiché
//
// ARGUMENTS SORTANTS :
//   %checkin_done%           bool   — true si check-in validé
//   %checkin_already_done%   bool   — true si déjà fait aujourd'hui
//   %checkin_xp%             int    — XP gagné (0 si refus)
//   %checkin_count%          int    — cases cochées sur la carte courante
//   %checkin_card_complete%  bool   — true si carte complète ce check-in
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
