// Payload WebSocket leaderboard — sérialisé en JSON vers l'overlay
public class LeaderboardPlayerEntry
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
        // 1. Configuration
        var configPath  = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config      = new ConfigService(CPH).LoadConfig(configPath);
        var configDir   = Path.GetDirectoryName(configPath ?? "");
        var projectPath = Path.GetDirectoryName(configDir ?? "");

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[LEADERBOARD_Update] dataPath non configuré dans configs/config.json");
            return false;
        }

        // 2. Charger tous les profils
        var repo     = new UserRepository(config.DataPath);
        var allUsers = repo.GetAllUsers();

        if (allUsers.Count == 0)
        {
            CPH.LogInfo("[LEADERBOARD_Update] Aucun profil trouvé — overlay non mis à jour");
            CPH.SetArgument("lb_count", 0);
            return true;
        }

        // 3. Filtrer les comptes exclus (bots) avant le tri
        var bots = new BotExclusionService(
                       projectPath,
                       config.Bots.BroadcasterName,
                       config.Bots.ExcludeBroadcaster == true);

        var filtered = new List<UserProfile>();
        foreach (var u in allUsers)
            if (!bots.IsExcluded(u.Username))
                filtered.Add(u);

        if (filtered.Count == 0)
        {
            CPH.LogInfo("[LEADERBOARD_Update] Aucun profil après filtrage des exclusions");
            CPH.SetArgument("lb_count", 0);
            return true;
        }

        // 4. Trier — Level DESC → XP DESC → WatchTime DESC
        var ranked     = new XpService(repo).PrepareLeaderboard(filtered);
        var topCount   = ranked.Count < config.Leaderboard.TopCount ? ranked.Count : config.Leaderboard.TopCount;
        var topPlayers = ranked.GetRange(0, topCount);

        // 5. Construire le payload pour leaderboard.js
        var payload = new List<LeaderboardPlayerEntry>();
        foreach (var e in topPlayers)
            payload.Add(new LeaderboardPlayerEntry
            {
                rank     = e.Rank,
                username = e.DisplayName,
                level    = e.Level,
                xp       = e.Xp,
                avatar   = ""
            });

        // 6. Diffuser via WebSocket
        CPH.WebsocketBroadcastJson(JsonConvert.SerializeObject(new {
            @event  = "updateLeaderboard",
            players = payload
        }));

        // Cache du classement en GlobalVar SB — lu par RANK et CARD
        var cacheData = new { cachedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), players = payload };
        CPH.SetGlobalVar("xp_leaderboard_cache", JsonConvert.SerializeObject(cacheData), false);
        CPH.LogInfo("[LEADERBOARD_Update] Cache mis a jour — " + topPlayers.Count + " joueurs");

        var logCount = topPlayers.Count < 3 ? topPlayers.Count : 3;
        var logParts = new List<string>();
        for (var j = 0; j < logCount; j++)
            logParts.Add(topPlayers[j].DisplayName + " (Niv." + topPlayers[j].Level + ")");
        CPH.LogInfo("[LEADERBOARD_Update] Top " + topPlayers.Count + " envoyé — " + string.Join(", ", logParts.ToArray()));

        // 7. Exposer le Top 10 aux sub-actions
        CPH.SetArgument("lb_count", topPlayers.Count);
        for (var i = 0; i < topPlayers.Count; i++)
        {
            var prefix = "lb_" + (i + 1) + "_";
            CPH.SetArgument(prefix + "username", topPlayers[i].DisplayName);
            CPH.SetArgument(prefix + "level",    topPlayers[i].Level);
            CPH.SetArgument(prefix + "xp",       topPlayers[i].Xp);
        }

        return true;
    }
}
