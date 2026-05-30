// ============================================================
// ACTION : USER_GetOrCreate
// ============================================================
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "USER_GetOrCreate"
//   2. Add Sub-Action → Execute C# Code
//   3. Copier l'intégralité de ce fichier dans l'éditeur
//   4. Cliquer "Compile" puis "Save"
//
// PRÉREQUIS — une seule variable globale dans Streamer.bot :
//   Dans Streamer.bot : Settings → Global Variables
//   Nom : xp_configPath
//   Valeur : C:\Users\TonNom\streamerbot-xp-system\configs\config.json
//   Persistante : oui
//
// DÉCLENCHEMENT :
//   - Appelée en première sub-action de toute action Twitch
//     qui a besoin d'un profil utilisateur (XP_Add, CARD_ShowProfile...)
//   - Compatible avec tous les événements Twitch qui fournissent userName
//
// ARGUMENTS ENTRANTS (fournis automatiquement par Streamer.bot) :
//   args["userName"]        — login Twitch (toujours minuscule)
//   args["userDisplayName"] — pseudo affiché (avec majuscules)
//
// ARGUMENTS SORTANTS (disponibles dans les sub-actions suivantes) :
//   %user_username%          string — login Twitch
//   %user_displayName%       string — pseudo affiché
//   %user_xp%                int    — XP total accumulé
//   %user_level%             int    — niveau actuel
//   %user_messages%          int    — nombre de messages validés
//   %user_watchtime%         int    — watchtime en minutes
//   %user_rank%              int    — rang leaderboard (0 = non calculé)
//   %user_lastTimestamp%     long   — timestamp Unix du dernier message
//   %user_isNew%             bool   — true si le profil vient d'être créé
//
// AUCUNE logique XP — AUCUNE logique leaderboard — AUCUN overlay
// ============================================================

using System;
using System.IO;
using Newtonsoft.Json;

// ----- Modèle utilisateur (source : scripts/UserRepository.cs) -----

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
}

// ----- Repository JSON (source : scripts/UserRepository.cs) -----

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
        var path = GetFilePath(username);
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
            LastMessageTimestamp = 0
        };

        SaveUser(user);
        return user;
    }

    public void SaveUser(UserProfile user)
    {
        var json = JsonConvert.SerializeObject(user, Formatting.Indented);
        File.WriteAllText(GetFilePath(user.Username), json);
    }

    private string GetFilePath(string username)
    {
        return Path.Combine(_dataPath, $"{username}.json");
    }
}

// ----- Config + ConfigService (source : scripts/ConfigService.cs) -----

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

public class ConfigService
{
    public Config LoadConfig(string path)
    {
        Config c = null;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            try { c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path)); } catch { }
        c = c ?? new Config();
        if (c.XpPerMessage               <= 0) c.XpPerMessage               = 10;
        if (c.XpPerWatch                 <= 0) c.XpPerWatch                 = 1;
        if (c.CooldownSeconds            <= 0) c.CooldownSeconds            = 30;
        if (c.MinMessageLength           <= 0) c.MinMessageLength           = 2;
        if (c.LeaderboardIntervalMinutes <= 0) c.LeaderboardIntervalMinutes = 5;
        if (string.IsNullOrEmpty(c.ObsLeaderboardSource)) c.ObsLeaderboardSource = "Leaderboard";
        if (string.IsNullOrEmpty(c.ObsCardSource))        c.ObsCardSource        = "ProfileCard";
        return c;
    }
}

// ----- Action Streamer.bot -----

public class CPHInline
{
    public bool Execute()
    {
        // 1. Récupérer le login Twitch depuis les args de l'événement
        if (!args.ContainsKey("userName") || args["userName"] == null)
        {
            CPH.LogWarn("[USER_GetOrCreate] userName absent des args — action arrêtée");
            return false;
        }

        var username    = args["userName"].ToString().Trim();
        var displayName = args.ContainsKey("userDisplayName") && args["userDisplayName"] != null
                              ? args["userDisplayName"].ToString().Trim()
                              : username;

        if (string.IsNullOrEmpty(username))
        {
            CPH.LogWarn("[USER_GetOrCreate] userName vide — action arrêtée");
            return false;
        }

        // 2. Charger la configuration depuis config.json
        var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config     = new ConfigService().LoadConfig(configPath);

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[USER_GetOrCreate] dataPath non configuré dans configs/config.json");
            return false;
        }

        var dataPath = config.DataPath;

        // 3. Charger le profil ou le créer s'il n'existe pas encore
        var repo  = new UserRepository(dataPath);
        var isNew = false;
        var user  = repo.LoadUser(username);

        if (user == null)
        {
            user  = repo.CreateUser(username, displayName);
            isNew = true;
            CPH.LogInfo($"[USER_GetOrCreate] Nouveau joueur créé : {displayName} ({username})");
        }

        // 4. Exposer le profil aux sub-actions suivantes
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
