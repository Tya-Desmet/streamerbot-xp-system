// ============================================================
// ACTION : LEADERBOARD_Update (V2)
// ============================================================
// RÔLE : Lecture seule. Construit le Top 10 et l'envoie à l'overlay OBS.
//         Les comptes exclus (bots) sont filtrés avant le tri.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "LEADERBOARD_Update"
//   2. Déclencheur : Timer (intervalle configurable)
//      ⚠ Décaler de 30s par rapport au timer XP_WatchTime
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  Persistante : oui
//
// TRI V2 : Level DESC → XP DESC → WatchTime DESC
// EXCLUSION : comptes dans configs/excluded-users.json ignorés
//
// LECTURE SEULE — aucune écriture — aucune modification utilisateur
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

// ----- UserProfile (source : scripts/UserRepository.cs) -----

public class UserProfile
{
    public string Username             { get; set; }
    public string DisplayName          { get; set; }
    public int    Xp                   { get; set; }
    public int    Level                { get; set; }
    public int    Messages             { get; set; }
    public int    WatchTime            { get; set; }
    public long   LastMessageTimestamp { get; set; }
    public long   LastWatchTimestamp   { get; set; }
}

// ----- UserRepository (source : scripts/UserRepository.cs) -----

public class UserRepository
{
    private readonly string _dataPath;

    public UserRepository(string dataPath)
    {
        _dataPath = dataPath;
        Directory.CreateDirectory(_dataPath);
    }

    public List<UserProfile> GetAllUsers()
    {
        var users = new List<UserProfile>();
        foreach (var file in Directory.GetFiles(_dataPath, "*.json"))
        {
            if (file.EndsWith(".tmp")) continue;
            try
            {
                var user = JsonConvert.DeserializeObject<UserProfile>(File.ReadAllText(file));
                if (user != null) users.Add(user);
            }
            catch (Exception ex)
            {
                try
                {
                    var errPath = Path.Combine(_dataPath, "_errors.log");
                    File.AppendAllText(errPath,
                        "[" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "] "
                        + "Fichier corrompu : " + Path.GetFileName(file)
                        + " — " + ex.Message + "\n");
                }
                catch { }
            }
        }
        return users;
    }
}

// ----- BotExclusionService (source : scripts/BotExclusionService.cs) -----

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
        _excluded["nightbot"] = true;     _excluded["streamelements"] = true;
        _excluded["streamlabs"] = true;   _excluded["moobot"] = true;
        _excluded["fossabot"] = true;     _excluded["wizebot"] = true;
        _excluded["mixitupbot"] = true;   _excluded["streamerbot"] = true;
    }
}

// ----- LeaderboardEntry + LeaderboardPlayerEntry -----

public class LeaderboardEntry
{
    public int    Rank        { get; set; }
    public string DisplayName { get; set; }
    public int    Xp          { get; set; }
    public int    Level       { get; set; }
    public int    WatchTime   { get; set; }
}

public class LeaderboardPlayerEntry
{
    public int    rank     { get; set; }
    public string username { get; set; }
    public int    level    { get; set; }
    public int    xp       { get; set; }
    public string avatar   { get; set; }
}

// ----- XpService — PrepareLeaderboard (source : scripts/XpService.cs) -----

public class XpService
{
    // Tri V2 : Level DESC → XP DESC → WatchTime DESC
    public List<LeaderboardEntry> PrepareLeaderboard(List<UserProfile> users)
    {
        users.Sort((a, b) => {
            if (b.Level    != a.Level)    return b.Level.CompareTo(a.Level);
            if (b.Xp       != a.Xp)       return b.Xp.CompareTo(a.Xp);
            return b.WatchTime.CompareTo(a.WatchTime);
        });

        var result = new List<LeaderboardEntry>();
        for (var i = 0; i < users.Count; i++)
        {
            var u = users[i];
            result.Add(new LeaderboardEntry
            {
                Rank        = i + 1,
                DisplayName = string.IsNullOrEmpty(u.DisplayName) ? u.Username : u.DisplayName,
                Xp          = u.Xp,
                Level       = u.Level,
                WatchTime   = u.WatchTime
            });
        }
        return result;
    }
}

