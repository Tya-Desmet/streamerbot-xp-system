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

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

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
            Rank                 = 0,
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


// ----- WatchTimeService (source : scripts/WatchTimeService.cs) -----

// ============================================================
// WatchTimeService.cs — Streamer.bot XP System
// ============================================================
// RESPONSABILITÉ : Vérifier l'éligibilité watchtime d'un viewer.
//                  Ne lit et n'écrit jamais le disque directement.
//
// UTILISATION DANS STREAMER.BOT :
//   Coller WatchTimeResult + WatchTimeService au-dessus de CPHInline
//   dans l'action XP_WatchTime.
//
// FLUX DANS XP_WatchTime :
//   1. UserRepository.LoadUser() ou CreateUser()   → UserProfile
//   2. WatchTimeService.IsEligible()               → bool
//   3. Si true : XpService.AddWatchTimeXp()        → XpResult
//
// AUCUNE logique XP — AUCUNE écriture disque — AUCUN overlay
// ============================================================

// Résultat d'un cycle watchtime pour un viewer — pour logging et debug
public class WatchTimeResult
{
    public string Username    { get; set; }
    public int    XpAwarded   { get; set; }
    public bool   WasEligible { get; set; }
    public string SkipReason  { get; set; }  // vide si éligible
}

public class WatchTimeService
{
    // Vérifie si le viewer peut recevoir de l'XP watchtime ce cycle.
    //
    // Retourne false si :
    //   - user est null
    //   - LastWatchTimestamp est trop récent (< intervalMinutes - TOLERANCE)
    //
    // Tolérance de 10 secondes pour absorber les imprécisions des timers
    // Streamer.bot (un timer de 5 min peut déclencher à 4 min 52 sec).
    private const int TimerToleranceSeconds = 10;

    public bool IsEligible(UserProfile user, int intervalMinutes, long nowSeconds)
    {
        if (user == null)
            return false;

        var intervalSeconds  = intervalMinutes * 60;
        var elapsed          = nowSeconds - user.LastWatchTimestamp;

        return elapsed >= (intervalSeconds - TimerToleranceSeconds);
    }

    // Construit un WatchTimeResult pour logging
    public WatchTimeResult BuildResult(string username, int xpAwarded, bool eligible, string skipReason = "")
    {
        return new WatchTimeResult
        {
            Username    = username,
            XpAwarded   = eligible ? xpAwarded : 0,
            WasEligible = eligible,
            SkipReason  = skipReason
        };
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

using System;
using System.Collections.Generic;

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
    // Inclut Messages++ et LastMessageTimestamp — une seule écriture disque
    public XpResult AddXp(UserProfile user, int amount)
    {
        if (user == null) return null;

        var oldLevel              = user.Level;
        user.Xp                  += amount;
        user.Level                = CalculateLevel(user.Xp);
        user.Messages++;
        user.LastMessageTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

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

using System;
using System.IO;
using Newtonsoft.Json;

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
        var config      = new ConfigService(CPH).LoadConfig(configPath);
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
            if (config.Debug.Verbose)
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
            if (config.Debug.Verbose)
                CPH.LogInfo("[XP_WatchTime_V2] Watchtime désactivé dans config.json");
            return true;
        }

        // 4. Construire le set de présence depuis la liste SB
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

        // 5. Services
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

        // 6. Boucle principale
        foreach (var user in allUsers)
        {
            if (bots.IsExcluded(user.Username)) { skipped++; continue; }
            if (!watchService.IsEligible(user, interval, now)) { skipped++; continue; }

            var inPresenceList = presentSet.ContainsKey(user.Username);
            var recentChat     = (now - user.LastMessageTimestamp) <= activityWindowSeconds;

            if (!inPresenceList && !recentChat) { skipped++; continue; }

            if (streakEnabled)
            {
                var prevGap = now - user.LastWatchTimestamp;
                var maxGap  = activityWindowSeconds;
                user.WatchStreak = (user.LastWatchTimestamp > 0 && prevGap <= maxGap)
                                   ? user.WatchStreak + 1
                                   : 1;
            }

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

        // 7. Exposer résultats
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

    private static int StreakBonus(int streak)
    {
        if (streak >= 24) return 5;
        if (streak >= 12) return 3;
        if (streak >= 6)  return 2;
        if (streak >= 3)  return 1;
        return 0;
    }
}

