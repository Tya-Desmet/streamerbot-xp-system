// ============================================================
// ACTION : LEADERBOARD_Update
// ============================================================
// RÔLE : Lecture seule. Construit le Top 3 et l'envoie à l'overlay OBS.
//         Aucune modification de données utilisateur — aucune logique XP.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "LEADERBOARD_Update"
//   2. Déclencheur suggéré : Timer (ex: toutes les 5 min)
//                            ou appelée manuellement en sous-action
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//
// PRÉREQUIS — une seule variable globale dans Streamer.bot :
//   xp_configPath  string  ex: C:\...\streamerbot-xp-system\configs\config.json
//   Persistante : oui — toute la config est dans configs/config.json
//
// FORMAT JSON envoyé à l'overlay (leaderboard.js attend ce format) :
//   window.updateLeaderboard([
//     { rank: 1, username: "Mystya",    level: 42, xp: 18500, avatar: "" },
//     { rank: 2, username: "Tya_Plays", level: 31, xp: 11200, avatar: "" },
//     { rank: 3, username: "ZephyrTV",  level: 28, xp: 9800,  avatar: "" }
//   ])
//
// ARGUMENTS SORTANTS (disponibles dans les sub-actions suivantes) :
//   %lb_count%       int    — entrées envoyées (0–3)
//   %lb_1_username%  string — displayName #1
//   %lb_1_xp%        int    — XP #1
//   %lb_1_level%     int    — niveau #1
//   %lb_2_username%  string — displayName #2  (idem lb_3_*)
//   ...
//
// LECTURE SEULE — aucune écriture — aucune modification utilisateur
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

// ----- UserProfile -----

public class UserProfile
{
    public string Username             { get; set; }
    public string DisplayName          { get; set; }
    public int    Xp                   { get; set; }
    public int    Level                { get; set; }
    public int    Messages             { get; set; }
    public int    WatchTime            { get; set; }
    public int    Rank                 { get; set; }
    public long   LastMessageTimestamp { get; set; }
}

// ----- UserRepository — lecture seule, GetAllUsers inclus -----

public class UserRepository
{
    private readonly string _dataPath;

    public UserRepository(string dataPath)
    {
        _dataPath = dataPath;
        Directory.CreateDirectory(_dataPath);
    }

    // Charge tous les profils du dossier data/users/*.json
    // Scalable : leaderboard, export web, stats globales
    public List<UserProfile> GetAllUsers()
    {
        var users = new List<UserProfile>();
        var files = Directory.GetFiles(_dataPath, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var user = JsonConvert.DeserializeObject<UserProfile>(json);
                if (user != null) users.Add(user);
            }
            catch { /* ignorer les fichiers malformés */ }
        }

        return users;
    }
}

// ----- LeaderboardEntry — résultat du tri -----

public class LeaderboardEntry
{
    public int    Rank        { get; set; }
    public string Username    { get; set; }
    public string DisplayName { get; set; }
    public int    Xp          { get; set; }
    public int    Level       { get; set; }
}

// ----- Top3Entry — payload JSON exact attendu par leaderboard.js -----
// Noms de propriétés en minuscule → sérialisation JSON directement compatible JS

public class Top3Entry
{
    public int    rank     { get; set; }
    public string username { get; set; }
    public int    level    { get; set; }
    public int    xp       { get; set; }
    public string avatar   { get; set; }
}

// ----- XpService — extrait : PrepareLeaderboard uniquement -----

public class XpService
{
    // Tri décroissant par XP + attribution des rangs
    // Reçoit la liste complète fournie par UserRepository.GetAllUsers()
    public List<LeaderboardEntry> PrepareLeaderboard(List<UserProfile> users)
    {
        users.Sort((a, b) => b.Xp.CompareTo(a.Xp));
        var result = new List<LeaderboardEntry>();
        for (var i = 0; i < users.Count; i++)
        {
            var u = users[i];
            result.Add(new LeaderboardEntry
            {
                Rank        = i + 1,
                Username    = u.Username,
                DisplayName = string.IsNullOrEmpty(u.DisplayName) ? u.Username : u.DisplayName,
                Xp          = u.Xp,
                Level       = u.Level
            });
        }
        return result;
    }
}

