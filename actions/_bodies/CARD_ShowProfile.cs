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

// Payload WebSocket profile card — sérialisé en JSON vers l'overlay
public class CardPayload
{
    public string username  { get; set; }
    public string avatar    { get; set; }
    public int    level     { get; set; }
    public int    xpCurrent { get; set; }
    public int    xpForNext { get; set; }
    public int    rank      { get; set; }
    public string title     { get; set; }
    public int    messages  { get; set; }
    public int    watchTime { get; set; }
}

public class CPHInline
{
    public bool Execute()
    {
        // 1. Identité
        if (!args.ContainsKey("userName") || args["userName"] == null)
        {
            CPH.LogWarn("[CARD_ShowProfile] userName absent des args");
            return false;
        }

        var username    = args["userName"].ToString().Trim();
        var displayName = args.ContainsKey("userDisplayName") && args["userDisplayName"] != null
                              ? args["userDisplayName"].ToString().Trim()
                              : username;

        if (string.IsNullOrEmpty(username)) { CPH.LogWarn("[CARD_ShowProfile] userName vide"); return false; }

        // 2. Configuration + chemins
        var configPath  = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config      = new ConfigService(CPH).LoadConfig(configPath);
        var configDir   = Path.GetDirectoryName(configPath ?? "");
        var projectPath = Path.GetDirectoryName(configDir ?? "");

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[CARD_ShowProfile] dataPath non configuré");
            return false;
        }

        // 3. Vérification exclusion
        var bots = new BotExclusionService(
            projectPath,
            config.Bots.BroadcasterName,
            config.Bots.ExcludeBroadcaster == true);

        if (bots.IsExcluded(username))
        {
            if (config.Debug.Verbose)
                CPH.LogInfo("[CARD_ShowProfile] Bot exclu, card ignoree pour : " + username);
            CPH.SetArgument("card_sent", false);
            return true;
        }

        // 4. Profil viewer
        var repo = new UserRepository(config.DataPath);
        var user = repo.LoadUser(username);

        if (user == null)
        {
            CPH.LogWarn("[CARD_ShowProfile] Profil introuvable pour '" + username + "'");
            CPH.SetArgument("card_sent", false);
            return true;
        }

        // 5. Rang live — cache leaderboard prioritaire, fallback scan complet
        var cacheJson = CPH.GetGlobalVar<string>("xp_leaderboard_cache", false);
        var maxAge    = (long)(config.Leaderboard.IntervalMinutes * 60 * 1.5);
        var liveRank  = GetRankFromCache(username, cacheJson, maxAge);

        if (liveRank == 0)
        {
            var allUsers = repo.GetAllUsers();
            var filtered = new List<UserProfile>();
            foreach (var u in allUsers)
                if (!bots.IsExcluded(u.Username))
                    filtered.Add(u);

            filtered.Sort((a, b) => {
                if (b.Level    != a.Level)    return b.Level.CompareTo(a.Level);
                if (b.Xp       != a.Xp)       return b.Xp.CompareTo(a.Xp);
                return b.WatchTime.CompareTo(a.WatchTime);
            });

            for (var i = 0; i < filtered.Count; i++)
                if (string.Equals(filtered[i].Username, username, StringComparison.OrdinalIgnoreCase))
                { liveRank = i + 1; break; }
        }

        // 6. Progression XP + titre
        var progress = new XpService(repo).GetProgress(user);
        var title    = new TitleService().GetTitle(user.Level, projectPath, config.Theme);

        // 7. Payload V2
        var name    = string.IsNullOrEmpty(user.DisplayName) ? displayName : user.DisplayName;
        var payload = new CardPayload
        {
            username  = name,
            avatar    = "",
            level     = user.Level,
            xpCurrent = progress.XpIntoLevel,
            xpForNext = progress.XpForNext,
            rank      = liveRank,
            title     = title,
            messages  = user.Messages,
            watchTime = user.WatchTime
        };

        // 8. Diffuser via WebSocket
        CPH.WebsocketBroadcastJson(JsonConvert.SerializeObject(new { @event = "showCard", card = payload }));

        CPH.LogInfo("[CARD_ShowProfile] " + name + " (" + title + ") | Niv." + user.Level + " | Rang #" + liveRank);

        // 9. Exposer
        CPH.SetArgument("card_sent",      true);
        CPH.SetArgument("card_username",  name);
        CPH.SetArgument("card_level",     user.Level);
        CPH.SetArgument("card_xp",        user.Xp);
        CPH.SetArgument("card_rank",      liveRank);
        CPH.SetArgument("card_title",     title);
        CPH.SetArgument("card_messages",  user.Messages);
        CPH.SetArgument("card_watchtime", user.WatchTime);

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
