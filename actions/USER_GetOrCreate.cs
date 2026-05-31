// ============================================================
// ACTION : USER_GetOrCreate
// ============================================================
// DÉCLENCHEMENT :
//   Appelée en première sub-action de toute action Twitch
//   qui a besoin d'un profil utilisateur (XP_Add, etc.)
//
// PRÉREQUIS — une seule variable globale dans Streamer.bot :
//   xp_configPath  string  ex: C:\...\streamerbot-xp-system\configs\config.json
//   Persistante : oui
//
// ARGUMENTS ENTRANTS :
//   args["userName"]        — login Twitch (toujours minuscule)
//   args["userDisplayName"] — pseudo affiché (avec majuscules)
//
// ARGUMENTS SORTANTS :
//   %user_excluded%      bool   — true si le compte est dans la liste d'exclusion
//   %user_username%      string — login Twitch
//   %user_displayName%   string — pseudo affiché
//   %user_xp%            int    — XP total accumulé
//   %user_level%         int    — niveau actuel
//   %user_messages%      int    — messages validés
//   %user_watchtime%     int    — watchtime en minutes
//   %user_isNew%         bool   — true si profil créé ce cycle
//
// AUCUNE logique XP — AUCUNE logique leaderboard — AUCUN overlay
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
    public int    WatchStreak          { get; set; }
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

    public UserProfile LoadUser(string username)
    {
        var path = Path.Combine(_dataPath, username + ".json");
        if (!File.Exists(path)) return null;
        return JsonConvert.DeserializeObject<UserProfile>(File.ReadAllText(path));
    }

    public UserProfile CreateUser(string username, string displayName)
    {
        var user = new UserProfile
        {
            Username             = username,
            DisplayName          = displayName,
            Xp                   = 0,
            Level                = 1,
            Messages             = 0,
            WatchTime            = 0,
            Rank                 = 0,
            LastMessageTimestamp = 0,
            LastWatchTimestamp   = 0,
            WatchStreak          = 0
        };
        SaveUser(user);
        return user;
    }

    public void SaveUser(UserProfile user)
    {
        File.WriteAllText(
            Path.Combine(_dataPath, user.Username + ".json"),
            JsonConvert.SerializeObject(user, Formatting.Indented));
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
    public Config LoadConfig(string path)
    {
        Config c = null;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            try { c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path)); } catch { }
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
        // 1. Identité du viewer
        if (!args.ContainsKey("userName") || args["userName"] == null)
        {
            CPH.LogWarn("[USER_GetOrCreate] userName absent des args");
            return false;
        }

        var username    = args["userName"].ToString().Trim();
        var displayName = args.ContainsKey("userDisplayName") && args["userDisplayName"] != null
                              ? args["userDisplayName"].ToString().Trim()
                              : username;

        if (string.IsNullOrEmpty(username))
        {
            CPH.LogWarn("[USER_GetOrCreate] userName vide");
            return false;
        }

        // 2. Configuration
        var configPath  = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config      = new ConfigService().LoadConfig(configPath);
        var configDir   = Path.GetDirectoryName(configPath ?? "");
        var projectPath = Path.GetDirectoryName(configDir ?? "");

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[USER_GetOrCreate] dataPath non configuré dans configs/config.json");
            return false;
        }

        // 3. Vérification exclusion — AVANT toute création de profil
        var bots = new BotExclusionService(
            projectPath,
            config.Bots.BroadcasterName,
            config.Bots.ExcludeBroadcaster == true);

        if (bots.IsExcluded(username))
        {
            CPH.SetArgument("user_excluded", true);
            return true; // Silencieux — aucun profil créé, aucune erreur
        }

        CPH.SetArgument("user_excluded", false);

        // 4. Charger ou créer le profil
        var repo  = new UserRepository(config.DataPath);
        var isNew = false;
        var user  = repo.LoadUser(username);

        if (user == null)
        {
            user  = repo.CreateUser(username, displayName);
            isNew = true;
            CPH.LogInfo("[USER_GetOrCreate] Nouveau joueur créé : " + displayName + " (" + username + ")");
        }

        // 5. Exposer le profil aux sub-actions suivantes
        CPH.SetArgument("user_username",      user.Username);
        CPH.SetArgument("user_displayName",   user.DisplayName ?? displayName);
        CPH.SetArgument("user_xp",            user.Xp);
        CPH.SetArgument("user_level",         user.Level);
        CPH.SetArgument("user_messages",      user.Messages);
        CPH.SetArgument("user_watchtime",     user.WatchTime);
        CPH.SetArgument("user_rank",          user.Rank);
        CPH.SetArgument("user_lastTimestamp", user.LastMessageTimestamp);
        CPH.SetArgument("user_isNew",         isNew);

        return true;
    }
}
