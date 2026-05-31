// ============================================================
// ACTION : XP_WatchTime_V2
// ============================================================
// RÔLE : Distribuer l'XP watchtime via le trigger Present Viewers.
//         Système hybride : présence SB chatters + activité chat.
//         Les comptes exclus (bots) sont ignorés.
//         Action silencieuse — aucun overlay déclenché.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "XP_WatchTime_V2"
//   2. Déclencheur : Twitch → General → Present Viewers
//      Activer "Live Update" (cocher la case)
//      Intervalle : 5 minutes
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//   ⚠ Remplace XP_WatchTime (Timer) — désactiver l'ancienne action
//
// PRÉREQUIS :
//   xp_configPath  string  Persistante : oui
//
// ARGUMENTS ENTRANTS (fournis par le trigger Present Viewers) :
//   args["isLive"]  bool                             — stream en live
//   args["isTest"]  bool                             — test manuel SB
//   args["users"]   List<Dictionary<string,object>>  — chatters présents
//
// ARGUMENTS SORTANTS :
//   %watch_processed%        int  — viewers traités ce cycle
//   %watch_xp_distributed%   int  — XP total distribué
//   %watch_level_ups%        int  — level-ups détectés
//   %watch_skipped%          int  — viewers ignorés (exclus / inéligibles)
//   %watch_presence_count%   int  — chatters dans la liste SB
//
// ÉLIGIBILITÉ HYBRIDE :
//   Un viewer est servi s'il remplit au moins une condition :
//   • Présent dans la liste SB chatters (Live Update)
//   • A chatté dans les 2× intervalles précédents (LastMessageTimestamp)
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
        // 1. Lire les args du trigger
        object rawIsLive;
        var isLive = args.TryGetValue("isLive", out rawIsLive)
                     && rawIsLive != null
                     && Convert.ToBoolean(rawIsLive);

        object rawIsTest;
        var isTest = args.TryGetValue("isTest", out rawIsTest)
                     && rawIsTest != null
                     && Convert.ToBoolean(rawIsTest);

        // 2. Configuration + chemins (avant le guard offline pour accéder à countOffline)
        var configPath  = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config      = new ConfigService().LoadConfig(configPath);
        var configDir   = Path.GetDirectoryName(configPath ?? "");
        var projectPath = Path.GetDirectoryName(configDir ?? "");

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[XP_WatchTime_V2] dataPath non configuré");
            return false;
        }

        // 3. Guard offline — config.Watchtime.CountOffline permet d'accumuler hors live
        if (!isLive && !isTest && config.Watchtime.CountOffline != true)
        {
            CPH.LogInfo("[XP_WatchTime_V2] Stream offline — cycle ignoré");
            CPH.SetArgument("watch_processed",      0);
            CPH.SetArgument("watch_xp_distributed", 0);
            CPH.SetArgument("watch_level_ups",      0);
            CPH.SetArgument("watch_skipped",        0);
            CPH.SetArgument("watch_presence_count", 0);
            return true;
        }

        if (config.Watchtime.Enabled == false)
        {
            CPH.LogInfo("[XP_WatchTime_V2] Watchtime désactivé dans config.json");
            return true;
        }

        // 3. Construire le HashSet de présence depuis la liste SB
        var presentSet = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        object rawUsers;
        if (args.TryGetValue("users", out rawUsers) && rawUsers != null)
        {
            var sbList = rawUsers as List<Dictionary<string, object>>;

            // Fallback : re-sérialiser si le cast direct échoue (type interne SB)
            if (sbList == null)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(rawUsers);
                    sbList   = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                }
                catch { }
            }

            if (sbList != null)
            {
                foreach (var u in sbList)
                {
                    var login = ExtractUsername(u);
                    if (!string.IsNullOrEmpty(login))
                        presentSet[login] = true;
                }
            }
            else
            {
                CPH.LogDebug("[XP_WatchTime_V2] Type inattendu pour args[users] : " + rawUsers.GetType().Name + " — mode activité seule");
            }
        }
        else
        {
            CPH.LogDebug("[XP_WatchTime_V2] args[users] absent — mode activité seule");
        }

        // 4. Services
        var bots         = new BotExclusionService(projectPath, config.Bots.BroadcasterName, config.Bots.ExcludeBroadcaster == true);
        var repo         = new UserRepository(config.DataPath);
        var xpService    = new XpService(repo);
        var watchService = new WatchTimeService();

        var now                   = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var interval              = config.Watchtime.IntervalMinutes;
        var xpAmount              = config.Xp.PerWatchInterval;
        var activityWindowSeconds = (long)(interval * 60 * 2);
        var streakEnabled         = config.Watchtime.StreakEnabled == true;

        var allUsers = repo.GetAllUsers();

        CPH.LogInfo("[XP_WatchTime_V2] Cycle start — "
            + allUsers.Count + " profils, "
            + presentSet.Count + " présents SB"
            + (isTest ? " [TEST]" : ""));

        if (allUsers.Count == 0)
        {
            CPH.LogInfo("[XP_WatchTime_V2] Aucun profil trouvé — cycle terminé");
            CPH.SetArgument("watch_processed",      0);
            CPH.SetArgument("watch_xp_distributed", 0);
            CPH.SetArgument("watch_level_ups",      0);
            CPH.SetArgument("watch_skipped",        0);
            CPH.SetArgument("watch_presence_count", presentSet.Count);
            return true;
        }

        var processed = 0; var xpTotal = 0; var levelUps = 0; var skipped = 0;

        // 5. Boucle principale
        foreach (var user in allUsers)
        {
            // a. Bots exclus en premier — avant toute autre logique
            if (bots.IsExcluded(user.Username)) { skipped++; continue; }

            // b. Cooldown anti-double : 1 seule attribution par cycle
            if (!watchService.IsEligible(user, interval, now)) { skipped++; continue; }

            // c. Éligibilité hybride — au moins une source suffit
            var inPresenceList = presentSet.ContainsKey(user.Username);
            var recentChat     = (now - user.LastMessageTimestamp) <= activityWindowSeconds;

            if (!inPresenceList && !recentChat) { skipped++; continue; }

            // d. WatchStreak — consécutivité des cycles
            if (streakEnabled)
            {
                var prevGap = now - user.LastWatchTimestamp;
                var maxGap  = activityWindowSeconds;
                user.WatchStreak = (user.LastWatchTimestamp > 0 && prevGap <= maxGap)
                                   ? user.WatchStreak + 1
                                   : 1;
            }

            // e. Attribution XP + watchtime (WatchStreak déjà mis à jour sur l'objet)
            var bonus  = streakEnabled ? StreakBonus(user.WatchStreak) : 0;
            var result = xpService.AddWatchTimeXp(user, xpAmount + bonus, interval, now);
            if (result == null) { skipped++; continue; }

            processed++;
            xpTotal += result.XpAdded;

            if (result.IsLevelUp)
            {
                levelUps++;
                CPH.LogInfo("[XP_WatchTime_V2] LEVEL UP ! "
                    + user.Username + " : Niv. " + result.OldLevel + " -> " + result.NewLevel);
            }
        }

        // 6. Exposer résultats
        CPH.SetArgument("watch_processed",      processed);
        CPH.SetArgument("watch_xp_distributed", xpTotal);
        CPH.SetArgument("watch_level_ups",      levelUps);
        CPH.SetArgument("watch_skipped",        skipped);
        CPH.SetArgument("watch_presence_count", presentSet.Count);

        CPH.LogInfo("[XP_WatchTime_V2] Cycle terminé — "
            + processed + " traités, "
            + xpTotal   + " XP, "
            + levelUps  + " level-ups, "
            + skipped   + " ignorés, "
            + presentSet.Count + " présents SB");

        return true;
    }

    // Tente plusieurs clés connues pour extraire le login depuis le dict SB
    private static string ExtractUsername(Dictionary<string, object> user)
    {
        var keys = new string[] { "login", "userName", "username", "name", "user" };
        foreach (var key in keys)
        {
            object val;
            if (user.TryGetValue(key, out val) && val != null)
            {
                var s = val.ToString().Trim();
                if (!string.IsNullOrEmpty(s)) return s.ToLowerInvariant();
            }
        }
        return null;
    }

    // Bonus XP progressif basé sur la consécutivité des cycles (streak)
    private static int StreakBonus(int streak)
    {
        if (streak >= 24) return 5;   // 2h+ consécutif
        if (streak >= 12) return 3;   // 1h+ consécutif
        if (streak >= 6)  return 2;   // 30 min+ consécutif
        if (streak >= 3)  return 1;   // 15 min+ consécutif
        return 0;
    }
}
