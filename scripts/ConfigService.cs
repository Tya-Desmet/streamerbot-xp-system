// ============================================================
// ConfigService.cs — Streamer.bot XP System
// ============================================================
// UTILISATION DANS STREAMER.BOT :
//   Coller Config + ConfigService au-dessus de CPHInline.
//   Une seule Global Variable à définir dans Streamer.bot :
//
//     Nom     : xp_configPath
//     Valeur  : C:\Users\TonNom\streamerbot-xp-system\configs\config.json
//     Persist : oui
//
//   Puis dans Execute() :
//     var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);
//     var config     = new ConfigService().LoadConfig(configPath);
//     // config.DataPath, config.XpPerMessage, config.ObsCardSource...
//
// FALLBACK :
//   Si le fichier est absent ou malformé, LoadConfig retourne les valeurs
//   par défaut sans lever d'exception. Les actions continuent normalement.
//
// AUCUNE logique métier — lecture seule
// ============================================================

using System;
using System.IO;
using Newtonsoft.Json;

// Modèle de configuration — structure exacte de configs/config.json
// Newtonsoft.Json est insensible à la casse : camelCase JSON → PascalCase C# ✓
public class Config
{
    public string DataPath                   { get; set; }
    public int    XpPerMessage               { get; set; }
    public int    XpPerWatch                 { get; set; }
    public int    CooldownSeconds            { get; set; }
    public int    MinMessageLength           { get; set; }
    public int    LeaderboardIntervalMinutes { get; set; }
    public string ObsLeaderboardSource       { get; set; }
    public string ObsCardSource              { get; set; }
}

// Chargeur de config — retourne toujours un objet valide
// Champ absent ou invalide dans le JSON → valeur par défaut appliquée
public class ConfigService
{
    private static readonly Config Defaults = new Config
    {
        DataPath                   = "",
        XpPerMessage               = 10,
        XpPerWatch                 = 1,
        CooldownSeconds            = 30,
        MinMessageLength           = 2,
        LeaderboardIntervalMinutes = 5,
        ObsLeaderboardSource       = "Leaderboard",
        ObsCardSource              = "ProfileCard"
    };

    // Point d'entrée — ne lève jamais d'exception
    public Config LoadConfig(string configPath)
    {
        Config loaded = null;

        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                loaded   = JsonConvert.DeserializeObject<Config>(json);
            }
            catch { /* fichier malformé → fallback complet */ }
        }

        return ApplyDefaults(loaded ?? new Config());
    }

    // Applique les valeurs par défaut champ par champ
    // Seuls les champs explicitement définis dans le JSON surchargent les defaults
    private Config ApplyDefaults(Config c)
    {
        return new Config
        {
            DataPath                   = !string.IsNullOrEmpty(c.DataPath)
                                             ? c.DataPath
                                             : Defaults.DataPath,

            XpPerMessage               = c.XpPerMessage > 0
                                             ? c.XpPerMessage
                                             : Defaults.XpPerMessage,

            XpPerWatch                 = c.XpPerWatch > 0
                                             ? c.XpPerWatch
                                             : Defaults.XpPerWatch,

            CooldownSeconds            = c.CooldownSeconds > 0
                                             ? c.CooldownSeconds
                                             : Defaults.CooldownSeconds,

            MinMessageLength           = c.MinMessageLength > 0
                                             ? c.MinMessageLength
                                             : Defaults.MinMessageLength,

            LeaderboardIntervalMinutes = c.LeaderboardIntervalMinutes > 0
                                             ? c.LeaderboardIntervalMinutes
                                             : Defaults.LeaderboardIntervalMinutes,

            ObsLeaderboardSource       = !string.IsNullOrEmpty(c.ObsLeaderboardSource)
                                             ? c.ObsLeaderboardSource
                                             : Defaults.ObsLeaderboardSource,

            ObsCardSource              = !string.IsNullOrEmpty(c.ObsCardSource)
                                             ? c.ObsCardSource
                                             : Defaults.ObsCardSource
        };
    }
}
