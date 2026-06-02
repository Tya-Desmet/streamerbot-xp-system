using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

// ============================================================
// ACTION : REWARD_BonusXp
// ============================================================
// RÔLE : Activer le multiplicateur XP temporaire pour un viewer
//         suite au rachat d'un Channel Point.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "REWARD_BonusXp"
//   2. Déclencheur : Twitch → Channel Point Redemption
//      Nom du reward : "Double XP" (configurable)
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
//   %reward_bonus_applied%         bool   — true si bonus activé avec succès
//   %reward_multiplier%            float  — multiplicateur appliqué (ex: 2.0)
//   %reward_expires_in_minutes%    int    — durée restante en minutes
//   %reward_was_already_active%    bool   — true si bonus remplacé
//   %reward_message%               string — message de confirmation envoyé
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

// ----- RewardService (source : scripts/RewardService.cs) -----

// ============================================================
// RewardService.cs — Streamer.bot XP System
// ============================================================
// RESPONSABILITÉ :
//   Gérer le cycle de vie du multiplicateur XP temporaire.
//   Ne lit et n'écrit jamais le disque directement.
//   Délègue la persistance à l'action appelante.
//
// UTILISATION :
//   var rewards = new RewardService();
//   var multiplier = rewards.GetCurrentMultiplier(user, now);
//   var result = rewards.ApplyBonus(user, 2.0f, 30, now);
//   repo.SaveUser(user); // ← sauvegarder APRÈS ApplyBonus
//
// AUCUNE logique XP — AUCUNE écriture disque — AUCUN overlay
// ============================================================

public class BonusResult
{
    public float MultiplierApplied { get; set; }
    public long  ExpiresAt         { get; set; }
    public bool  WasAlreadyActive  { get; set; }
    public int   ExpiresInMinutes  { get; set; }
}

public class RewardService
{
    // Retourne le multiplicateur actif.
    // Si le bonus est expiré, remet le profil à 1.0 (lazy cleanup) — NE SAUVEGARDE PAS.
    // L'action appelante doit sauvegarder si le profil a été modifié.
    public float GetCurrentMultiplier(UserProfile user, long nowSeconds)
    {
        if (user == null) return 1.0f;
        if (user.BonusExpiryTimestamp <= 0) return 1.0f;
        if (nowSeconds >= user.BonusExpiryTimestamp)
        {
            user.ActiveBonusMultiplier = 1.0f;
            user.BonusExpiryTimestamp  = 0;
            return 1.0f;
        }
        return user.ActiveBonusMultiplier > 1.0f ? user.ActiveBonusMultiplier : 1.0f;
    }

    public bool IsBonusActive(UserProfile user, long nowSeconds)
    {
        if (user == null) return false;
        return user.ActiveBonusMultiplier > 1.0f
            && user.BonusExpiryTimestamp > 0
            && nowSeconds < user.BonusExpiryTimestamp;
    }

    // Active un multiplicateur sur le profil. Ne sauvegarde pas.
    // L'action appelante doit appeler SaveUser() après.
    public BonusResult ApplyBonus(UserProfile user, float multiplier, int durationMinutes, long nowSeconds)
    {
        var wasActive = IsBonusActive(user, nowSeconds);
        user.ActiveBonusMultiplier = multiplier;
        user.BonusExpiryTimestamp  = nowSeconds + (long)(durationMinutes * 60);

        var minutesRemaining = (int)((user.BonusExpiryTimestamp - nowSeconds) / 60);

        return new BonusResult
        {
            MultiplierApplied = multiplier,
            ExpiresAt         = user.BonusExpiryTimestamp,
            WasAlreadyActive  = wasActive,
            ExpiresInMinutes  = minutesRemaining
        };
    }

    public int GetMinutesRemaining(UserProfile user, long nowSeconds)
    {
        if (!IsBonusActive(user, nowSeconds)) return 0;
        var seconds = user.BonusExpiryTimestamp - nowSeconds;
        return seconds > 0 ? (int)(seconds / 60) : 0;
    }
}

// ----- ConfigService (source : scripts/ConfigService.cs) -----

// ============================================================
// ConfigService.cs — Streamer.bot XP System (V2)
// ============================================================
// UTILISATION DANS STREAMER.BOT :
//   Coller tous les types Config + ConfigService au-dessus de CPHInline.
//   Une seule Global Variable à définir dans Streamer.bot :
//
//     Nom     : xp_configPath
//     Valeur  : C:\Users\TonNom\streamerbot-xp-system\configs\config.json
//     Persist : oui
//
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
        // 1. Vérifier exclusion
        var excluded = args.ContainsKey("user_excluded")
                       && args["user_excluded"] != null
                       && (bool)args["user_excluded"] == true;
        if (excluded) return true;

        // 2. Username
        if (!args.ContainsKey("user_username") || args["user_username"] == null)
        {
            CPH.LogWarn("[REWARD_BonusXp] user_username absent — USER_GetOrCreate requis");
            return false;
        }
        var username = args["user_username"].ToString();

        // 3. Configuration
        var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config     = new ConfigService(CPH).LoadConfig(configPath);

        if (config.Rewards.BonusXpEnabled != true)
        {
            CPH.LogInfo("[REWARD_BonusXp] Feature desactivee dans config.json");
            CPH.SetArgument("reward_bonus_applied", false);
            return true;
        }

        // 4. Charger profil
        var repo = new UserRepository(config.DataPath);
        var user = repo.LoadUser(username);

        if (user == null)
        {
            CPH.LogWarn("[REWARD_BonusXp] Profil introuvable pour '" + username + "'");
            return false;
        }

        // 5. Appliquer le bonus
        var rewards = new RewardService();
        var now     = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var result  = rewards.ApplyBonus(
            user,
            config.Rewards.BonusXpMultiplier,
            config.Rewards.BonusXpDurationMinutes,
            now);

        repo.SaveUser(user);

        // 6. Message chat
        var displayName = string.IsNullOrEmpty(user.DisplayName) ? username : user.DisplayName;
        var message     = result.WasAlreadyActive
            ? "@" + displayName + " — Double XP prolonge ! x" + result.MultiplierApplied + " pendant encore " + result.ExpiresInMinutes + " min"
            : "@" + displayName + " — Double XP active ! x" + result.MultiplierApplied + " pendant " + result.ExpiresInMinutes + " min";

        CPH.SendMessage(message);
        CPH.LogInfo("[REWARD_BonusXp] " + username + " — x" + result.MultiplierApplied + " pendant " + result.ExpiresInMinutes + " min");

        // 7. Exposer
        CPH.SetArgument("reward_bonus_applied",      true);
        CPH.SetArgument("reward_multiplier",         result.MultiplierApplied);
        CPH.SetArgument("reward_expires_in_minutes", result.ExpiresInMinutes);
        CPH.SetArgument("reward_was_already_active", result.WasAlreadyActive);
        CPH.SetArgument("reward_message",            message);

        return true;
    }
}
