using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

// ============================================================
// ACTION : REWARD_GrantXp
// ============================================================
// RÔLE : Octroyer un montant fixe d'XP à un viewer
//         suite au rachat d'un Channel Point.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "REWARD_GrantXp"
//   2. Déclencheur : Twitch → Channel Point Redemption → "Bonus XP"
//   3. Sub-Action 1 : Run Action → USER_GetOrCreate
//   4. Sub-Action 2 : Execute C# Code → coller ce fichier
//   5. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  Persistante : oui
//
// ARGUMENTS ENTRANTS :
//   args["user_username"]   — login Twitch (posé par USER_GetOrCreate)
//   args["user_excluded"]   — bool (posé par USER_GetOrCreate)
//
// ARGUMENTS SORTANTS :
//   %reward_xp_granted%   int    — XP octroyé
//   %reward_xp_total%     int    — XP total après ajout
//   %reward_isLevelUp%    bool   — true si montée de niveau
//   %reward_message%      string — message de confirmation envoyé
// ============================================================

// ----- UserRepository (source : scripts/UserRepository.cs) -----

// ============================================================
// UserRepository.cs — Streamer.bot XP System
// ============================================================

// Modèle utilisateur — structure exacte du JSON sur disque
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
    public int    WatchStreak          { get; set; }
    public int    XpFromChat             { get; set; }
    public int    XpFromWatch            { get; set; }
    public int    XpFromRewards          { get; set; }
    public float  ActiveBonusMultiplier  { get; set; } = 1.0f;
    public long   BonusExpiryTimestamp   { get; set; }
    public int    CheckInCount           { get; set; }
    public int    LastCheckInDay         { get; set; }
    public int    TotalCheckIns          { get; set; }
}

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
        var json    = JsonConvert.SerializeObject(user, Formatting.Indented);
        var path    = Path.Combine(_dataPath, user.Username + ".json");
        var tmpPath = path + ".tmp";

        try
        {
            File.WriteAllText(tmpPath, json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmpPath, path);
        }
        catch (Exception ex)
        {
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
            throw;
        }
    }
}

// ----- XpService (source : scripts/XpService.cs) -----

// ============================================================
// XpService.cs — Streamer.bot XP System
// ============================================================

public class XpResult
{
    public string Username  { get; set; }
    public int    XpAdded   { get; set; }
    public int    TotalXp   { get; set; }
    public int    OldLevel  { get; set; }
    public int    NewLevel  { get; set; }
    public bool   IsLevelUp { get; set; }
}

public class XpService
{
    private readonly UserRepository _repo;

    public XpService(UserRepository repo) { _repo = repo; }

