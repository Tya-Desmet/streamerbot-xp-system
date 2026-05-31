// ============================================================
// TitleService.cs — Streamer.bot XP System
// ============================================================
// RESPONSABILITÉ :
//   Charger et résoudre les titres associés aux niveaux.
//   Chaque niveau correspond au titre de la tranche dont il fait partie.
//
// ORDRE DE PRIORITÉ DU CHARGEMENT :
//   1. configs/titles.json           ← surcharge utilisateur (priorité absolue)
//   2. themes/{theme}/titles.json    ← titres du thème courant
//   3. themes/default/titles.json    ← titres par défaut
//   4. Fallback codé en dur          ← jamais en échec
//
// RÈGLE DE RÉSOLUTION :
//   Le titre retourné est celui dont MinLevel est le plus grand
//   parmi ceux dont MinLevel ≤ niveau du viewer.
//   Exemple : level 23, titres à 1/10/20/30 → titre MinLevel 20.
//
// UTILISATION DANS UNE ACTION :
//   var projectPath = Path.GetDirectoryName(Path.GetDirectoryName(configPath));
//   var title = new TitleService().GetTitle(user.Level, projectPath, config.Theme);
//
// FORMAT DU FICHIER titles.json :
//   [
//     { "minLevel": 1,  "title": "Nouveau venu" },
//     { "minLevel": 10, "title": "Adepte" }
//   ]
//
// AUCUNE logique XP — AUCUNE écriture disque — AUCUN overlay
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

// Un palier de titre — correspond à une ligne dans titles.json
public class TitleEntry
{
    public int    MinLevel { get; set; }
    public string Title    { get; set; }
}

public class TitleService
{
    // Point d'entrée principal — retourne toujours une chaîne non-nulle
    public string GetTitle(int level, string projectPath, string theme)
    {
        var titles = LoadTitles(projectPath, theme);
        return ResolveTitle(level, titles);
    }

    // Charge la liste de titres selon la priorité configurée.
    // Retourne la première source valide trouvée.
    // Jamais null — fallback codé en dur en dernier recours.
    public List<TitleEntry> LoadTitles(string projectPath, string theme)
    {
        // 1. configs/titles.json — surcharge utilisateur (priorité absolue)
        var userPath = Path.Combine(projectPath, "configs", "titles.json");
        var user     = TryLoadFile(userPath);
        if (user != null) return user;

        // 2. themes/{theme}/titles.json — titres spécifiques au thème courant
        //    Ignoré si le thème est "default" (évite une double lecture)
        if (!string.IsNullOrEmpty(theme) &&
            !string.Equals(theme, "default", StringComparison.OrdinalIgnoreCase))
        {
            var themePath  = Path.Combine(projectPath, "themes", theme, "titles.json");
            var themeTitles = TryLoadFile(themePath);
            if (themeTitles != null) return themeTitles;
        }

        // 3. themes/default/titles.json — titres livrés par défaut avec le système
        var defaultPath   = Path.Combine(projectPath, "themes", "default", "titles.json");
        var defaultTitles = TryLoadFile(defaultPath);
        if (defaultTitles != null) return defaultTitles;

        // 4. Fallback codé en dur — le système ne peut jamais retourner un titre vide
        return HardcodedFallback();
    }

    // Résout le titre pour un niveau donné
    // Tri DESC par MinLevel → premier dont MinLevel ≤ level
    private string ResolveTitle(int level, List<TitleEntry> titles)
    {
        var candidates = titles
            .Where(t => t.MinLevel >= 1 && !string.IsNullOrEmpty(t.Title))
            .OrderByDescending(t => t.MinLevel)
            .ToList();

        foreach (var entry in candidates)
            if (level >= entry.MinLevel)
                return entry.Title;

        // Si aucun palier n'est atteint (level < MinLevel le plus bas),
        // retourner le titre du palier le plus bas disponible
        var lowest = candidates.LastOrDefault();
        return lowest != null ? lowest.Title : "";
    }

    // Tente de lire et valider un fichier titles.json
    // Retourne null si :
    //   - fichier absent
    //   - JSON malformé
    //   - liste vide
    //   - aucune entrée avec MinLevel >= 1 et Title non-vide
    private List<TitleEntry> TryLoadFile(string path)
    {
        if (!File.Exists(path)) return null;

        try
        {
            var json    = File.ReadAllText(path);
            var entries = JsonConvert.DeserializeObject<List<TitleEntry>>(json);

            if (entries == null || entries.Count == 0) return null;

            // Validation : au moins une entrée exploitable
            var valid = entries
                .Where(e => e.MinLevel >= 1 && !string.IsNullOrEmpty(e.Title))
                .ToList();

            return valid.Count > 0 ? valid : null;
        }
        catch
        {
            // JSON invalide → ignorer, passer au niveau suivant
            return null;
        }
    }

    // Fallback de sécurité — jamais exposé au viewer, sert uniquement à éviter
    // une chaîne vide si les trois fichiers titles.json sont tous absents/invalides
    private List<TitleEntry> HardcodedFallback()
    {
        return new List<TitleEntry>
        {
            new TitleEntry { MinLevel = 1, Title = "Viewer" }
        };
    }
}
