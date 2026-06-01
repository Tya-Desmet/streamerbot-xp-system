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
//
// AUCUNE logique XP — AUCUNE écriture disque — AUCUN overlay
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class TitleEntry
{
    public int    MinLevel { get; set; }
    public string Title    { get; set; }
}

public class TitleService
{
    public string GetTitle(int level, string projectPath, string theme)
    {
        var titles = LoadTitles(projectPath, theme);
        return ResolveTitle(level, titles);
    }

    public List<TitleEntry> LoadTitles(string projectPath, string theme)
    {
        var user = TryLoadFile(Path.Combine(projectPath, "configs", "titles.json"));
        if (user != null) return user;

        if (!string.IsNullOrEmpty(theme) &&
            !string.Equals(theme, "default", StringComparison.OrdinalIgnoreCase))
        {
            var t = TryLoadFile(Path.Combine(projectPath, "themes", theme, "titles.json"));
            if (t != null) return t;
        }

        var def = TryLoadFile(Path.Combine(projectPath, "themes", "default", "titles.json"));
        if (def != null) return def;

        var fallback = new List<TitleEntry>();
        fallback.Add(new TitleEntry { MinLevel = 1, Title = "Viewer" });
        return fallback;
    }

    private string ResolveTitle(int level, List<TitleEntry> titles)
    {
        var candidates = new List<TitleEntry>();
        foreach (var t in titles)
            if (t.MinLevel >= 1 && !string.IsNullOrEmpty(t.Title))
                candidates.Add(t);
        candidates.Sort((a, b) => b.MinLevel.CompareTo(a.MinLevel));
        foreach (var e in candidates)
            if (level >= e.MinLevel) return e.Title;
        if (candidates.Count > 0)
            return candidates[candidates.Count - 1].Title;
        return "";
    }

    private List<TitleEntry> TryLoadFile(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var entries = JsonConvert.DeserializeObject<List<TitleEntry>>(File.ReadAllText(path));
            if (entries == null || entries.Count == 0) return null;
            var valid = new List<TitleEntry>();
            foreach (var e in entries)
                if (e.MinLevel >= 1 && !string.IsNullOrEmpty(e.Title))
                    valid.Add(e);
            return valid.Count > 0 ? valid : null;
        }
        catch { return null; }
    }
}
