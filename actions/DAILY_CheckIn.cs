using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

// ============================================================
// ACTION : DAILY_CheckIn
// ============================================================
// RÔLE : Check-in quotidien du viewer via Channel Point.
//         1 fois par jour, coche une case sur la carte de fidélité (10 cases).
//         Donne 10 XP par case, 100 XP quand la carte est complète.
//         Déclenche une animation sur l'overlay dédié check-in.
//         TotalCheckIns sert de hook pour les coffres V4.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "DAILY_CheckIn"
//   2. Déclencheur : Twitch → Channel Point Redemption → "Check-in"
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  Variable globale persistante
//
// ARGUMENTS ENTRANTS (fournis par SB automatiquement) :
//   args["userName"]        — login Twitch (minuscules)
//   args["userDisplayName"] — pseudo affiché
//
// ARGUMENTS SORTANTS :
//   %checkin_done%           bool   — true si check-in validé
//   %checkin_already_done%   bool   — true si déjà fait aujourd'hui
//   %checkin_xp%             int    — XP gagné (0 si refus)
//   %checkin_count%          int    — cases cochées sur la carte courante
//   %checkin_card_complete%  bool   — true si carte complète ce check-in
// ============================================================

// ----- UserRepository (source : scripts/UserRepository.cs) -----

// ============================================================
// UserRepository.cs — Streamer.bot XP System
// ============================================================
// UTILISATION DANS STREAMER.BOT :
//   Ce fichier est une référence source.
//   Coller UserProfile + UserRepository au-dessus de CPHInline
//   dans chaque action C# qui en a besoin.
// ============================================================


// Modèle utilisateur — structure exacte du JSON sur disque
// Champs alignés avec /data/users/{username}.json (voir README)
public class UserProfile
{
    public string Username             { get; set; }
    public string DisplayName          { get; set; }
    public int    Xp                   { get; set; }
    public int    Level                { get; set; }
    public int    Messages             { get; set; }
    public int    WatchTime            { get; set; }   // minutes cumulées regardées
    public long   LastMessageTimestamp { get; set; }
    public long   LastWatchTimestamp   { get; set; }   // Unix — dernier cycle watchtime reçu
    public int    WatchStreak          { get; set; }   // cycles consécutifs — bonus fidélité
    public int    XpFromChat             { get; set; }
    public int    XpFromWatch            { get; set; }
    public int    XpFromRewards          { get; set; }
    public float  ActiveBonusMultiplier  { get; set; } = 1.0f;
    public long   BonusExpiryTimestamp   { get; set; }
    public int    CheckInCount           { get; set; }
    public int    LastCheckInDay         { get; set; }
    public int    TotalCheckIns          { get; set; }
}

// Repository — lecture et écriture JSON uniquement
// Aucune logique XP, aucune logique overlay, aucun calcul
public class UserRepository
{
    private readonly string _dataPath;

    public UserRepository(string dataPath)
    {
        _dataPath = dataPath;
        Directory.CreateDirectory(_dataPath);
    }

    public bool UserExists(string username)
    {
        return File.Exists(GetFilePath(username));
    }

    public UserProfile CreateUser(string username, string displayName = null)
    {
        var user = new UserProfile
        {
            Username             = username,
            DisplayName          = displayName ?? username,
            Xp                   = 0,
            Level                = 1,
            Messages             = 0,
            WatchTime            = 0,
            LastMessageTimestamp = 0,
            LastWatchTimestamp   = 0,
            WatchStreak          = 0
        };

        SaveUser(user);
        return user;
    }

    public UserProfile LoadUser(string username)
    {
        var path = GetFilePath(username);

        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<UserProfile>(json);
    }

    public void SaveUser(UserProfile user)
    {
        var json    = JsonConvert.SerializeObject(user, Formatting.Indented);
        var path    = GetFilePath(user.Username);
        var tmpPath = path + ".tmp";

        try
        {
            File.WriteAllText(tmpPath, json);

            if (File.Exists(path))
                File.Delete(path);

            File.Move(tmpPath, path);
        }
        catch (Exception ex)
        {
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
            throw;
        }
    }

