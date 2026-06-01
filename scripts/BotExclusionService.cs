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
// AUCUNE logique XP — AUCUNE écriture disque — AUCUN overlay
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class BotExclusionService
{
    private readonly Dictionary<string, bool> _excluded;

    public BotExclusionService(string projectPath, string broadcasterName, bool excludeBroadcaster)
    {
        _excluded = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        Load(projectPath);
        if (excludeBroadcaster && !string.IsNullOrEmpty(broadcasterName))
            _excluded[broadcasterName.Trim()] = true;
    }

    public bool IsExcluded(string username)
    {
        if (string.IsNullOrEmpty(username)) return true;
        return _excluded.ContainsKey(username.Trim());
    }

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
                            _excluded[name.Trim()] = true;
                // Ce fichier REMPLACE le fallback — voir configs/EXCLUDED-USERS-README.md
                if (_excluded.Count > 0) return;
            }
            catch { }
        }
        _excluded["nightbot"]      = true;
        _excluded["streamelements"] = true;
        _excluded["streamlabs"]    = true;
        _excluded["moobot"]        = true;
        _excluded["fossabot"]      = true;
        _excluded["wizebot"]       = true;
        _excluded["mixitupbot"]    = true;
        _excluded["streamerbot"]   = true;
    }
}
