// ============================================================
// ACTION : XP_WatchTime_V2
// ============================================================
// RÔLE : Distribuer l'XP watchtime via le trigger Present Viewers.
//         Système hybride : présence SB chatters + activité chat.
//         Les comptes exclus (bots) sont ignorés.
//         Action silencieuse — aucun overlay déclenché.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "XP_WatchTime_V2"
//   2. Déclencheur : Twitch → General → Present Viewers
//      Activer "Live Update" (cocher la case)
//      Intervalle : 5 minutes
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//   ⚠ Remplace XP_WatchTime (Timer) — désactiver l'ancienne action
//
// PRÉREQUIS :
//   xp_configPath  string  Persistante : oui
//
// ARGUMENTS ENTRANTS (fournis par le trigger Present Viewers) :
//   args["isLive"]  bool                             — stream en live
//   args["isTest"]  bool                             — test manuel SB
//   args["users"]   List<Dictionary<string,object>>  — chatters présents
//
// ARGUMENTS SORTANTS :
//   %watch_processed%        int  — viewers traités ce cycle
//   %watch_xp_distributed%   int  — XP total distribué
//   %watch_level_ups%        int  — level-ups détectés
//   %watch_skipped%          int  — viewers ignorés (exclus / inéligibles)
//   %watch_presence_count%   int  — chatters dans la liste SB
//
// ÉLIGIBILITÉ HYBRIDE :
//   Un viewer est servi s'il remplit au moins une condition :
//   • Présent dans la liste SB chatters (Live Update)
//   • A chatté dans les 2× intervalles précédents (LastMessageTimestamp)
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
