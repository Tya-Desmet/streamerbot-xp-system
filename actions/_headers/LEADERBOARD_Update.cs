// ============================================================
// ACTION : LEADERBOARD_Update (V2)
// ============================================================
// RÔLE : Lecture seule. Construit le Top 10 et l'envoie à l'overlay OBS.
//         Les comptes exclus (bots) sont filtrés avant le tri.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "LEADERBOARD_Update"
//   2. Déclencheur : Timer (intervalle configurable)
//      ⚠ Décaler de 30s par rapport au timer XP_WatchTime
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  Persistante : oui
//
// TRI V2 : Level DESC → XP DESC → WatchTime DESC
// EXCLUSION : comptes dans configs/excluded-users.json ignorés
//
// LECTURE SEULE — aucune écriture — aucune modification utilisateur
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
