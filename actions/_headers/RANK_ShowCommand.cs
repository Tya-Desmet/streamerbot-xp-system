// ============================================================
// ACTION : RANK_ShowCommand (V2)
// ============================================================
// RÔLE : Répondre à !rank dans le chat. Les comptes exclus
//         sont ignorés silencieusement.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "RANK_ShowCommand"
//   2. Déclencheur : Twitch → Chat Command → !rank
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  Persistante : oui
//
// LECTURE SEULE — AUCUNE modification de profil
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
