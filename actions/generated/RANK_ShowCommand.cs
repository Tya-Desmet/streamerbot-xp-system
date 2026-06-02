using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

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

// ----- UserRepository (source : scripts/UserRepository.cs) -----

// ============================================================
// UserRepository.cs — Streamer.bot XP System
// ============================================================
// UTILISATION DANS STREAMER.BOT :
//   Ce fichier est une référence source.
//   Coller UserProfile + UserRepository au-dessus de CPHInline
//   dans chaque action C# qui en a besoin.
//
// Exemple d'action Streamer.bot :
//   [UserProfile class]
//   [UserRepository class]
//   public class CPHInline {
//       public bool Execute() {
//           var repo = new UserRepository(CPH.GetGlobalVar<string>("dataPath", true));
//           ...
//           return true;
//       }
//   }
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

    // Vérifie si le fichier {username}.json existe
    public bool UserExists(string username)
    {
        return File.Exists(GetFilePath(username));
    }

    // Crée un profil vierge pour un nouveau viewer
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

    // Charge le profil depuis le JSON — retourne null si introuvable
    public UserProfile LoadUser(string username)
    {
        var path = GetFilePath(username);

        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<UserProfile>(json);
    }

    // Persiste le profil sur le disque
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

    // Charge tous les profils du dossier — pour leaderboard, export, stats globales
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
//
// Flux recommandé dans une action Streamer.bot :
//   1. ValidationService.ValidateMessage()  → rejeter si invalide
//   2. UserRepository.LoadUser()            → charger profil
//   3. XpService.AddXp()                   → modifier XP + sauvegarder
//   4. Lire XpResult.IsLevelUp             → déclencher overlay si besoin
//
// UTILISATION DANS STREAMER.BOT :
//   Coller les 4 classes + XpService au-dessus de CPHInline.
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
    // Accepte le profil déjà chargé pour éviter un double accès disque.
    // Met à jour XP, Level, WatchTime et LastWatchTimestamp en une seule écriture.
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

    // Retourne la progression XP dans le niveau actuel
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

    // Prépare le leaderboard — tri V2 : Level DESC → XP DESC → WatchTime DESC
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

// ----- RankService (source : scripts/RankService.cs) -----

// ============================================================
// RankService.cs — Streamer.bot XP System
// ============================================================
// RESPONSABILITÉ :
//   - Calcul du rang live (aucun rang stocké en base)
//   - Formatage du message Twitch chat pour !rank
//
// UTILISATION DANS STREAMER.BOT :
//   Coller RankService au-dessus de CPHInline dans RANK_ShowCommand.
//   Requiert : UserProfile, XpProgress (défini dans XpService.cs)
//
// TRI DU RANG :
//   1. Level DESC  — prime les niveaux élevés
//   2. XP DESC     — départage les égalités de niveau
//   3. WatchTime DESC — départage les égalités d'XP
//
// AUCUNE écriture disque — AUCUNE logique XP — AUCUN overlay
// ============================================================


public class RankService
{
    // Calcule le rang live (1-based) du viewer dans la liste complète.
    // Tri : Level DESC → XP DESC → WatchTime DESC
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

    // Construit le message chat pour !rank — format single-line Twitch
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

// ----- TitleService (source : scripts/TitleService.cs) -----

// ============================================================
// TitleService.cs — Streamer.bot XP System
// ============================================================
// RESPONSABILITÉ :
//   Charger et résoudre les titres associés aux niveaux.
//   Chaque niveau correspond au titre de la tranche dont il fait partie.
//
// ORDRE DE PRIORITÉ DU CHARGEMENT :
//   1. configs/titles.json           ← surcharge utilisateur (priorité absolue)
//   2. themes/{theme}/titles.json    ← titres du thème courant
//   3. themes/default/titles.json    ← titres par défaut
//   4. Fallback codé en dur          ← jamais en échec
//
// RÈGLE DE RÉSOLUTION :
//   Le titre retourné est celui dont MinLevel est le plus grand
//   parmi ceux dont MinLevel ≤ niveau du viewer.
//
// AUCUNE logique XP — AUCUNE écriture disque — AUCUN overlay
// ============================================================


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

