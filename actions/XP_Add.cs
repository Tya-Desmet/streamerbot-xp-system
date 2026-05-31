// ============================================================
// ACTION : XP_Add
// ============================================================
// RÔLE : Orchestrateur du pipeline XP chat Twitch.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "XP_Add"
//   2. Déclencheur : Twitch → Chat Message
//   3. Sub-Action 1 : Run Action → USER_GetOrCreate
//   4. Sub-Action 2 : Execute C# Code → coller ce fichier
//   5. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  ex: C:\...\streamerbot-xp-system\configs\config.json
//   Persistante : oui
//
// ARGUMENTS ENTRANTS :
//   args["user_username"]  — login Twitch (posé par USER_GetOrCreate)
//   args["user_excluded"]  — bool (posé par USER_GetOrCreate)
//   args["rawInput"]       — message brut du chat
//
// ARGUMENTS SORTANTS :
//   %xp_skipped%     bool   — true si ignoré
//   %xp_skipReason%  string — "excluded" | "command" | "too_short" | "cooldown" | ""
//   %xp_added%       int    — XP ajouté
//   %xp_total%       int    — XP total après ajout
//   %xp_oldLevel%    int    — niveau avant
//   %xp_newLevel%    int    — niveau après
//   %xp_isLevelUp%   bool   — true si montée de niveau
//   %xp_xpIntoLevel% int    — XP dans le niveau actuel
//   %xp_xpForNext%   int    — XP requis pour ce niveau
//   %xp_percentage%  float  — % de progression
//
// AUCUNE logique overlay — AUCUNE logique leaderboard
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

    public void SaveUser(UserProfile user)
    {
        File.WriteAllText(
            Path.Combine(_dataPath, user.Username + ".json"),
            JsonConvert.SerializeObject(user, Formatting.Indented));
    }
}

// ----- ValidationService (source : scripts/ValidationService.cs) -----

public class ValidationResult
{
    public bool   IsValid { get; set; }
    public string Reason  { get; set; }
}

public class ValidationService
{
    private readonly IInlineInvokeProxy _CPH;
    private readonly int _cooldownSeconds;
    private readonly int _minLength;

    public ValidationService(IInlineInvokeProxy CPH, int cooldownSeconds = 30, int minLength = 2)
    {
        _CPH             = CPH;
        _cooldownSeconds = cooldownSeconds;
        _minLength       = minLength;
    }

    public ValidationResult ValidateMessage(string username, string message)
    {
        if (IsCommand(message))     return Reject("command");
        if (IsTooShort(message))    return Reject("too_short");
        if (IsOnCooldown(username)) return Reject("cooldown");
        SetCooldown(username);
        return Accept();
    }

    private bool IsCommand(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return true;
        var first = message.TrimStart()[0];
        return first == '!' || first == '/' || first == '.';
    }

    private bool IsTooShort(string message) => message.Trim().Length < _minLength;

    private bool IsOnCooldown(string username)
    {
        var last = _CPH.GetGlobalVar<long>("cooldown_" + username, false);
        return (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - last) < _cooldownSeconds;
    }

    private void SetCooldown(string username)
        => _CPH.SetGlobalVar("cooldown_" + username, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), false);

    private ValidationResult Accept()         => new ValidationResult { IsValid = true,  Reason = "" };
    private ValidationResult Reject(string r) => new ValidationResult { IsValid = false, Reason = r };
}

// ----- XpService (source : scripts/XpService.cs) -----

public class XpResult
{
    public int  XpAdded   { get; set; }
    public int  TotalXp   { get; set; }
    public int  OldLevel  { get; set; }
    public int  NewLevel  { get; set; }
    public bool IsLevelUp { get; set; }
}

public class XpProgress
{
    public int   XpIntoLevel { get; set; }
    public int   XpForNext   { get; set; }
    public float Percentage  { get; set; }
}

public class XpService
{
    private readonly UserRepository _repo;

    public XpService(UserRepository repo) { _repo = repo; }

