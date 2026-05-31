// ============================================================
// ACTION : XP_WatchTime
// ============================================================
// RÔLE : Distribuer l'XP watchtime aux viewers actifs en chat.
//         Les comptes exclus (bots) sont ignorés.
//         Action silencieuse — aucun overlay déclenché.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "XP_WatchTime"
//   2. Déclencheur : Timer (intervalle configurable)
//      Ajouter un Delay de 30 000 ms pour décaler du timer LB
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  Persistante : oui
//
// ARGUMENTS SORTANTS :
//   %watch_processed%        int  — viewers traités
//   %watch_xp_distributed%   int  — XP total distribué
//   %watch_level_ups%        int  — level-ups détectés
//   %watch_skipped%          int  — viewers ignorés (exclus ou pas actifs)
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
    public int    Rank                 { get; set; }
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
            try
            {
                var user = JsonConvert.DeserializeObject<UserProfile>(File.ReadAllText(file));
                if (user != null) users.Add(user);
            }
            catch { }
        }
        return users;
    }

    public void SaveUser(UserProfile user)
    {
        File.WriteAllText(
            Path.Combine(_dataPath, user.Username + ".json"),
            JsonConvert.SerializeObject(user, Formatting.Indented));
    }
}

// ----- WatchTimeService (source : scripts/WatchTimeService.cs) -----

public class WatchTimeService
{
    private const int TimerToleranceSeconds = 10;

    public bool IsEligible(UserProfile user, int intervalMinutes, long nowSeconds)
    {
        if (user == null) return false;
        return (nowSeconds - user.LastWatchTimestamp) >= (intervalMinutes * 60 - TimerToleranceSeconds);
    }
}

// ----- XpResult + XpService (source : scripts/XpService.cs) -----

public class XpResult
{
    public int  XpAdded   { get; set; }
    public int  OldLevel  { get; set; }
    public int  NewLevel  { get; set; }
    public bool IsLevelUp { get; set; }
}

public class XpService
{
    private readonly UserRepository _repo;

    public XpService(UserRepository repo) { _repo = repo; }

    public XpResult AddWatchTimeXp(UserProfile user, int amount, int intervalMinutes, long nowSeconds)
    {
        if (user == null) return null;
        var oldLevel            = user.Level;
        user.Xp                += amount;
        user.Level              = CalculateLevel(user.Xp);
        user.WatchTime         += intervalMinutes;
        user.LastWatchTimestamp = nowSeconds;
        _repo.SaveUser(user);
        return new XpResult { XpAdded = amount, OldLevel = oldLevel, NewLevel = user.Level, IsLevelUp = user.Level > oldLevel };
    }

    private int CalculateLevel(int totalXp)
    {
        var level = 1; var acc = 0;
        while (true)
        {
            var threshold = (int)(100 * Math.Pow(level, 1.5));
            if (acc + threshold > totalXp) break;
            acc += threshold; level++;
        }
        return level;
    }
}

// ----- BotExclusionService (source : scripts/BotExclusionService.cs) -----

public class BotExclusionService
{
    private readonly HashSet<string> _excluded;

    public BotExclusionService(string projectPath, string broadcasterName, bool excludeBroadcaster)
    {
        _excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Load(projectPath);
        if (excludeBroadcaster && !string.IsNullOrEmpty(broadcasterName))
            _excluded.Add(broadcasterName.Trim());
    }

    public bool IsExcluded(string username)
    {
        if (string.IsNullOrEmpty(username)) return true;
        return _excluded.Contains(username.Trim());
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
                        if (!string.IsNullOrWhiteSpace(name)) _excluded.Add(name.Trim());
                if (_excluded.Count > 0) return;
            }
            catch { }
        }
        _excluded.Add("nightbot");     _excluded.Add("streamelements");
        _excluded.Add("streamlabs");   _excluded.Add("moobot");
        _excluded.Add("fossabot");     _excluded.Add("wizebot");
        _excluded.Add("mixitupbot");   _excluded.Add("streamerbot");
    }
}

// ----- Config + ConfigService (source : scripts/ConfigService.cs) -----

