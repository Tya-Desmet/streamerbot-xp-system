// ============================================================
// ACTION : XP_Add
// ============================================================
// RÔLE : Orchestrateur du pipeline XP chat Twitch.
//         Appelle les services dans l'ordre — aucun calcul direct.
//
// INSTALLATION DANS STREAMER.BOT :
//   1. Actions → Add Action → nommer "XP_Add"
//   2. Déclencheur : Twitch → Chat Message
//   3. Sub-Action 1 : Run Action → USER_GetOrCreate
//   4. Sub-Action 2 : Execute C# Code → coller ce fichier
//   5. Compiler et sauvegarder
//
// PRÉREQUIS — une seule variable globale dans Streamer.bot :
//   xp_configPath  string  ex: C:\...\streamerbot-xp-system\configs\config.json
//   Persistante : oui — toute la config est dans configs/config.json
//
// ARGUMENTS ENTRANTS :
//   args["user_username"]  — login Twitch (posé par USER_GetOrCreate)
//   args["rawInput"]       — message brut du chat (fourni par Streamer.bot)
//
// ARGUMENTS SORTANTS (disponibles dans les sub-actions suivantes) :
//   %xp_skipped%           bool   — true si message ignoré (spam/cooldown/commande)
//   %xp_skipReason%        string — "command" | "too_short" | "cooldown" | ""
//   %xp_added%             int    — XP ajouté cette fois
//   %xp_total%             int    — XP total après ajout
//   %xp_oldLevel%          int    — niveau avant ajout
//   %xp_newLevel%          int    — niveau après ajout
//   %xp_isLevelUp%         bool   — true si montée de niveau
//   %xp_xpIntoLevel%       int    — XP dans le niveau actuel (pour barre overlay)
//   %xp_xpForNext%         int    — XP requis pour compléter ce niveau
//   %xp_percentage%        float  — % de progression dans le niveau
//
// AUCUNE logique overlay — AUCUNE logique leaderboard
// ============================================================

using System;
using System.IO;
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
}

// ----- UserRepository — extrait : lecture/écriture uniquement -----

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

    public void SaveUser(UserProfile user)
    {
        File.WriteAllText(GetFilePath(user.Username),
            JsonConvert.SerializeObject(user, Formatting.Indented));
    }

    private string GetFilePath(string username)
        => Path.Combine(_dataPath, $"{username}.json");
}

// ----- ValidationService (source : scripts/ValidationService.cs) -----

public class ValidationResult
{
    public bool   IsValid { get; set; }
    public string Reason  { get; set; }
}

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

    public ValidationResult ValidateMessage(string username, string message)
    {
        if (IsCommand(message))     return Reject("command");
        if (IsTooShort(message))    return Reject("too_short");
        if (IsOnCooldown(username)) return Reject("cooldown");
        SetCooldown(username);
        return Accept();
    }

    private bool IsCommand(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return true;
        var first = message.TrimStart()[0];
        return first == '!' || first == '/' || first == '.';
    }

    private bool IsTooShort(string message)
        => message.Trim().Length < _minLength;

    private bool IsOnCooldown(string username)
    {
        var last = _CPH.GetGlobalVar<long>($"cooldown_{username}", false);
        return (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - last) < _cooldownSeconds;
    }

    private void SetCooldown(string username)
        => _CPH.SetGlobalVar($"cooldown_{username}", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), false);

    private ValidationResult Accept()            => new ValidationResult { IsValid = true,  Reason = "" };
    private ValidationResult Reject(string r)    => new ValidationResult { IsValid = false, Reason = r };
}

// ----- XpService — extrait : AddXp + GetProgress -----

public class XpResult
{
    public string Username  { get; set; }
    public int    XpAdded   { get; set; }
    public int    TotalXp   { get; set; }
    public int    OldLevel  { get; set; }
    public int    NewLevel  { get; set; }
    public bool   IsLevelUp { get; set; }
}

public class XpProgress
{
    public int   XpIntoLevel { get; set; }
    public int   XpForNext   { get; set; }
    public float Percentage  { get; set; }
}

public class XpService
{
    private readonly UserRepository _repo;

    public XpService(UserRepository repo) { _repo = repo; }

    // GATEWAY — seule méthode autorisée à modifier le XP
    public XpResult AddXp(string username, int amount)
    {
        var user = _repo.LoadUser(username);
        if (user == null) return null;

        var oldLevel = user.Level;
        user.Xp   += amount;
        user.Level = CalculateLevel(user.Xp);
        _repo.SaveUser(user);

        return new XpResult
        {
            Username  = username,
            XpAdded   = amount,
            TotalXp   = user.Xp,
            OldLevel  = oldLevel,
            NewLevel  = user.Level,
            IsLevelUp = user.Level > oldLevel
        };
    }

