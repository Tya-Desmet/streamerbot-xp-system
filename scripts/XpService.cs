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
using System.Linq;

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
    public int   CurrentXp    { get; set; }
    public int   XpIntoLevel  { get; set; }
    public int   XpForNext    { get; set; }
    public float Percentage   { get; set; }
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

    public XpService(UserRepository repo)
    {
        _repo = repo;
    }

    // GATEWAY PRINCIPAL — seule méthode autorisée à modifier le XP d'un utilisateur
    // Retourne null si l'utilisateur n'existe pas
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

    // GATEWAY WATCHTIME (V2) — variante optimisée pour les cycles watchtime
    // Accepte le profil déjà chargé pour éviter un double accès disque.
    // Met à jour XP, Level, WatchTime et LastWatchTimestamp en une seule écriture.
    public XpResult AddWatchTimeXp(UserProfile user, int amount, int intervalMinutes, long nowSeconds)
    {
        if (user == null) return null;

        var oldLevel = user.Level;

        user.Xp                  += amount;
        user.Level                = CalculateLevel(user.Xp);
        user.WatchTime           += intervalMinutes;
        user.LastWatchTimestamp   = nowSeconds;

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
    // Utilisé par : profile cards, overlay barre XP, export web
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
    // Aligné avec RankService.GetLiveRank() et LEADERBOARD_Update.cs
    public List<LeaderboardEntry> PrepareLeaderboard(List<UserProfile> users)
    {
        return users
            .OrderByDescending(u => u.Level)
            .ThenByDescending(u => u.Xp)
            .ThenByDescending(u => u.WatchTime)
            .Select((u, index) => new LeaderboardEntry
            {
                Rank        = index + 1,
                Username    = u.Username,
                DisplayName = string.IsNullOrEmpty(u.DisplayName) ? u.Username : u.DisplayName,
                Xp          = u.Xp,
                Level       = u.Level,
                WatchTime   = u.WatchTime
            })
            .ToList();
    }

    // Calcule le niveau depuis le XP total accumulé
    // Formule README : XP pour passer du niveau N au N+1 = 100 × N^1.5
    private int CalculateLevel(int totalXp)
    {
        var level       = 1;
        var accumulated = 0;

        while (true)
        {
            var threshold = XpForNextLevel(level);
            if (accumulated + threshold > totalXp) break;
            accumulated += threshold;
            level++;
        }

        return level;
    }

    // XP cumulé requis pour atteindre le début du niveau donné
    private int XpAtLevelStart(int level)
    {
        var accumulated = 0;
        for (var l = 1; l < level; l++)
            accumulated += XpForNextLevel(l);
        return accumulated;
    }

    // XP requis pour avancer du niveau N au niveau N+1 — formule README : 100 × N^1.5
    private int XpForNextLevel(int level)
    {
        return (int)(100 * Math.Pow(level, 1.5));
    }
}
