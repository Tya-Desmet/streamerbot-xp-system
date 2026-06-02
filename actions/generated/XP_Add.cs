using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

// ============================================================
// ACTION : XP_Add
// ============================================================
// RÔLE : Orchestrateur du pipeline XP chat Twitch.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "XP_Add"
//   2. Déclencheur : Twitch → Chat Message
//   3. Sub-Action 1 : Run Action → USER_GetOrCreate
//   4. Sub-Action 2 : Execute C# Code → coller ce fichier
//   5. Compiler et sauvegarder
//
// PRÉREQUIS :
//   xp_configPath  string  ex: C:\...\streamerbot-xp-system\configs\config.json
//   Persistante : oui
//
// ARGUMENTS ENTRANTS :
//   args["user_username"]  — login Twitch (posé par USER_GetOrCreate)
//   args["user_excluded"]  — bool (posé par USER_GetOrCreate)
//   args["rawInput"]       — message brut du chat
//
// ARGUMENTS SORTANTS :
//   %xp_skipped%     bool   — true si ignoré
//   %xp_skipReason%  string — "excluded" | "command" | "too_short" | "cooldown" | ""
//   %xp_added%       int    — XP ajouté
//   %xp_total%       int    — XP total après ajout
//   %xp_oldLevel%    int    — niveau avant
//   %xp_newLevel%    int    — niveau après
//   %xp_isLevelUp%   bool   — true si montée de niveau
//   %xp_xpIntoLevel% int    — XP dans le niveau actuel
//   %xp_xpForNext%   int    — XP requis pour ce niveau
//   %xp_percentage%  float  — % de progression
//
// AUCUNE logique overlay — AUCUNE logique leaderboard
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

// ----- ValidationService (source : scripts/ValidationService.cs) -----

// ============================================================
// ValidationService.cs — Streamer.bot XP System
// ============================================================
// UTILISATION DANS STREAMER.BOT :
//   Coller ValidationResult + ValidationService au-dessus de CPHInline
//   dans chaque action C# qui en a besoin.
//   Passer CPH au constructeur pour la gestion du cooldown.
//
// Exemple d'action Streamer.bot :
//   [ValidationResult class]
//   [ValidationService class]
//   public class CPHInline {
//       public bool Execute() {
//           var validation = new ValidationService(CPH, cooldownSeconds: 30, minLength: 2);
//           var result = validation.ValidateMessage(userName, rawInput);
//           if (!result.IsValid) { CPH.LogInfo($"Rejeté : {result.Reason}"); return true; }
//           // continuer vers XP_Add, watchtime, etc.
//           return true;
//       }
//   }
// ============================================================


// Résultat de validation — IsValid + code raison pour logging et réactions futures
public class ValidationResult
{
    public bool   IsValid { get; set; }
    public string Reason  { get; set; }

    // Raisons possibles : "command" | "too_short" | "cooldown" | ""
}

// Service de validation — anti-spam, cooldown, commandes, longueur
// Aucune logique XP, aucune logique overlay
// Réutilisable par : XP chat, watchtime, rewards, mini-jeux
public class ValidationService
{
    private readonly IInlineInvokeProxy _CPH;
    private readonly int _cooldownSeconds;
    private readonly int _minLength;

    public ValidationService(IInlineInvokeProxy CPH, int cooldownSeconds = 30, int minLength = 2)
    {
        _CPH             = CPH;
        _cooldownSeconds = cooldownSeconds;
        _minLength       = minLength;
    }

    // Point d'entrée — valide un message de chat Twitch
    public ValidationResult ValidateMessage(string username, string message)
    {
        if (IsCommand(message))
            return Reject("command");

        if (IsTooShort(message))
            return Reject("too_short");

        if (IsOnCooldown(username))
            return Reject("cooldown");

        SetCooldown(username);
        return Accept();
    }

    // Détecte les commandes Twitch (!command, /me, .color...)
    private bool IsCommand(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return true;

        var first = message.TrimStart()[0];
        return first == '!' || first == '/' || first == '.';
    }

    // Vérifie la longueur minimale après nettoyage des espaces
    private bool IsTooShort(string message)
    {
        return message.Trim().Length < _minLength;
    }

    // Vérifie si l'utilisateur est encore en cooldown via Global Variables Streamer.bot
    // GetGlobalVar retourne 0 si la clé n'existe pas — premier message toujours autorisé
    private bool IsOnCooldown(string username)
    {
        var lastTime = _CPH.GetGlobalVar<long>("cooldown_" + username, false);
        var now      = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return (now - lastTime) < _cooldownSeconds;
    }

    // Enregistre le timestamp Unix du dernier message valide
    private void SetCooldown(string username)
    {
        _CPH.SetGlobalVar("cooldown_" + username, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), false);
    }

    private ValidationResult Accept()
        => new ValidationResult { IsValid = true, Reason = "" };

    private ValidationResult Reject(string reason)
        => new ValidationResult { IsValid = false, Reason = reason };
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

