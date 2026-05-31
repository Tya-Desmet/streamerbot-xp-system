// ============================================================
// ACTION : RANK_ShowCommand (V2)
// ============================================================
// RÔLE : Répondre à !rank dans le chat. Les comptes exclus
//         sont ignorés silencieusement.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "RANK_ShowCommand"
//   2. Déclencheur : Twitch → Chat Command → !rank
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  Persistante : oui
//
// LECTURE SEULE — AUCUNE modification de profil
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
}

// ----- XpProgress + XpService (source : scripts/XpService.cs) -----

public class XpProgress
{
    public int   XpIntoLevel { get; set; }
    public int   XpForNext   { get; set; }
    public float Percentage  { get; set; }
}

public class XpService
{
    public XpProgress GetProgress(UserProfile user)
    {
        var xpAtStart   = XpAtLevelStart(user.Level);
        var xpForNext   = XpForNextLevel(user.Level);
        var xpIntoLevel = user.Xp - xpAtStart;
        return new XpProgress
        {
            XpIntoLevel = xpIntoLevel,
            XpForNext   = xpForNext,
            Percentage  = xpForNext > 0 ? (float)xpIntoLevel / xpForNext * 100f : 0f
        };
    }

    private int XpAtLevelStart(int level)
    {
        var acc = 0;
        for (var l = 1; l < level; l++) acc += XpForNextLevel(l);
        return acc;
    }

    private int XpForNextLevel(int level) => (int)(100 * Math.Pow(level, 1.5));
}

// ----- TitleEntry + TitleService (source : scripts/TitleService.cs) -----

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

    private List<TitleEntry> LoadTitles(string projectPath, string theme)
    {
        var user = TryLoadFile(Path.Combine(projectPath, "configs", "titles.json"));
        if (user != null) return user;
        if (!string.IsNullOrEmpty(theme) && !string.Equals(theme, "default", StringComparison.OrdinalIgnoreCase))
        {
            var t = TryLoadFile(Path.Combine(projectPath, "themes", theme, "titles.json"));
            if (t != null) return t;
        }
        var def = TryLoadFile(Path.Combine(projectPath, "themes", "default", "titles.json"));
        if (def != null) return def;
        var fb = new List<TitleEntry>(); fb.Add(new TitleEntry { MinLevel = 1, Title = "Viewer" }); return fb;
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
        if (candidates.Count > 0) return candidates[candidates.Count - 1].Title;
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
                if (e.MinLevel >= 1 && !string.IsNullOrEmpty(e.Title)) valid.Add(e);
            return valid.Count > 0 ? valid : null;
        }
        catch { return null; }
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
                        if (!string.IsNullOrWhiteSpace(name)) _excluded[name.Trim()] = true;
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

// ----- RankService (source : scripts/RankService.cs) -----

public class RankService
{
    public int GetLiveRank(string username, List<UserProfile> allUsers)
    {
        allUsers.Sort((a, b) => {
            if (b.Level != a.Level) return b.Level.CompareTo(a.Level);
            if (b.Xp    != a.Xp)   return b.Xp.CompareTo(a.Xp);
            return b.WatchTime.CompareTo(a.WatchTime);
        });
        for (var i = 0; i < allUsers.Count; i++)
            if (string.Equals(allUsers[i].Username, username, StringComparison.OrdinalIgnoreCase))
                return i + 1;
        return 0;
    }

    public string FormatRankMessage(UserProfile user, int rank, XpProgress progress, int totalUsers, string title)
    {
        var displayName = !string.IsNullOrEmpty(user.DisplayName) ? user.DisplayName : user.Username;
        var xpRemaining = progress.XpForNext - progress.XpIntoLevel;
        var rankStr     = totalUsers > 0 ? "#" + rank + "/" + totalUsers : "#" + rank;
        var titlePart   = !string.IsNullOrEmpty(title) ? " · " + title : "";

        return "@" + displayName + titlePart
             + " | Rang " + rankStr
             + " | Niv." + user.Level
             + " | " + progress.XpIntoLevel + "/" + progress.XpForNext + " XP (" + xpRemaining + " restant)"
             + " | " + user.Messages + " messages"
             + " | " + FormatWatchTime(user.WatchTime) + " watchtime";
    }

    public string FormatNotFoundMessage(string displayName)
        => "@" + displayName + ", tu n'as pas encore de profil ! Chatte sur le stream pour commencer. PogChamp";