    public List<UserProfile> GetAllUsers()
    {
        var users = new List<UserProfile>();
        var files = Directory.GetFiles(_dataPath, "*.json");

        foreach (var file in files)
        {
            if (file.EndsWith(".tmp")) continue;
            try
            {
                var json = File.ReadAllText(file);
                var user = JsonConvert.DeserializeObject<UserProfile>(json);
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

    private string GetFilePath(string username)
    {
        return Path.Combine(_dataPath, username + ".json");
    }
}

// ----- XpService (source : scripts/XpService.cs) -----

// ============================================================
// XpService.cs — Streamer.bot XP System
// ============================================================
// RÈGLE ABSOLUE : toute modification XP passe par AddXp()
// Ce service ne déclenche jamais d'overlay, ne gère jamais OBS.
// ============================================================


// Résultat retourné par AddXp — consommé par l'action Streamer.bot pour les overlays
public class XpResult
{
    public string Username  { get; set; }
    public int    XpAdded   { get; set; }
    public int    TotalXp   { get; set; }
    public int    OldLevel  { get; set; }
    public int    NewLevel  { get; set; }
    public bool   IsLevelUp { get; set; }
}

// Progression XP dans le niveau actuel — profile cards, overlay barre XP
public class XpProgress
{
    public int   CurrentXp   { get; set; }
    public int   XpIntoLevel { get; set; }
    public int   XpForNext   { get; set; }
    public float Percentage  { get; set; }
}

// Entrée de leaderboard — overlay, export web, profile cards
public class LeaderboardEntry
{
    public int    Rank        { get; set; }
    public string Username    { get; set; }
    public string DisplayName { get; set; }
    public int    Xp          { get; set; }
    public int    Level       { get; set; }
    public int    WatchTime   { get; set; }
}

// GATEWAY XP — aucune logique overlay, aucune logique OBS
public class XpService
{
    private readonly UserRepository _repo;

    public XpService(UserRepository repo) { _repo = repo; }

    // GATEWAY PRINCIPAL — seule méthode autorisée à modifier le XP d'un utilisateur
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

    // GATEWAY WATCHTIME (V2) — variante optimisée pour les cycles watchtime
    public XpResult AddWatchTimeXp(UserProfile user, int amount, int intervalMinutes, long nowSeconds)
    {
        if (user == null) return null;

        var oldLevel             = user.Level;
        user.Xp                 += amount;
        user.Level               = CalculateLevel(user.Xp);
        user.WatchTime          += intervalMinutes;
        user.LastWatchTimestamp  = nowSeconds;
        user.XpFromWatch        += amount;
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

    public XpProgress GetProgress(UserProfile user)
    {
        var xpAtStart   = XpAtLevelStart(user.Level);
        var xpForNext   = XpForNextLevel(user.Level);
        var xpIntoLevel = user.Xp - xpAtStart;

        return new XpProgress
        {
            CurrentXp   = user.Xp,
            XpIntoLevel = xpIntoLevel,
            XpForNext   = xpForNext,
            Percentage  = xpForNext > 0 ? (float)xpIntoLevel / xpForNext * 100f : 0f
        };
    }

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
                Username    = u.Username,
                DisplayName = string.IsNullOrEmpty(u.DisplayName) ? u.Username : u.DisplayName,
                Xp          = u.Xp,
                Level       = u.Level,
                WatchTime   = u.WatchTime
            });
        }
        return result;
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

// ----- BotExclusionService (source : scripts/BotExclusionService.cs) -----

// ============================================================
// BotExclusionService.cs — Streamer.bot XP System
// ============================================================
// RESPONSABILITÉ :
//   Charger et vérifier la liste des comptes exclus du système XP.
// COMPARAISON : insensible à la casse (OrdinalIgnoreCase)
// AUCUNE logique XP — AUCUNE écriture disque — AUCUN overlay
// ============================================================


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
        _excluded["nightbot"]       = true;
        _excluded["streamelements"] = true;
        _excluded["streamlabs"]     = true;
        _excluded["moobot"]         = true;
        _excluded["fossabot"]       = true;
        _excluded["wizebot"]        = true;
        _excluded["mixitupbot"]     = true;
        _excluded["streamerbot"]    = true;
    }
}

// ----- ConfigService (source : scripts/ConfigService.cs) -----

// ============================================================
// ConfigService.cs — Streamer.bot XP System (V2)
// ============================================================
// AUCUNE logique métier — lecture et mapping uniquement
// ============================================================


// ----- Sous-sections de config.json -----

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

// ----- Racine de config.json -----

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