// ----- Config (source : scripts/ConfigService.cs) -----

public class XpConfig
{
    public int PerMessage       { get; set; }
    public int PerWatchInterval { get; set; }
    public int CooldownSeconds  { get; set; }
    public int MinMessageLength { get; set; }
}

public class WatchtimeConfig
{
    public bool? Enabled         { get; set; }
    public int   IntervalMinutes { get; set; }
    public bool? StreakEnabled   { get; set; }
    public bool? CountOffline    { get; set; }
}

public class LeaderboardConfig
{
    public int IntervalMinutes { get; set; }
    public int TopCount        { get; set; }
}

public class ObsConfig
{
    public string LeaderboardSource { get; set; }
    public string CardSource        { get; set; }
}

public class RankConfig
{
    public int CooldownSeconds { get; set; }
}

public class BotsConfig
{
    public bool?  ExcludeBroadcaster { get; set; }
    public string BroadcasterName    { get; set; }
}

public class DebugConfig
{
    public bool Verbose { get; set; }
}

public class Config
{
    public string            DataPath    { get; set; }
    public string            Theme       { get; set; }
    public XpConfig          Xp          { get; set; }
    public WatchtimeConfig   Watchtime   { get; set; }
    public LeaderboardConfig Leaderboard { get; set; }
    public ObsConfig         Obs         { get; set; }
    public RankConfig        Rank        { get; set; }
    public BotsConfig        Bots        { get; set; }
    public DebugConfig       Debug       { get; set; }
}

public class ConfigService
{
    private readonly IInlineInvokeProxy _CPH;

    public ConfigService(IInlineInvokeProxy CPH = null) { _CPH = CPH; }

    public Config LoadConfig(string path)
    {
        Config c = null;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                if (_CPH != null)
                    _CPH.LogWarn("[ConfigService] config.json invalide : " + ex.Message);
            }
        }
        c = c ?? new Config();
        ApplyDefaults(c);
        return c;
    }

    private void ApplyDefaults(Config c)
    {
        if (string.IsNullOrEmpty(c.Theme)) c.Theme = "default";

        if (c.Xp == null) c.Xp = new XpConfig();
        if (c.Xp.PerMessage       <= 0) c.Xp.PerMessage       = 10;
        if (c.Xp.PerWatchInterval <= 0) c.Xp.PerWatchInterval = 5;
        if (c.Xp.CooldownSeconds  <= 0) c.Xp.CooldownSeconds  = 30;
        if (c.Xp.MinMessageLength <= 0) c.Xp.MinMessageLength = 2;

        if (c.Watchtime == null) c.Watchtime = new WatchtimeConfig();
        if (!c.Watchtime.Enabled.HasValue)       c.Watchtime.Enabled         = true;
        if (c.Watchtime.IntervalMinutes  <= 0)   c.Watchtime.IntervalMinutes = 5;
        if (!c.Watchtime.StreakEnabled.HasValue)  c.Watchtime.StreakEnabled   = true;
        if (!c.Watchtime.CountOffline.HasValue)   c.Watchtime.CountOffline    = false;

        if (c.Leaderboard == null) c.Leaderboard = new LeaderboardConfig();
        if (c.Leaderboard.IntervalMinutes <= 0) c.Leaderboard.IntervalMinutes = 5;
        if (c.Leaderboard.TopCount        <= 0) c.Leaderboard.TopCount        = 10;

        if (c.Obs == null) c.Obs = new ObsConfig();
        if (string.IsNullOrEmpty(c.Obs.LeaderboardSource)) c.Obs.LeaderboardSource = "Leaderboard";
        if (string.IsNullOrEmpty(c.Obs.CardSource))        c.Obs.CardSource        = "ProfileCard";

        if (c.Rank == null) c.Rank = new RankConfig();
        if (c.Rank.CooldownSeconds <= 0) c.Rank.CooldownSeconds = 30;

        if (c.Bots == null) c.Bots = new BotsConfig();
        if (!c.Bots.ExcludeBroadcaster.HasValue) c.Bots.ExcludeBroadcaster = false;
        if (c.Bots.BroadcasterName == null)      c.Bots.BroadcasterName    = "";

        if (c.Debug == null) c.Debug = new DebugConfig();
    }
}

