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
