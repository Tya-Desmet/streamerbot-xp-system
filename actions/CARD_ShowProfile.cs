// ============================================================
// ACTION : CARD_ShowProfile
// ============================================================
// RÔLE : Déclenche l'affichage de la profile card OBS.
//         Charge le profil, calcule la progression visuelle, envoie à l'overlay.
//         Aucune modification de données.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "CARD_ShowProfile"
//   2. Déclencheur : Twitch → Channel Point Redemption
//                   (sélectionner la reward souhaitée)
//   3. Sub-Action : Execute C# Code → coller ce fichier
//   4. Compiler et sauvegarder
//
// POUR MASQUER LA CARD MANUELLEMENT (action séparée) :
//   Sub-Action → Execute C# Code :
//     var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);
//     var config     = new ConfigService().LoadConfig(configPath);
//     CPH.ObsSendBrowserSourceScriptUrl(config.ObsCardSource, "window.hideCard()");
//
// PRÉREQUIS — une seule variable globale dans Streamer.bot :
//   xp_configPath  string  ex: C:\...\streamerbot-xp-system\configs\config.json
//   Persistante : oui — toute la config est dans configs/config.json
//
// FORMAT JSON envoyé à l'overlay (card.js attend ce format) :
//   window.showCard({
//     "username":  "Mystya",   // displayName du joueur
//     "avatar":    "",          // "" = fallback couleur CSS (extension future : URL Twitch)
//     "level":     12,
//     "xpCurrent": 183,         // XP gagné dans le niveau actuel
//     "xpForNext": 520,         // XP total requis pour compléter ce niveau
//     "rank":      4            // rang calculé en temps réel (tri XP décroissant)
//   })
//
// ARGUMENTS SORTANTS :
//   %card_sent%      bool   — true si la card a été envoyée à OBS
//   %card_username%  string — displayName affiché
//   %card_level%     int    — niveau
//   %card_xp%        int    — XP total accumulé
//   %card_rank%      int    — rang leaderboard
//
// AUCUNE logique XP — AUCUNE logique leaderboard — AUCUNE écriture
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

// ----- UserProfile -----

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

// ----- UserRepository — lecture seule -----

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

// ----- Payload JSON envoyé à card.js -----
// Propriétés en camelCase → sérialisation directement compatible JavaScript

public class CardPayload
{
    public string username  { get; set; }
    public string avatar    { get; set; }
    public int    level     { get; set; }
    public int    xpCurrent { get; set; }
    public int    xpForNext { get; set; }
    public int    rank      { get; set; }
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
        // 1. Username depuis l'événement Twitch Channel Point Redemption
        if (!args.ContainsKey("userName") || args["userName"] == null)
        {
            CPH.LogWarn("[CARD_ShowProfile] userName absent des args");
            return false;
        }

        var username    = args["userName"].ToString().Trim();
        var displayName = args.ContainsKey("userDisplayName") && args["userDisplayName"] != null
                              ? args["userDisplayName"].ToString().Trim()
                              : username;

        if (string.IsNullOrEmpty(username))
        {
            CPH.LogWarn("[CARD_ShowProfile] userName vide");
            return false;
        }

        // 2. Charger la configuration depuis config.json
        var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config     = new ConfigService().LoadConfig(configPath);

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[CARD_ShowProfile] dataPath non configuré dans configs/config.json");
            return false;
        }

        if (string.IsNullOrEmpty(config.ObsCardSource))
        {
            CPH.LogWarn("[CARD_ShowProfile] obsCardSource non configuré dans configs/config.json");
            return false;
        }

        var dataPath  = config.DataPath;
        var obsSource = config.ObsCardSource;

        // 3. Charger le profil (lecture seule — aucune écriture)
        var repo = new UserRepository(dataPath);
        var user = repo.LoadUser(username);

        if (user == null)
        {
            CPH.LogWarn($"[CARD_ShowProfile] Profil introuvable pour '{username}' — aucune card affichée");
            CPH.SetArgument("card_sent", false);
            return true;  // true = ne pas bloquer les autres sub-actions
        }

        // 4. Calculer le rang en temps réel — tri XP décroissant sur tous les profils
        var allUsers = repo.GetAllUsers();
        var liveRank = CalculateLiveRank(username, allUsers);

        // 5. Calculer la progression XP dans le niveau actuel (affichage uniquement)
        var xpIntoLevel = user.Xp - XpAtLevelStart(user.Level);
        var xpForNext   = XpForLevel(user.Level);

        // 6. Construire le payload pour card.js
        var name    = string.IsNullOrEmpty(user.DisplayName) ? displayName : user.DisplayName;
        var payload = new CardPayload
        {
            username  = name,
            avatar    = "",
            level     = user.Level,
            xpCurrent = xpIntoLevel,
            xpForNext = xpForNext,
            rank      = liveRank
        };

        // 7. Diffuser via WebSocket Streamer.bot → overlay card
        var wsPayload = JsonConvert.SerializeObject(new {
            @event = "showCard",
            card   = payload
        });
        CPH.WebsocketBroadcastJson(wsPayload);

        CPH.LogInfo($"[CARD_ShowProfile] Card affichée : {name} | Niv.{user.Level} | {user.Xp} XP | Rang #{liveRank}");

        // 8. Exposer les données aux sub-actions suivantes
        CPH.SetArgument("card_sent",     true);
        CPH.SetArgument("card_username", name);
        CPH.SetArgument("card_level",    user.Level);
        CPH.SetArgument("card_xp",       user.Xp);
        CPH.SetArgument("card_rank",     liveRank);

        return true;
    }

    // Rang en temps réel : position du joueur dans le tri XP décroissant
    private int CalculateLiveRank(string username, List<UserProfile> allUsers)
    {
        allUsers.Sort((a, b) => b.Xp.CompareTo(a.Xp));
        for (var i = 0; i < allUsers.Count; i++)
            if (allUsers[i].Username == username) return i + 1;
        return 0;
    }

    // Formule README : XP nécessaire pour compléter le niveau N = 100 × N^1.5
    private int XpForLevel(int level)
        => (int)(100 * Math.Pow(level, 1.5));

    // XP total cumulé depuis le niveau 1 jusqu'au début du niveau donné
    private int XpAtLevelStart(int level)
    {
        var acc = 0;
        for (var l = 1; l < level; l++) acc += XpForLevel(l);
        return acc;
    }
}
