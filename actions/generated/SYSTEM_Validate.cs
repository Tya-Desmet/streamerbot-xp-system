using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

// ============================================================
// ACTION : SYSTEM_Validate
// ============================================================
// RÔLE : Vérifier l'intégrité de l'installation et afficher
//         un rapport complet dans les logs Streamer.bot.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "SYSTEM_Validate"
//   2. Aucun trigger automatique — déclencher manuellement
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//
// USAGE :
//   Clic droit sur l'action → Test
//   Vérifier les logs dans la console SB
//
// PRÉREQUIS :
//   xp_configPath  string  Persistante : oui  (peut être absent — l'action le détecte)
// ============================================================

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
//   Puis dans Execute() :
//     var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);
//     var config     = new ConfigService().LoadConfig(configPath);
//     // config.DataPath, config.Xp.PerMessage, config.Bots.BroadcasterName...
//
// STRUCTURE JSON :
//   config.json utilise des sections imbriquées (nested).
//   Newtonsoft.Json mappe automatiquement PascalCase C# ↔ camelCase JSON.
//   Toute section absente du JSON est reconstruite avec les valeurs par défaut.
//
// FALLBACK :
//   Si le fichier est absent ou malformé, LoadConfig retourne les valeurs
//   par défaut sans lever d'exception. Les actions continuent normalement.
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
        CPH.LogInfo("=== SYSTEM_Validate : demarrage ===");

        var ok = true;

        // 1. Vérifier xp_configPath
        var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);

        if (string.IsNullOrEmpty(configPath))
        {
            CPH.LogWarn("[SYSTEM] FAIL — xp_configPath non defini dans les GlobalVars SB");
            CPH.LogWarn("[SYSTEM] Action requise : Settings → Global Variables → Ajouter xp_configPath");
            ok = false;
        }
        else
        {
            CPH.LogInfo("[SYSTEM] OK   — xp_configPath : " + configPath);
        }

        // 2. Vérifier que config.json existe
        if (!string.IsNullOrEmpty(configPath))
        {
            if (!File.Exists(configPath))
            {
                CPH.LogWarn("[SYSTEM] FAIL — config.json introuvable : " + configPath);
                ok = false;
            }
            else
            {
                CPH.LogInfo("[SYSTEM] OK   — config.json existe");
            }
        }

        // 3. Vérifier que config.json est valide JSON
        Config config = null;
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                config   = JsonConvert.DeserializeObject<Config>(json);
                if (config == null) throw new Exception("Deserialisation retourne null");
                CPH.LogInfo("[SYSTEM] OK   — config.json valide (JSON correct)");
            }
            catch (Exception ex)
            {
                CPH.LogWarn("[SYSTEM] FAIL — config.json JSON invalide : " + ex.Message);
                ok = false;
            }
        }

        // 4. Vérifier dataPath
        if (config != null)
        {
            if (string.IsNullOrEmpty(config.DataPath))
            {
                CPH.LogWarn("[SYSTEM] FAIL — dataPath non configure dans config.json");
                ok = false;
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(config.DataPath);
                    CPH.LogInfo("[SYSTEM] OK   — dataPath accessible : " + config.DataPath);

                    var profileCount = Directory.GetFiles(config.DataPath, "*.json").Length;
                    CPH.LogInfo("[SYSTEM] INFO — " + profileCount + " profil(s) dans dataPath");
                }
                catch (Exception ex)
                {
                    CPH.LogWarn("[SYSTEM] FAIL — dataPath inaccessible : " + ex.Message);
                    ok = false;
                }
            }
        }

        // 5. Vérifier excluded-users.json
        if (!string.IsNullOrEmpty(configPath))
        {
            var configDir    = Path.GetDirectoryName(configPath ?? "");
            var projectPath  = Path.GetDirectoryName(configDir ?? "");
            var excludedPath = Path.Combine(projectPath, "configs", "excluded-users.json");

            if (File.Exists(excludedPath))
            {
                try
                {
                    var list  = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(excludedPath));
                    var count = (list != null) ? list.Count : 0;
                    CPH.LogInfo("[SYSTEM] OK   — excluded-users.json : " + count + " entree(s)");
                    if (count == 0)
                        CPH.LogWarn("[SYSTEM] WARN — excluded-users.json vide → fallback bots standard");
                }
                catch (Exception ex)
                {
                    CPH.LogWarn("[SYSTEM] WARN — excluded-users.json invalide : " + ex.Message);
                }
            }
            else
            {
                CPH.LogWarn("[SYSTEM] WARN — excluded-users.json absent → fallback bots standard");
            }

            // 6. Vérifier titles.json
            var titlesPath = Path.Combine(projectPath, "configs", "titles.json");
            if (File.Exists(titlesPath))
            {
                try
                {
                    var titles = JsonConvert.DeserializeObject<List<object>>(File.ReadAllText(titlesPath));
                    var count  = (titles != null) ? titles.Count : 0;
                    if (count == 0)
                        CPH.LogWarn("[SYSTEM] WARN — configs/titles.json vide → themes/default/titles.json sera utilise");
                    else
                        CPH.LogInfo("[SYSTEM] OK   — configs/titles.json : " + count + " palier(s)");
                }
                catch (Exception ex)
                {
                    CPH.LogWarn("[SYSTEM] WARN — configs/titles.json invalide : " + ex.Message);
                }
            }
        }

        // 7. Vérifier config rewards
        if (config != null && config.Rewards != null)
        {
            CPH.LogInfo("[SYSTEM] OK   — rewards.bonusXpMultiplier  : " + config.Rewards.BonusXpMultiplier);
            CPH.LogInfo("[SYSTEM] OK   — rewards.bonusXpDuration    : " + config.Rewards.BonusXpDurationMinutes + " min");
            CPH.LogInfo("[SYSTEM] OK   — rewards.grantXpAmount      : " + config.Rewards.GrantXpAmount + " XP");

            if (config.Rewards.BonusXpMultiplier < 1.0f)
            {
                CPH.LogWarn("[SYSTEM] WARN — rewards.bonusXpMultiplier < 1.0 (valeur anormale)");
            }
            if (config.Rewards.BonusXpDurationMinutes <= 0)
            {
                CPH.LogWarn("[SYSTEM] WARN — rewards.bonusXpDurationMinutes invalide");
            }
        }

        // 8. Vérifier le cache leaderboard
        var cacheJson = CPH.GetGlobalVar<string>("xp_leaderboard_cache", false);
        if (!string.IsNullOrEmpty(cacheJson))
            CPH.LogInfo("[SYSTEM] INFO — Cache leaderboard present en GlobalVar SB");
        else
            CPH.LogInfo("[SYSTEM] INFO — Cache leaderboard absent (normal si LEADERBOARD_Update n'a pas encore tourne)");

        // 9. Résumé
        CPH.LogInfo("=== SYSTEM_Validate : " + (ok ? "SUCCES — installation valide" : "ECHEC — voir les FAIL ci-dessus") + " ===");

        return ok;
    }
}