public class ExportConfig
{
    public bool?  Enabled       { get; set; }
    public string Path          { get; set; }
    public int    TopCount      { get; set; }
    public bool?  WriteProfiles { get; set; }
    public string Season        { get; set; }
    public string StreamerName  { get; set; }
    public bool?  PushEnabled   { get; set; }
    public string PushUrl       { get; set; }
    public string PushApiKey    { get; set; }
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
    public ExportConfig      Export      { get; set; }
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

        if (c.Export == null) c.Export = new ExportConfig();
        if (!c.Export.Enabled.HasValue)       c.Export.Enabled       = false;
        if (c.Export.Path == null)            c.Export.Path          = "";
        if (c.Export.TopCount <= 0)           c.Export.TopCount      = 50;
        if (!c.Export.WriteProfiles.HasValue) c.Export.WriteProfiles = true;
        if (string.IsNullOrEmpty(c.Export.Season)) c.Export.Season   = "all-time";
        if (c.Export.StreamerName == null)    c.Export.StreamerName  = "";
        if (!c.Export.PushEnabled.HasValue)   c.Export.PushEnabled   = false;
        if (c.Export.PushUrl == null)         c.Export.PushUrl       = "";
        if (c.Export.PushApiKey == null)      c.Export.PushApiKey    = "";
    }
}

// ----- Action Streamer.bot -----

public class CPHInline
{
    public bool Execute()
    {
        // 1. Vérifier le flag d'exclusion posé par USER_GetOrCreate
        var excluded = args.ContainsKey("user_excluded")
                       && args["user_excluded"] != null
                       && (bool)args["user_excluded"] == true;

        if (excluded)
        {
            CPH.SetArgument("xp_skipped",    true);
            CPH.SetArgument("xp_skipReason", "excluded");
            return true;
        }

        // 2. Username posé par USER_GetOrCreate
        if (!args.ContainsKey("user_username") || args["user_username"] == null)
        {
            CPH.LogWarn("[XP_Add] user_username absent — USER_GetOrCreate doit être la sub-action précédente");
            return false;
        }

        var username = args["user_username"].ToString();

        // 3. Message brut
        if (!args.ContainsKey("rawInput") || args["rawInput"] == null)
        {
            CPH.LogWarn("[XP_Add] rawInput absent — vérifier le déclencheur Twitch Chat Message");
            return false;
        }

        var rawInput = args["rawInput"].ToString();

        // 4. Configuration
        var configPath   = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config       = new ConfigService(CPH).LoadConfig(configPath);

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[XP_Add] dataPath non configuré dans configs/config.json");
            return false;
        }

        // 5. Validation anti-spam
        var validation = new ValidationService(CPH, config.Xp.CooldownSeconds, config.Xp.MinMessageLength);
        var check      = validation.ValidateMessage(username, rawInput);

        if (!check.IsValid)
        {
            if (config.Debug.Verbose)
                CPH.LogInfo("[XP_Add] Skip : " + check.Reason + " pour " + username);
            CPH.SetArgument("xp_skipped",    true);
            CPH.SetArgument("xp_skipReason", check.Reason);
            return true;
        }

        // 6. Charger profil (une seule fois)
        var repo = new UserRepository(config.DataPath);
        var user = repo.LoadUser(username);

        if (user == null)
        {
            CPH.LogWarn("[XP_Add] Profil introuvable pour '" + username + "' malgré USER_GetOrCreate");
            return false;
        }

        // 7. Vérifier bonus actif + calculer XP effectif
        var rewards     = new RewardService();
        var now         = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var multiplier  = rewards.GetCurrentMultiplier(user, now);
        var effectiveXp = (int)Math.Round(config.Xp.PerMessage * multiplier);

        var xpService = new XpService(repo);
        var xpResult  = xpService.AddXp(user, effectiveXp, "chat");

        // 8. Progression XP dans le niveau (user déjà en mémoire — zéro I/O)
        var progress = xpService.GetProgress(user);

        // 9. Exposer les résultats
        CPH.SetArgument("xp_skipped",      false);
        CPH.SetArgument("xp_skipReason",   "");
        CPH.SetArgument("xp_added",        xpResult.XpAdded);
        CPH.SetArgument("xp_total",        xpResult.TotalXp);
        CPH.SetArgument("xp_oldLevel",     xpResult.OldLevel);
        CPH.SetArgument("xp_newLevel",     xpResult.NewLevel);
        CPH.SetArgument("xp_isLevelUp",    xpResult.IsLevelUp);
        CPH.SetArgument("xp_xpIntoLevel",  progress.XpIntoLevel);
        CPH.SetArgument("xp_xpForNext",    progress.XpForNext);
        CPH.SetArgument("xp_percentage",   progress.Percentage);
        CPH.SetArgument("xp_multiplier",   multiplier);
        CPH.SetArgument("xp_effective",    effectiveXp);
        CPH.SetArgument("xp_from_chat",    user.XpFromChat);
        CPH.SetArgument("xp_from_watch",   user.XpFromWatch);
        CPH.SetArgument("xp_from_rewards", user.XpFromRewards);
        CPH.SetArgument("xp_bonus_active", multiplier > 1.0f);

        if (xpResult.IsLevelUp)
            CPH.LogInfo("[XP_Add] LEVEL UP ! " + username + " : Niv. " + xpResult.OldLevel + " -> " + xpResult.NewLevel);

        return true;
    }
}