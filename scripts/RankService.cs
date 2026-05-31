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

using System;
using System.Collections.Generic;
using System.Linq;

public class RankService
{
    // Calcule le rang live (1-based) du viewer dans la liste complète.
    // Tri : Level DESC → XP DESC → WatchTime DESC
    // Retourne 0 si le username est introuvable (ne devrait pas arriver en pratique).
    public int GetLiveRank(string username, List<UserProfile> allUsers)
    {
        var sorted = allUsers
            .OrderByDescending(u => u.Level)
            .ThenByDescending(u => u.Xp)
            .ThenByDescending(u => u.WatchTime)
            .ToList();

        var index = sorted.FindIndex(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

        return index >= 0 ? index + 1 : 0;
    }

    // Construit le message chat pour !rank — format single-line Twitch
    //
    // Exemples de sortie :
    //   Avec titre    : @Mystya · Compagnon du Stream | Rang #5/127 | Niv.24 | 2450/3100 XP (650 restant) | 2541 messages | 87h32m watchtime
    //   Sans titre    : @Mystya | Rang #5/127 | Niv.24 | 2450/3100 XP (650 restant) | 2541 messages | 87h32m watchtime
    //
    // title est optionnel — passer "" ou null pour l'omettre
    public string FormatRankMessage(UserProfile user, int rank, XpProgress progress, int totalUsers, string title = "")
    {
        var displayName = !string.IsNullOrEmpty(user.DisplayName)
                              ? user.DisplayName
                              : user.Username;

        var xpRemaining = progress.XpForNext - progress.XpIntoLevel;
        var watchStr    = FormatWatchTime(user.WatchTime);
        var rankStr     = totalUsers > 0 ? $"#{rank}/{totalUsers}" : $"#{rank}";
        var titlePart   = !string.IsNullOrEmpty(title) ? $" · {title}" : "";

        return $"@{displayName}{titlePart} | Rang {rankStr} | Niv.{user.Level} | "
             + $"{progress.XpIntoLevel}/{progress.XpForNext} XP ({xpRemaining} restant) | "
             + $"{user.Messages} messages | {watchStr} watchtime";
    }

    // Réponse quand l'utilisateur n'a pas encore de profil dans la base
    public string FormatNotFoundMessage(string displayName)
    {
        return $"@{displayName}, tu n'as pas encore de profil ! "
             + "Chatte sur le stream pour commencer à gagner de l'XP. PogChamp";
    }

    // Convertit des minutes en format humain lisible
    // 0       → "0m"
    // 45      → "45m"
    // 60      → "1h"
    // 87*60+32 → "87h32m"
    private string FormatWatchTime(int totalMinutes)
    {
        if (totalMinutes <= 0) return "0m";
        if (totalMinutes < 60) return $"{totalMinutes}m";

        var h = totalMinutes / 60;
        var m = totalMinutes % 60;
        return m > 0 ? $"{h}h{m:D2}m" : $"{h}h";
    }
}
