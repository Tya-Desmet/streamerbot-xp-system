// ============================================================
// ACTION : SYSTEM_Validate
// ============================================================
// RÔLE : Vérifier l'intégrité de l'installation et afficher
//         un rapport complet dans les logs Streamer.bot.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "SYSTEM_Validate"
//   2. Aucun trigger automatique — déclencher manuellement
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//
// USAGE :
//   Clic droit sur l'action → Test
//   Vérifier les logs dans la console SB
//
// PRÉREQUIS :
//   xp_configPath  string  Persistante : oui  (peut être absent — l'action le détecte)
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
