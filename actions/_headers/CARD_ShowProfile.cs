// ============================================================
// ACTION : CARD_ShowProfile (V2)
// ============================================================
// RÔLE : Affiche la profile card OBS pour un viewer.
//         Les comptes exclus (bots) sont silencieusement ignorés.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "CARD_ShowProfile"
//   2. Déclencheur : Twitch → Channel Point Redemption
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  Persistante : oui
//
// LECTURE SEULE — aucune écriture — aucune modification
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