// ----- Action Streamer.bot -----

public class CPHInline
{
    public bool Execute()
    {
        // 1. Configuration
        var configPath  = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config      = new ConfigService(CPH).LoadConfig(configPath);
        var configDir   = Path.GetDirectoryName(configPath ?? "");
        var projectPath = Path.GetDirectoryName(configDir ?? "");

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[LEADERBOARD_Update] dataPath non configuré dans configs/config.json");
            return false;
        }

        // 2. Charger tous les profils
        var repo     = new UserRepository(config.DataPath);
        var allUsers = repo.GetAllUsers();

        if (allUsers.Count == 0)
        {
            CPH.LogInfo("[LEADERBOARD_Update] Aucun profil trouvé — overlay non mis à jour");
            CPH.SetArgument("lb_count", 0);
            return true;
        }

        // 3. Filtrer les comptes exclus (bots) avant le tri
        var bots     = new BotExclusionService(
                           projectPath,
                           config.Bots.BroadcasterName,
                           config.Bots.ExcludeBroadcaster == true);

        var filtered = new List<UserProfile>();
        foreach (var u in allUsers)
            if (!bots.IsExcluded(u.Username))
                filtered.Add(u);

        if (filtered.Count == 0)
        {
            CPH.LogInfo("[LEADERBOARD_Update] Aucun profil après filtrage des exclusions");
            CPH.SetArgument("lb_count", 0);
            return true;
        }

        // 4. Trier — Level DESC → XP DESC → WatchTime DESC
        var ranked     = new XpService().PrepareLeaderboard(filtered);
        var topCount   = ranked.Count < config.Leaderboard.TopCount ? ranked.Count : config.Leaderboard.TopCount;
        var topPlayers = ranked.GetRange(0, topCount);

        // 5. Construire le payload pour leaderboard.js
        var payload = new List<LeaderboardPlayerEntry>();
        foreach (var e in topPlayers)
            payload.Add(new LeaderboardPlayerEntry
            {
                rank     = e.Rank,
                username = e.DisplayName,
                level    = e.Level,
                xp       = e.Xp,
                avatar   = ""
            });

        // 6. Diffuser via WebSocket
        CPH.WebsocketBroadcastJson(JsonConvert.SerializeObject(new {
            @event  = "updateLeaderboard",
            players = payload
        }));

        // Cache du classement en GlobalVar SB — lu par RANK et CARD
        var cacheData = new { cachedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), players = payload };
        CPH.SetGlobalVar("xp_leaderboard_cache", JsonConvert.SerializeObject(cacheData), false);
        CPH.LogInfo("[LEADERBOARD_Update] Cache mis a jour — " + topPlayers.Count + " joueurs");

        // Log top 3
        var logCount = topPlayers.Count < 3 ? topPlayers.Count : 3;
        var logParts = new List<string>();
        for (var j = 0; j < logCount; j++)
            logParts.Add(topPlayers[j].DisplayName + " (Niv." + topPlayers[j].Level + ")");
        CPH.LogInfo("[LEADERBOARD_Update] Top " + topPlayers.Count + " envoyé — " + string.Join(", ", logParts.ToArray()));

        // 7. Exposer le Top 10 aux sub-actions
        CPH.SetArgument("lb_count", topPlayers.Count);
        for (var i = 0; i < topPlayers.Count; i++)
        {
            var prefix = "lb_" + (i + 1) + "_";
            CPH.SetArgument(prefix + "username", topPlayers[i].DisplayName);
            CPH.SetArgument(prefix + "level",    topPlayers[i].Level);
            CPH.SetArgument(prefix + "xp",       topPlayers[i].Xp);
        }

        return true;
    }
}