    private string FormatWatchTime(int totalMinutes)
    {
        if (totalMinutes <= 0) return "0m";
        if (totalMinutes < 60) return totalMinutes + "m";
        var h = totalMinutes / 60;
        var m = totalMinutes % 60;
        return m > 0 ? h + "h" + m.ToString("D2") + "m" : h + "h";
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
        // 1. Identité
        if (!args.ContainsKey("userName") || args["userName"] == null)
        {
            CPH.LogWarn("[RANK_ShowCommand] userName absent des args");
            return false;
        }

        var username    = args["userName"].ToString().Trim().ToLowerInvariant();
        var displayName = args.ContainsKey("userDisplayName") && args["userDisplayName"] != null
                              ? args["userDisplayName"].ToString().Trim()
                              : username;

        if (string.IsNullOrEmpty(username)) { CPH.LogWarn("[RANK_ShowCommand] userName vide"); return false; }

        // 2. Configuration + chemins
        var configPath  = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config      = new ConfigService().LoadConfig(configPath);
        var configDir   = Path.GetDirectoryName(configPath ?? "");
        var projectPath = Path.GetDirectoryName(configDir ?? "");

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[RANK_ShowCommand] dataPath non configuré");
            return false;
        }

        // 3. Vérification exclusion — silencieux pour les bots
        var bots = new BotExclusionService(
            projectPath,
            config.Bots.BroadcasterName,
            config.Bots.ExcludeBroadcaster == true);

        if (bots.IsExcluded(username))
        {
            CPH.SetArgument("rank_skipped",  true);
            CPH.SetArgument("rank_sent",     false);
            CPH.SetArgument("rank_notfound", false);
            CPH.SetArgument("rank_position", 0);
            return true;
        }

        // 4. Cooldown par utilisateur
        var cooldownKey = "rank_cooldown_" + username;
        var lastUse     = CPH.GetGlobalVar<long>(cooldownKey, false);
        var now         = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var elapsed     = now - lastUse;

        if (elapsed < config.Rank.CooldownSeconds)
        {
            CPH.SetArgument("rank_skipped",  true);
            CPH.SetArgument("rank_sent",     false);
            CPH.SetArgument("rank_notfound", false);
            CPH.SetArgument("rank_position", 0);
            return true;
        }

        // 5. Profil viewer
        var repo = new UserRepository(config.DataPath);
        var user = repo.LoadUser(username);

        if (user == null)
        {
            CPH.SendMessage(new RankService().FormatNotFoundMessage(displayName));
            CPH.SetGlobalVar(cooldownKey, now, false);
            CPH.SetArgument("rank_skipped",  false);
            CPH.SetArgument("rank_sent",     true);
            CPH.SetArgument("rank_notfound", true);
            CPH.SetArgument("rank_position", 0);
            CPH.SetArgument("rank_level",    0);
            CPH.SetArgument("rank_xp",       0);
            CPH.SetArgument("rank_messages", 0);
            CPH.SetArgument("rank_watchtime",0);
            CPH.SetArgument("rank_title",    "");
            return true;
        }

        // 6. Tous les profils — filtrés des exclusions pour le rang
        var allUsers = repo.GetAllUsers();
        var filtered = new List<UserProfile>();
        foreach (var u in allUsers)
            if (!bots.IsExcluded(u.Username))
                filtered.Add(u);

        var totalUsers   = filtered.Count;
        var rankService  = new RankService();
        var xpService    = new XpService();
        var titleService = new TitleService();

        var rank     = rankService.GetLiveRank(username, filtered);
        var progress = xpService.GetProgress(user);
        var title    = titleService.GetTitle(user.Level, projectPath, config.Theme);

        // 7. Message chat
        CPH.SendMessage(rankService.FormatRankMessage(user, rank, progress, totalUsers, title));
        CPH.SetGlobalVar(cooldownKey, now, false);

        // 8. Exposer
        CPH.SetArgument("rank_skipped",  false);
        CPH.SetArgument("rank_sent",     true);
        CPH.SetArgument("rank_notfound", false);
        CPH.SetArgument("rank_position", rank);
        CPH.SetArgument("rank_level",    user.Level);
        CPH.SetArgument("rank_xp",       user.Xp);
        CPH.SetArgument("rank_messages", user.Messages);
        CPH.SetArgument("rank_watchtime",user.WatchTime);
        CPH.SetArgument("rank_title",    title);

        CPH.LogInfo("[RANK_ShowCommand] " + displayName + " -> Rang #" + rank + "/" + totalUsers + " Niv." + user.Level + " (" + title + ")");

        return true;
    }
}