// ----- Chargeur de configuration -----

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
        // 1. Identité
        if (!args.ContainsKey("userName") || args["userName"] == null)
        {
            CPH.LogWarn("[DAILY_CheckIn] userName absent");
            return false;
        }

        var username    = args["userName"].ToString().Trim().ToLowerInvariant();
        var displayName = args.ContainsKey("userDisplayName") && args["userDisplayName"] != null
                              ? args["userDisplayName"].ToString().Trim()
                              : username;

        // 2. Configuration
        var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config     = new ConfigService(CPH).LoadConfig(configPath);

        if (config.CheckIn == null || config.CheckIn.Enabled != true)
        {
            CPH.LogInfo("[DAILY_CheckIn] Feature desactivee dans config.json");
            return true;
        }

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[DAILY_CheckIn] dataPath non configure");
            return false;
        }

        // 3. Exclusion bots
        var configDir   = Path.GetDirectoryName(configPath ?? "");
        var projectPath = Path.GetDirectoryName(configDir ?? "");
        var bots = new BotExclusionService(
            projectPath,
            config.Bots.BroadcasterName,
            config.Bots.ExcludeBroadcaster == true);

        if (bots.IsExcluded(username)) return true;

        // 4. Charger profil
        var repo = new UserRepository(config.DataPath);
        var user = repo.LoadUser(username);

        if (user == null)
        {
            CPH.SendMessage("@" + displayName + " — Tu n'as pas encore de profil ! Envoie un message dans le chat d'abord. PogChamp");
            CPH.SetArgument("checkin_done",          false);
            CPH.SetArgument("checkin_already_done",  false);
            CPH.SetArgument("checkin_xp",            0);
            CPH.SetArgument("checkin_count",         0);
            CPH.SetArgument("checkin_card_complete", false);
            return true;
        }

        // 5. Guard unicité jour
        var todayDay = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400);

        if (user.LastCheckInDay == todayDay)
        {
            CPH.SendMessage("@" + displayName + " — Check-in deja fait aujourd'hui ! Reviens demain. PauseChamp (" + user.CheckInCount + "/" + config.CheckIn.CardSize + " cases)");
            CPH.SetArgument("checkin_done",          false);
            CPH.SetArgument("checkin_already_done",  true);
            CPH.SetArgument("checkin_xp",            0);
            CPH.SetArgument("checkin_count",         user.CheckInCount);
            CPH.SetArgument("checkin_card_complete", false);
            return true;
        }

        // 6. Cocher une case
        user.CheckInCount++;
        user.TotalCheckIns++;
        user.LastCheckInDay = todayDay;

        var cardSize     = config.CheckIn.CardSize > 0 ? config.CheckIn.CardSize : 10;
        var cardComplete = user.CheckInCount >= cardSize;
        var xpGained     = cardComplete ? config.CheckIn.XpCardComplete : config.CheckIn.XpPerCheckin;

        if (cardComplete)
            user.CheckInCount = 0;

        // 7. Attribuer XP
        var xpService = new XpService(repo);
        var xpResult  = xpService.AddXp(user, xpGained, "reward");

        // 8. Message chat
        string message;
        if (cardComplete)
        {
            message = "@" + displayName + " — CARTE COMPLETE ! +" + xpGained + " XP bonus ! La carte repart a zero. SeemsGood";
        }
        else
        {
            var progress = user.CheckInCount + "/" + cardSize;
            message = "@" + displayName + " — Check-in ! +" + xpGained + " XP (" + progress + " cases) PogChamp";
        }

        CPH.SendMessage(message);

        // 9. Animation WebSocket overlay dédié
        var progressForOverlay = cardComplete ? cardSize : user.CheckInCount;
        CPH.WebsocketBroadcastJson(JsonConvert.SerializeObject(new Dictionary<string, object>
        {
            { "event",         cardComplete ? "checkIn_cardComplete" : "checkIn_normal" },
            { "username",      displayName },
            { "xpGained",      xpGained },
            { "count",         progressForOverlay },
            { "cardSize",      cardSize },
            { "totalCheckins", user.TotalCheckIns },
            { "durationMs",    config.CheckIn.AnimationDurationMs }
        }));

        CPH.LogInfo("[DAILY_CheckIn] " + username + " — +" + xpGained + " XP, case " + progressForOverlay + "/" + cardSize + (cardComplete ? " (carte complete!)" : ""));

        // 10. Exposer
        CPH.SetArgument("checkin_done",          true);
        CPH.SetArgument("checkin_already_done",  false);
        CPH.SetArgument("checkin_xp",            xpGained);
        CPH.SetArgument("checkin_count",         progressForOverlay);
        CPH.SetArgument("checkin_card_complete", cardComplete);

        return true;
    }
}
