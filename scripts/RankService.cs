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
