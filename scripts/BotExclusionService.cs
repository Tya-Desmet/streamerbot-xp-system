// ============================================================
// BotExclusionService.cs — Streamer.bot XP System
// ============================================================
// RESPONSABILITÉ :
//   Charger et vérifier la liste des comptes exclus du système XP.
//   Aucun exclu ne gagne d'XP, n'apparaît dans le leaderboard,
//   ne peut afficher de profile card ni répondre à !rank.
//
// SOURCES D'EXCLUSION (priorité) :
//   1. configs/excluded-users.json     ← liste personnalisée utilisateur
//   2. Si config.excludeBroadcaster = true et config.broadcasterName renseigné
//      → le streamer est automatiquement exclu
//   3. Fallback codé en dur si le fichier JSON est absent ou vide
//
// COMPARAISON : insensible à la casse (OrdinalIgnoreCase)
//   "NightBot", "nightbot", "NIGHTBOT" → même compte
//
// UTILISATION DANS UNE ACTION :
//   var configDir   = Path.GetDirectoryName(configPath ?? "");
//   var projectPath = Path.GetDirectoryName(configDir ?? "");
//   var bots = new BotExclusionService(
//                  projectPath,
//                  config.BroadcasterName ?? "",
//                  config.ExcludeBroadcaster == true);
//   if (bots.IsExcluded(username)) return true; // ignorer silencieusement
//
// AUCUNE logique XP — AUCUNE écriture disque — AUCUN overlay
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class BotExclusionService
{
    // HashSet pour recherche O(1) insensible à la casse
    private readonly HashSet<string> _excluded;

    public BotExclusionService(string projectPath, string broadcasterName, bool excludeBroadcaster)
    {
        _excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Load(projectPath);

        // Exclusion optionnelle du propriétaire du stream
        if (excludeBroadcaster && !string.IsNullOrEmpty(broadcasterName))
            _excluded.Add(broadcasterName.Trim());
    }

    // Retourne true si le compte doit être ignoré par le système XP
    public bool IsExcluded(string username)
    {
        if (string.IsNullOrEmpty(username)) return true;
        return _excluded.Contains(username.Trim());
    }

    // Charge la liste depuis configs/excluded-users.json
    // Fallback sur la liste intégrée si fichier absent ou vide
    private void Load(string projectPath)
    {
        var path = Path.Combine(projectPath, "configs", "excluded-users.json");

        if (File.Exists(path))
        {
            try
            {
                var list = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(path));
                if (list != null)
                    foreach (var name in list)
                        if (!string.IsNullOrWhiteSpace(name))
                            _excluded.Add(name.Trim());

                if (_excluded.Count > 0) return;
            }
            catch { /* JSON malformé → fallback */ }
        }

        // Fallback : bots courants si le fichier est absent ou vide
        _excluded.Add("nightbot");
        _excluded.Add("streamelements");
        _excluded.Add("streamlabs");
        _excluded.Add("moobot");
        _excluded.Add("fossabot");
        _excluded.Add("wizebot");
        _excluded.Add("mixitupbot");
        _excluded.Add("streamerbot");
    }
}