    public List<TitleEntry> LoadTitles(string projectPath, string theme)
    {
        var user = TryLoadFile(Path.Combine(projectPath, "configs", "titles.json"));
        if (user != null) return user;

        if (!string.IsNullOrEmpty(theme) &&
            !string.Equals(theme, "default", StringComparison.OrdinalIgnoreCase))
        {
            var t = TryLoadFile(Path.Combine(projectPath, "themes", theme, "titles.json"));
            if (t != null) return t;
        }

        var def = TryLoadFile(Path.Combine(projectPath, "themes", "default", "titles.json"));
        if (def != null) return def;

        var fallback = new List<TitleEntry>();
        fallback.Add(new TitleEntry { MinLevel = 1, Title = "Viewer" });
        return fallback;
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
        if (candidates.Count > 0)
            return candidates[candidates.Count - 1].Title;
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
                if (e.MinLevel >= 1 && !string.IsNullOrEmpty(e.Title))
                    valid.Add(e);
            return valid.Count > 0 ? valid : null;
        }
        catch { return null; }
    }
}

// ----- BotExclusionService (source : scripts/BotExclusionService.cs) -----

// ============================================================
// BotExclusionService.cs — Streamer.bot XP System
// ============================================================
// RESPONSABILITÉ :
//   Charger et vérifier la liste des comptes exclus du système XP.
//   Aucun exclu ne gagne d'XP, n'apparaît dans le leaderboard,
//   ne peut afficher de profile card ni répondre à !rank.
//
// SOURCES D'EXCLUSION (priorité) :
//   1. configs/excluded-users.json     ← liste personnalisée utilisateur
//   2. Si config.excludeBroadcaster = true et config.broadcasterName renseigné
//      → le streamer est automatiquement exclu
//   3. Fallback codé en dur si le fichier JSON est absent ou vide
//
// COMPARAISON : insensible à la casse (OrdinalIgnoreCase)
//   "NightBot", "nightbot", "NIGHTBOT" → même compte
//
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
                // Ce fichier REMPLACE le fallback — voir configs/EXCLUDED-USERS-README.md
                if (_excluded.Count > 0) return;
            }
            catch { }
        }
        _excluded["nightbot"]      = true;
        _excluded["streamelements"] = true;
        _excluded["streamlabs"]    = true;
        _excluded["moobot"]        = true;
        _excluded["fossabot"]      = true;
        _excluded["wizebot"]       = true;
        _excluded["mixitupbot"]    = true;
        _excluded["streamerbot"]   = true;
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

public class LeaderboardCache
{
    public long              CachedAt { get; set; }
    public List<CachedPlayer> Players  { get; set; }
}

public class CachedPlayer
{
    public int    rank     { get; set; }
    public string username { get; set; }
    public int    level    { get; set; }
    public int    xp       { get; set; }
    public string avatar   { get; set; }
}

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
        var config      = new ConfigService(CPH).LoadConfig(configPath);
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
            if (config.Debug.Verbose)
                CPH.LogInfo("[RANK_ShowCommand] Skip cooldown pour : " + username);
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

        // 6. Rang — cache leaderboard prioritaire, fallback scan complet
        var rankService  = new RankService();
        var xpService    = new XpService(repo);
        var titleService = new TitleService();

        var cacheJson  = CPH.GetGlobalVar<string>("xp_leaderboard_cache", false);
        var maxAge     = (long)(config.Leaderboard.IntervalMinutes * 60 * 1.5);
        var rank       = GetRankFromCache(username, cacheJson, maxAge);
        var totalUsers = 0;

        if (rank == 0)
        {
            CPH.LogInfo("[RANK_ShowCommand] Cache absent ou expire — scan complet");
            var allUsers = repo.GetAllUsers();
            var filtered = new List<UserProfile>();
            foreach (var u in allUsers)
                if (!bots.IsExcluded(u.Username))
                    filtered.Add(u);

            totalUsers = filtered.Count;
            rank       = rankService.GetLiveRank(username, filtered);
        }
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

    private static int GetRankFromCache(string username, string cacheJson, long maxAgeSeconds)
    {
        if (string.IsNullOrEmpty(cacheJson)) return 0;
        try
        {
            var cache = JsonConvert.DeserializeObject<LeaderboardCache>(cacheJson);
            if (cache == null || cache.Players == null) return 0;
            var cacheAge = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - cache.CachedAt;
            if (cacheAge > maxAgeSeconds) return 0;
            foreach (var p in cache.Players)
                if (string.Equals(p.username, username, StringComparison.OrdinalIgnoreCase))
                    return p.rank;
            return 0;
        }
        catch { return 0; }
    }
}