// ----- Config + ConfigService (source : scripts/ConfigService.cs) -----

public class Config
{
    public string DataPath                   { get; set; }
    public int    XpPerMessage               { get; set; }
    public int    XpPerWatch                 { get; set; }
    public int    CooldownSeconds            { get; set; }
    public int    MinMessageLength           { get; set; }
    public int    LeaderboardIntervalMinutes { get; set; }
    public string ObsLeaderboardSource       { get; set; }
    public string ObsCardSource              { get; set; }
}

public class ConfigService
{
    public Config LoadConfig(string path)
    {
        Config c = null;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            try { c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path)); } catch { }
        c = c ?? new Config();
        if (c.XpPerMessage               <= 0) c.XpPerMessage               = 10;
        if (c.XpPerWatch                 <= 0) c.XpPerWatch                 = 1;
        if (c.CooldownSeconds            <= 0) c.CooldownSeconds            = 30;
        if (c.MinMessageLength           <= 0) c.MinMessageLength           = 2;
        if (c.LeaderboardIntervalMinutes <= 0) c.LeaderboardIntervalMinutes = 5;
        if (string.IsNullOrEmpty(c.ObsLeaderboardSource)) c.ObsLeaderboardSource = "Leaderboard";
        if (string.IsNullOrEmpty(c.ObsCardSource))        c.ObsCardSource        = "ProfileCard";
        return c;
    }
}

// ----- Action Streamer.bot -----

public class CPHInline
{
    public bool Execute()
    {
        // 1. Charger la configuration depuis config.json
        var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config     = new ConfigService().LoadConfig(configPath);

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[LEADERBOARD_Update] dataPath non configuré dans configs/config.json");
            return false;
        }

        if (string.IsNullOrEmpty(config.ObsLeaderboardSource))
        {
            CPH.LogWarn("[LEADERBOARD_Update] obsLeaderboardSource non configuré dans configs/config.json");
            return false;
        }

        var dataPath  = config.DataPath;
        var obsSource = config.ObsLeaderboardSource;

        // 2. Charger tous les profils (lecture seule)
        var repo     = new UserRepository(dataPath);
        var allUsers = repo.GetAllUsers();

        if (allUsers.Count == 0)
        {
            CPH.LogInfo("[LEADERBOARD_Update] Aucun profil trouvé — overlay non mis à jour");
            CPH.SetArgument("lb_count", 0);
            return true;
        }

        // 3. Trier + attribuer les rangs
        var xpService = new XpService();
        var ranked    = xpService.PrepareLeaderboard(allUsers);

        // 4. Extraire le Top 10
        var topCount  = ranked.Count < 10 ? ranked.Count : 10;
        var topPlayers = ranked.GetRange(0, topCount);

        // 5. Construire le payload JSON pour leaderboard.js
        var payload = new List<Top3Entry>();
        foreach (var e in topPlayers)
        {
            payload.Add(new Top3Entry
            {
                rank     = e.Rank,
                username = e.DisplayName,
                level    = e.Level,
                xp       = e.Xp,
                avatar   = ""
            });
        }

        // 6. Diffuser via WebSocket Streamer.bot → overlay leaderboard
        var wsPayload = JsonConvert.SerializeObject(new {
            @event  = "updateLeaderboard",
            players = payload
        });
        CPH.WebsocketBroadcastJson(wsPayload);

        CPH.LogInfo($"[LEADERBOARD_Update] Top {topPlayers.Count} envoyé");

        // 7. Exposer le Top 10 aux sub-actions suivantes si besoin
        CPH.SetArgument("lb_count", topPlayers.Count);

        for (var i = 0; i < topPlayers.Count; i++)
        {
            var prefix = "lb_" + (i + 1) + "_";
            CPH.SetArgument(prefix + "username", topPlayers[i].DisplayName);
            CPH.SetArgument(prefix + "xp",       topPlayers[i].Xp);
            CPH.SetArgument(prefix + "level",     topPlayers[i].Level);
        }

        return true;
    }
}