public class Config
{
    public string DataPath                 { get; set; }
    public bool?  WatchtimeEnabled         { get; set; }
    public int    WatchtimeIntervalMinutes { get; set; }
    public int    XpPerWatchInterval       { get; set; }
    public bool?  ExcludeBroadcaster       { get; set; }
    public string BroadcasterName          { get; set; }
}

public class ConfigService
{
    public Config LoadConfig(string path)
    {
        Config c = null;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            try { c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path)); } catch { }
        c = c ?? new Config();
        if (c.WatchtimeIntervalMinutes <= 0) c.WatchtimeIntervalMinutes = 5;
        if (c.XpPerWatchInterval       <= 0) c.XpPerWatchInterval       = 5;
        if (!c.WatchtimeEnabled.HasValue)    c.WatchtimeEnabled         = true;
        if (!c.ExcludeBroadcaster.HasValue)  c.ExcludeBroadcaster       = false;
        return c;
    }
}

// ----- Action Streamer.bot -----

public class CPHInline
{
    public bool Execute()
    {
        // 1. Configuration + chemins
        var configPath  = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config      = new ConfigService().LoadConfig(configPath);
        var configDir   = Path.GetDirectoryName(configPath ?? "");
        var projectPath = Path.GetDirectoryName(configDir ?? "");

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[XP_WatchTime] dataPath non configuré");
            return false;
        }

        if (config.WatchtimeEnabled == false)
        {
            CPH.LogInfo("[XP_WatchTime] Watchtime désactivé dans config.json");
            return true;
        }

        // 2. Service d'exclusion
        var bots = new BotExclusionService(
            projectPath,
            config.BroadcasterName ?? "",
            config.ExcludeBroadcaster == true);

        var now          = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var repo         = new UserRepository(config.DataPath);
        var xpService    = new XpService(repo);
        var watchService = new WatchTimeService();
        var interval     = config.WatchtimeIntervalMinutes;
        var xpAmount     = config.XpPerWatchInterval;
        var activityWindowSeconds = interval * 60 * 2;

        var allUsers = repo.GetAllUsers();

        if (allUsers.Count == 0)
        {
            CPH.LogInfo("[XP_WatchTime] Aucun profil trouvé");
            CPH.SetArgument("watch_processed",      0);
            CPH.SetArgument("watch_xp_distributed", 0);
            CPH.SetArgument("watch_level_ups",      0);
            CPH.SetArgument("watch_skipped",        0);
            return true;
        }

        var processed = 0; var xpTotal = 0; var levelUps = 0; var skipped = 0;

        // 3. Boucle — exclure les bots en premier
        foreach (var user in allUsers)
        {
            // Ignorer les exclus (bots)
            if (bots.IsExcluded(user.Username)) { skipped++; continue; }

            // Ignorer si pas actif en chat récemment
            if (now - user.LastMessageTimestamp > activityWindowSeconds) { skipped++; continue; }

            // Ignorer si watchtime déjà attribué ce cycle
            if (!watchService.IsEligible(user, interval, now)) { skipped++; continue; }

            // Octroyer l'XP watchtime
            var result = xpService.AddWatchTimeXp(user, xpAmount, interval, now);
            if (result == null) { skipped++; continue; }

            processed++;
            xpTotal += result.XpAdded;

            if (result.IsLevelUp)
            {
                levelUps++;
                CPH.LogInfo("[XP_WatchTime] LEVEL UP ! " + user.Username + " : Niv. " + result.OldLevel + " -> " + result.NewLevel);
            }
        }

        // 4. Exposer
        CPH.SetArgument("watch_processed",      processed);
        CPH.SetArgument("watch_xp_distributed", xpTotal);
        CPH.SetArgument("watch_level_ups",      levelUps);
        CPH.SetArgument("watch_skipped",        skipped);

        CPH.LogInfo("[XP_WatchTime] Cycle terminé — " + processed + " traités, " + xpTotal + " XP, " + levelUps + " level-ups, " + skipped + " ignorés");

        return true;
    }
}
