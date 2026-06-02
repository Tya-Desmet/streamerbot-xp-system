// ============================================================
// ACTION : EXPORT_Snapshot (V3)
// ============================================================
// RÔLE : Lecture seule. Exporte les données du système XP vers
//         des fichiers JSON (contrat V3) consommés par le hub web.
//         Filtre les bots avant export. N'écrit aucun profil.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "EXPORT_Snapshot"
//   2. Déclencheur : Timer (intervalle configurable, ex. 5 min)
//      ⚠ Décaler de ~60s par rapport aux timers LEADERBOARD/WATCHTIME
//   3. Sub-Action : Execute C# Code → coller actions/generated/EXPORT_Snapshot.cs
//   4. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  Persistante : oui
//   config.json → section "export" avec "enabled": true
//
// INOCUITÉ : si export.enabled != true → ne fait rien (return true).
// LECTURE SEULE — aucune écriture de profil — aucune modification XP
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