    // source : "chat" | "watchtime" | "reward"
    public XpResult AddXp(UserProfile user, int amount, string source)
    {
        if (user == null) return null;

        var oldLevel = user.Level;
        user.Xp     += amount;
        user.Level   = CalculateLevel(user.Xp);

        if (source == "chat")
        {
            user.Messages++;
            user.LastMessageTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            user.XpFromChat          += amount;
        }
        else if (source == "watchtime") { user.XpFromWatch   += amount; }
        else if (source == "reward")    { user.XpFromRewards  += amount; }

        _repo.SaveUser(user);

        return new XpResult
        {
            Username  = user.Username,
            XpAdded   = amount,
            TotalXp   = user.Xp,
            OldLevel  = oldLevel,
            NewLevel  = user.Level,
            IsLevelUp = user.Level > oldLevel
        };
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

// ----- ConfigService (source : scripts/ConfigService.cs) -----

// ============================================================
// ConfigService.cs — Streamer.bot XP System (V2)
// ============================================================

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

public class RewardsConfig
{
    public bool? BonusXpEnabled         { get; set; }
    public float BonusXpMultiplier      { get; set; }
    public int   BonusXpDurationMinutes { get; set; }
    public bool? GrantXpEnabled         { get; set; }
    public int   GrantXpAmount          { get; set; }
}

public class CheckInConfig
{
    public bool   Enabled             { get; set; }
    public string ChannelPointName    { get; set; }
    public int    XpPerCheckin        { get; set; }
    public int    XpCardComplete      { get; set; }
    public int    CardSize            { get; set; }
    public int    AnimationDurationMs { get; set; }
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
    public RewardsConfig     Rewards     { get; set; }
    public CheckInConfig     CheckIn     { get; set; }
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

        if (c.Rewards == null) c.Rewards = new RewardsConfig();
        if (!c.Rewards.BonusXpEnabled.HasValue)      c.Rewards.BonusXpEnabled         = true;
        if (c.Rewards.BonusXpMultiplier      <= 0)   c.Rewards.BonusXpMultiplier      = 2.0f;
        if (c.Rewards.BonusXpDurationMinutes <= 0)   c.Rewards.BonusXpDurationMinutes = 30;
        if (!c.Rewards.GrantXpEnabled.HasValue)      c.Rewards.GrantXpEnabled          = true;
        if (c.Rewards.GrantXpAmount          <= 0)   c.Rewards.GrantXpAmount           = 100;

        if (c.CheckIn == null) c.CheckIn = new CheckInConfig();
        if (string.IsNullOrEmpty(c.CheckIn.ChannelPointName)) c.CheckIn.ChannelPointName    = "Check-in";
        if (c.CheckIn.XpPerCheckin        <= 0)               c.CheckIn.XpPerCheckin        = 10;
        if (c.CheckIn.XpCardComplete      <= 0)               c.CheckIn.XpCardComplete      = 100;
        if (c.CheckIn.CardSize            <= 0)               c.CheckIn.CardSize            = 10;
        if (c.CheckIn.AnimationDurationMs <= 0)               c.CheckIn.AnimationDurationMs = 5000;
    }
}

// ----- Action Streamer.bot -----

public class CPHInline
{
    public bool Execute()
    {
        // 1. Vérifier exclusion
        var excluded = args.ContainsKey("user_excluded")
                       && args["user_excluded"] != null
                       && (bool)args["user_excluded"] == true;
        if (excluded) return true;

        // 2. Username
        if (!args.ContainsKey("user_username") || args["user_username"] == null)
        {
            CPH.LogWarn("[REWARD_GrantXp] user_username absent — USER_GetOrCreate requis");
            return false;
        }
        var username = args["user_username"].ToString();

        // 3. Configuration
        var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config     = new ConfigService(CPH).LoadConfig(configPath);

        if (config.Rewards.GrantXpEnabled != true)
        {
            CPH.LogInfo("[REWARD_GrantXp] Feature desactivee dans config.json");
            CPH.SetArgument("reward_xp_granted", 0);
            return true;
        }

        // 4. Charger profil
        var repo = new UserRepository(config.DataPath);
        var user = repo.LoadUser(username);

        if (user == null)
        {
            CPH.LogWarn("[REWARD_GrantXp] Profil introuvable pour '" + username + "'");
            return false;
        }

        // 5. Octroyer XP
        var xpService = new XpService(repo);
        var xpResult  = xpService.AddXp(user, config.Rewards.GrantXpAmount, "reward");

        // 6. Message chat
        var displayName = string.IsNullOrEmpty(user.DisplayName) ? username : user.DisplayName;
        var message     = "@" + displayName + " — Bonus XP ! +" + config.Rewards.GrantXpAmount + " XP PogChamp";

        CPH.SendMessage(message);
        CPH.LogInfo("[REWARD_GrantXp] " + username + " — +" + config.Rewards.GrantXpAmount + " XP");

        // 7. Exposer
        CPH.SetArgument("reward_xp_granted", xpResult.XpAdded);
        CPH.SetArgument("reward_xp_total",   xpResult.TotalXp);
        CPH.SetArgument("reward_isLevelUp",  xpResult.IsLevelUp);
        CPH.SetArgument("reward_message",    message);

        return true;
    }
}
