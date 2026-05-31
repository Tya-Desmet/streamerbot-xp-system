// ============================================================
// OBSService.cs — Streamer.bot XP System
// ============================================================
// RÔLE : Transport uniquement.
//         Sérialise les données et les injecte dans les Browser Sources OBS.
//         Aucune logique métier — aucune lecture fichier.
//
// UTILISATION DANS STREAMER.BOT :
//   Coller LeaderboardPayload + CardPayload + OBSService au-dessus de CPHInline.
//
//   var obs = new OBSService(CPH);
//
//   // Leaderboard
//   var entries = new List<LeaderboardPayload> {
//       new LeaderboardPayload { rank = 1, username = "Mystya", level = 42, xp = 18500, avatar = "" },
//       ...
//   };
//   obs.SendLeaderboard(config.ObsLeaderboardSource, entries);
//
//   // Profile card
//   var card = new CardPayload { username = "Mystya", level = 12, xpCurrent = 183, xpForNext = 520, rank = 4 };
//   obs.SendProfileCard(config.ObsCardSource, card);
//
//   // Masquer la card
//   obs.HideCard(config.ObsCardSource);
//
//   // Changer le thème — "default" | "blue" | "red" | "green"
//   obs.SetTheme(config.ObsLeaderboardSource, "blue");
//
// FORMATS JSON ENVOYÉS :
//   Leaderboard : window.updateLeaderboard([{ rank, username, level, xp, avatar }, ...])
//   Card        : window.showCard({ username, avatar, level, xpCurrent, xpForNext, rank })
//   HideCard    : window.hideCard()
//   SetTheme    : window.setTheme('blue')
// ============================================================

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ===== PAYLOADS =====
// Noms de propriétés en camelCase — sérialisation JSON directement compatible JavaScript

// Format attendu par leaderboard.js → window.updateLeaderboard([...])
public class LeaderboardPayload
{
    public int    rank     { get; set; }
    public string username { get; set; }  // displayName du joueur
    public int    level    { get; set; }
    public int    xp       { get; set; }
    public string avatar   { get; set; }  // URL ou "" pour fallback CSS
}

// Format attendu par card.js → window.showCard({...})
public class CardPayload
{
    public string username  { get; set; }  // displayName du joueur
    public string avatar    { get; set; }  // URL ou "" pour fallback CSS
    public int    level     { get; set; }
    public int    xpCurrent { get; set; }  // XP dans le niveau actuel (numérateur barre)
    public int    xpForNext { get; set; }  // XP total du niveau (dénominateur barre)
    public int    rank      { get; set; }  // rang leaderboard (0 = non calculé)
}

// ===== SERVICE =====

// Transport OBS — point d'entrée unique pour les deux overlays
// Construit la chaîne JS et délègue à CPH.ObsSendBrowserSourceScriptUrl
public class OBSService
{
    private readonly IInlineInvokeProxy _CPH;

    public OBSService(IInlineInvokeProxy CPH)
    {
        _CPH = CPH;
    }

    // Envoie le Top 3 vers l'overlay leaderboard
    // Appelé par LEADERBOARD_Update
    public void SendLeaderboard(string sourceName, List<LeaderboardPayload> entries)
    {
        if (entries == null || entries.Count == 0) return;

        var json   = JsonConvert.SerializeObject(entries);
        SendToOBS(sourceName, "window.updateLeaderboard(" + json + ")");
    }

    // Affiche la profile card (auto-disparition après 8 s côté JS)
    // Appelé par CARD_ShowProfile
    public void SendProfileCard(string sourceName, CardPayload data)
    {
        if (data == null) return;

        var json = JsonConvert.SerializeObject(data);
        SendToOBS(sourceName, "window.showCard(" + json + ")");
    }

    // Masque la card avant le timer auto-dismiss
    // Utilisé par une action CARD_Hide optionnelle
    public void HideCard(string sourceName)
    {
        SendToOBS(sourceName, "window.hideCard()");
    }

    // Change le thème d'un overlay
    // Thèmes : "default" | "blue" | "red" | "green"
    // Applicable aux deux overlays (leaderboard et card)
    public void SetTheme(string sourceName, string theme)
    {
        if (string.IsNullOrEmpty(theme)) return;

        // Échappement minimal — theme vient de la config, pas d'un input utilisateur
        var safeTheme = theme.Replace("'", "");
        SendToOBS(sourceName, "window.setTheme('" + safeTheme + "')");
    }

    // Transport bas niveau — seul endroit qui appelle CPH.ObsSendBrowserSourceScriptUrl
    private void SendToOBS(string sourceName, string script)
    {
        if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(script)) return;

        _CPH.ObsSendBrowserSourceScriptUrl(sourceName, script);
    }
}