    public XpResult AddXp(string username, int amount)
    {
        var user = _repo.LoadUser(username);
        if (user == null) return null;

        var oldLevel = user.Level;
        user.Xp   += amount;
        user.Level = CalculateLevel(user.Xp);
        _repo.SaveUser(user);

        return new XpResult
        {
            XpAdded   = amount,
            TotalXp   = user.Xp,
            OldLevel  = oldLevel,
            NewLevel  = user.Level,
            IsLevelUp = user.Level > oldLevel
        };
    }

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

    private int CalculateLevel(int totalXp)
    {
        var level = 1; var acc = 0;
        while (true)
        {
            var threshold = XpForNextLevel(level);
            if (acc + threshold > totalXp) break;
            acc += threshold; level++;
        }
        return level;
    }

    private int XpAtLevelStart(int level)
    {
        var acc = 0;
        for (var l = 1; l < level; l++) acc += XpForNextLevel(l);
        return acc;
    }

    private int XpForNextLevel(int level) => (int)(100 * Math.Pow(level, 1.5));
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
        // 1. Vérifier le flag d'exclusion posé par USER_GetOrCreate
        var excluded = args.ContainsKey("user_excluded")
                       && args["user_excluded"] != null
                       && (bool)args["user_excluded"] == true;

        if (excluded)
        {
            CPH.SetArgument("xp_skipped",    true);
            CPH.SetArgument("xp_skipReason", "excluded");
            return true;
        }

        // 2. Username posé par USER_GetOrCreate
        if (!args.ContainsKey("user_username") || args["user_username"] == null)
        {
            CPH.LogWarn("[XP_Add] user_username absent — USER_GetOrCreate doit être la sub-action précédente");
            return false;
        }

        var username = args["user_username"].ToString();

        // 3. Message brut
        if (!args.ContainsKey("rawInput") || args["rawInput"] == null)
        {
            CPH.LogWarn("[XP_Add] rawInput absent — vérifier le déclencheur Twitch Chat Message");
            return false;
        }

        var rawInput = args["rawInput"].ToString();

        // 4. Configuration
        var configPath   = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config       = new ConfigService().LoadConfig(configPath);

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[XP_Add] dataPath non configuré dans configs/config.json");
            return false;
        }

        // 5. Validation anti-spam
        var validation = new ValidationService(CPH, config.Xp.CooldownSeconds, config.Xp.MinMessageLength);
        var check      = validation.ValidateMessage(username, rawInput);

        if (!check.IsValid)
        {
            CPH.SetArgument("xp_skipped",    true);
            CPH.SetArgument("xp_skipReason", check.Reason);
            return true;
        }

        // 6. Ajout XP via gateway
        var repo      = new UserRepository(config.DataPath);
        var xpService = new XpService(repo);
        var xpResult  = xpService.AddXp(username, config.Xp.PerMessage);

        if (xpResult == null)
        {
            CPH.LogWarn("[XP_Add] Profil introuvable pour '" + username + "' malgré USER_GetOrCreate");
            return false;
        }

        // 7. Incrémenter Messages + timestamp
        var user = repo.LoadUser(username);
        user.Messages++;
        user.LastMessageTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        repo.SaveUser(user);

        // 8. Progression XP dans le niveau
        var progress = xpService.GetProgress(user);

        // 9. Exposer les résultats
        CPH.SetArgument("xp_skipped",     false);
        CPH.SetArgument("xp_skipReason",  "");
        CPH.SetArgument("xp_added",       xpResult.XpAdded);
        CPH.SetArgument("xp_total",       xpResult.TotalXp);
        CPH.SetArgument("xp_oldLevel",    xpResult.OldLevel);
        CPH.SetArgument("xp_newLevel",    xpResult.NewLevel);
        CPH.SetArgument("xp_isLevelUp",   xpResult.IsLevelUp);
        CPH.SetArgument("xp_xpIntoLevel", progress.XpIntoLevel);
        CPH.SetArgument("xp_xpForNext",   progress.XpForNext);
        CPH.SetArgument("xp_percentage",  progress.Percentage);

        if (xpResult.IsLevelUp)
            CPH.LogInfo("[XP_Add] LEVEL UP ! " + username + " : Niv. " + xpResult.OldLevel + " -> " + xpResult.NewLevel);

        return true;
    }
}
