// ============================================================
// ExportService.cs — Streamer.bot XP System (V3)
// ============================================================
// RESPONSABILITÉ :
//   Façonner les DTO publics (contrat V3) et les écrire en JSON
//   de façon atomique (.tmp → move).
//   NE CALCULE RIEN : XP, niveau, rang, titre, progression sont
//   calculés en amont par XpService / TitleService et passés ici.
//   NE LIT JAMAIS le disque.
//
// UTILISATION (dans EXPORT_Snapshot) :
//   var exp = new ExportService();
//   var dir = exp.ResolveExportDir(config.Export.Path, projectPath);
//   exp.WriteJson(Path.Combine(dir, "meta.json"),
//                 exp.BuildMeta(1, now, config.Export.Season, config.Export.StreamerName));
//
// MAPPING JSON :
//   Propriétés DTO en minuscules → JSON minuscule lisible par le hub web.
//   Aucun champ sensible (timestamps internes, chemins disque) n'est exporté.
//
// AUCUNE logique XP — AUCUN overlay — AUCUNE lecture disque
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

// ----- DTO publics — forme = contrat V3 (docs/EXPORT_CONTRACT.md) -----

// Profil public exporté — exports/users/{username}.json
// Champs d'AFFICHAGE uniquement. Aucun timestamp interne, aucun chemin disque.
public class PublicProfile
{
    public string username    { get; set; }
    public string displayName { get; set; }
    public int    level       { get; set; }
    public int    xp          { get; set; }
    public int    xpIntoLevel { get; set; }
    public int    xpForNext   { get; set; }
    public int    percentage  { get; set; }
    public int    rank        { get; set; }
    public string title       { get; set; }
    public int    messages    { get; set; }
    public int    watchTime   { get; set; }
    public int    watchStreak { get; set; }     // conservé (export inchangé), plus affiché côté site
    public int    totalCheckIns { get; set; }   // total cumulé de check-in (lifetime)
    public PublicSources sources { get; set; }
}

public class PublicSources
{
    public int chat    { get; set; }
    public int watch   { get; set; }
    public int rewards { get; set; }
}

// Entrée leaderboard public — exports/leaderboard.json → players[]
public class PublicLeaderboardEntry
{
    public int    rank        { get; set; }
    public string username    { get; set; }
    public string displayName { get; set; }
    public int    level       { get; set; }
    public int    xp          { get; set; }
    public int    watchTime   { get; set; }
    public string title       { get; set; }
}

// ----- Service d'export — façonnage + écriture JSON atomique -----

public class ExportService
{
    // Résout le dossier d'export. configuredPath vide → projectPath/exports.
    public string ResolveExportDir(string configuredPath, string projectPath)
    {
        if (!string.IsNullOrEmpty(configuredPath)) return configuredPath;
        return Path.Combine(projectPath ?? "", "exports");
    }

    // Construit une entrée leaderboard publique à partir d'une LeaderboardEntry
    // (déjà rangée par XpService.PrepareLeaderboard) + son titre résolu.
    public PublicLeaderboardEntry BuildLeaderboardEntry(LeaderboardEntry e, string title)
    {
        return new PublicLeaderboardEntry
        {
            rank        = e.Rank,
            username    = e.Username,
            displayName = e.DisplayName,
            level       = e.Level,
            xp          = e.Xp,
            watchTime   = e.WatchTime,
            title       = title ?? ""
        };
    }

    // Construit un profil public à partir du profil interne + données calculées.
    public PublicProfile BuildPublicProfile(UserProfile u, XpProgress p, int rank, string title)
    {
        return new PublicProfile
        {
            username    = u.Username,
            displayName = string.IsNullOrEmpty(u.DisplayName) ? u.Username : u.DisplayName,
            level       = u.Level,
            xp          = u.Xp,
            xpIntoLevel = p != null ? p.XpIntoLevel : 0,
            xpForNext   = p != null ? p.XpForNext   : 0,
            percentage  = p != null ? (int)p.Percentage : 0,
            rank        = rank,
            title       = title ?? "",
            messages    = u.Messages,
            watchTime   = u.WatchTime,
            watchStreak = u.WatchStreak,
            totalCheckIns = u.TotalCheckIns,
            sources     = new PublicSources
            {
                chat    = u.XpFromChat,
                watch   = u.XpFromWatch,
                rewards = u.XpFromRewards
            }
        };
    }

    // Construit l'objet meta.json.
    public object BuildMeta(int schemaVersion, long nowSeconds, string season, string streamerName)
    {
        return new
        {
            schemaVersion = schemaVersion,
            generatedAt   = nowSeconds,
            season        = string.IsNullOrEmpty(season) ? "all-time" : season,
            streamer      = new { name = streamerName ?? "" }
        };
    }

    // Construit l'objet leaderboard.json à partir d'entrées publiques déjà construites.
    public object BuildLeaderboardFile(List<PublicLeaderboardEntry> players, long nowSeconds, string season)
    {
        return new
        {
            generatedAt = nowSeconds,
            season      = string.IsNullOrEmpty(season) ? "all-time" : season,
            players     = players
        };
    }

    // Écriture atomique d'un objet en JSON indenté. Crée le dossier parent.
    // Retourne true si succès, false sinon (ne lève jamais).
    public bool WriteJson(string filePath, object payload)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var json    = JsonConvert.SerializeObject(payload, Formatting.Indented);
            var tmpPath = filePath + ".tmp";

            File.WriteAllText(tmpPath, json);
            if (File.Exists(filePath)) File.Delete(filePath);
            File.Move(tmpPath, filePath);
            return true;
        }
        catch
        {
            try { if (File.Exists(filePath + ".tmp")) File.Delete(filePath + ".tmp"); } catch { }
            return false;
        }
    }
}