    // Progression dans le niveau actuel — utilisé par l'overlay barre XP
    public XpProgress GetProgress(UserProfile user)
    {
        var xpAtStart   = XpAtLevelStart(user.Level);
        var xpForNext   = XpForNextLevel(user.Level);
        var xpIntoLevel = user.Xp - xpAtStart;

        return new XpProgress
        {
            XpIntoLevel = xpIntoLevel,
            XpForNext   = xpForNext,
            Percentage  = xpForNext > 0 ? (float)xpIntoLevel / xpForNext * 100f : 0f
        };
    }

    private int CalculateLevel(int totalXp)
    {
        var level = 1;
        var acc   = 0;
        while (true)
        {
            var threshold = XpForNextLevel(level);
            if (acc + threshold > totalXp) break;
            acc += threshold;
            level++;
        }
        return level;
    }

    private int XpAtLevelStart(int level)
    {
        var acc = 0;
        for (var l = 1; l < level; l++) acc += XpForNextLevel(l);
        return acc;
    }

    private int XpForNextLevel(int level)
        => (int)(100 * Math.Pow(level, 1.5));
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
        // 1. Username posé par USER_GetOrCreate (Sub-Action 1)
        if (!args.ContainsKey("user_username") || args["user_username"] == null)
        {
            CPH.LogWarn("[XP_Add] user_username absent — USER_GetOrCreate doit être la sub-action précédente");
            return false;
        }

        var username = args["user_username"].ToString();

        // 2. Message brut fourni par l'événement Twitch Chat Message
        if (!args.ContainsKey("rawInput") || args["rawInput"] == null)
        {
            CPH.LogWarn("[XP_Add] rawInput absent — vérifier le déclencheur Twitch Chat Message");
            return false;
        }

        var rawInput = args["rawInput"].ToString();

        // 3. Charger la configuration depuis config.json
        var configPath   = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config       = new ConfigService().LoadConfig(configPath);

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[XP_Add] dataPath non configuré dans configs/config.json");
            return false;
        }

        var dataPath     = config.DataPath;
        var cooldown     = config.CooldownSeconds;
        var minLength    = config.MinMessageLength;
        var xpPerMessage = config.XpPerMessage;

        // 4. Validation — anti-spam, cooldown, commandes Twitch
        var validation = new ValidationService(CPH, cooldown, minLength);
        var check      = validation.ValidateMessage(username, rawInput);

        if (!check.IsValid)
        {
            // Silencieux : ne pas bloquer la chaîne, juste marquer comme ignoré
            CPH.SetArgument("xp_skipped",    true);
            CPH.SetArgument("xp_skipReason", check.Reason);
            return true;
        }

        // 5. Ajout XP via gateway XpService
        var repo      = new UserRepository(dataPath);
        var xpService = new XpService(repo);
        var xpResult  = xpService.AddXp(username, xpPerMessage);

        if (xpResult == null)
        {
            CPH.LogWarn($"[XP_Add] Profil introuvable pour '{username}' malgré USER_GetOrCreate");
            return false;
        }

        // 6. Incrémenter Messages + timestamp (hors gateway XP — stat pure)
        var user = repo.LoadUser(username);
        user.Messages++;
        user.LastMessageTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        repo.SaveUser(user);

        // 7. Progression XP dans le niveau — pour overlay barre
        var progress = xpService.GetProgress(user);

        // 8. Exposer tous les résultats aux sub-actions suivantes
        CPH.SetArgument("xp_skipped",     false);
        CPH.SetArgument("xp_skipReason",  "");
        CPH.SetArgument("xp_added",       xpResult.XpAdded);
        CPH.SetArgument("xp_total",       xpResult.TotalXp);
        CPH.SetArgument("xp_oldLevel",    xpResult.OldLevel);
        CPH.SetArgument("xp_newLevel",    xpResult.NewLevel);
        CPH.SetArgument("xp_isLevelUp",   xpResult.IsLevelUp);
        CPH.SetArgument("xp_xpIntoLevel", progress.XpIntoLevel);
        CPH.SetArgument("xp_xpForNext",   progress.XpForNext);
        CPH.SetArgument("xp_percentage",  progress.Percentage);

        if (xpResult.IsLevelUp)
            CPH.LogInfo($"[XP_Add] LEVEL UP ! {username} : Niv. {xpResult.OldLevel} → {xpResult.NewLevel}");

        return true;
    }
}
