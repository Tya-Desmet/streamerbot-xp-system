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

public class CPHInline
{
    public bool Execute()
    {
        // 1. Identité
        if (!args.ContainsKey("userName") || args["userName"] == null)
        {
            CPH.LogWarn("[RANK_ShowCommand] userName absent des args");
            return false;
        }

        var username    = args["userName"].ToString().Trim().ToLowerInvariant();
        var displayName = args.ContainsKey("userDisplayName") && args["userDisplayName"] != null
                              ? args["userDisplayName"].ToString().Trim()
                              : username;

        if (string.IsNullOrEmpty(username)) { CPH.LogWarn("[RANK_ShowCommand] userName vide"); return false; }

        // 2. Configuration + chemins
        var configPath  = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config      = new ConfigService(CPH).LoadConfig(configPath);
        var configDir   = Path.GetDirectoryName(configPath ?? "");
        var projectPath = Path.GetDirectoryName(configDir ?? "");

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[RANK_ShowCommand] dataPath non configuré");
            return false;
        }

        // 3. Vérification exclusion — silencieux pour les bots
        var bots = new BotExclusionService(
            projectPath,
            config.Bots.BroadcasterName,
            config.Bots.ExcludeBroadcaster == true);

        if (bots.IsExcluded(username))
        {
            CPH.SetArgument("rank_skipped",  true);
            CPH.SetArgument("rank_sent",     false);
            CPH.SetArgument("rank_notfound", false);
            CPH.SetArgument("rank_position", 0);
            return true;
        }

        // 4. Cooldown par utilisateur
        var cooldownKey = "rank_cooldown_" + username;
        var lastUse     = CPH.GetGlobalVar<long>(cooldownKey, false);
        var now         = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var elapsed     = now - lastUse;

        if (elapsed < config.Rank.CooldownSeconds)
        {
            if (config.Debug.Verbose)
                CPH.LogInfo("[RANK_ShowCommand] Skip cooldown pour : " + username);
            CPH.SetArgument("rank_skipped",  true);
            CPH.SetArgument("rank_sent",     false);
            CPH.SetArgument("rank_notfound", false);
            CPH.SetArgument("rank_position", 0);
            return true;
        }

        // 5. Profil viewer
        var repo = new UserRepository(config.DataPath);
        var user = repo.LoadUser(username);

        if (user == null)
        {
            CPH.SendMessage(new RankService().FormatNotFoundMessage(displayName));
            CPH.SetGlobalVar(cooldownKey, now, false);
            CPH.SetArgument("rank_skipped",  false);
            CPH.SetArgument("rank_sent",     true);
            CPH.SetArgument("rank_notfound", true);
            CPH.SetArgument("rank_position", 0);
            CPH.SetArgument("rank_level",    0);
            CPH.SetArgument("rank_xp",       0);
            CPH.SetArgument("rank_messages", 0);
            CPH.SetArgument("rank_watchtime",0);
            CPH.SetArgument("rank_title",    "");
            return true;
        }

        // 6. Rang — cache leaderboard prioritaire, fallback scan complet
        var rankService  = new RankService();
        var xpService    = new XpService(repo);
        var titleService = new TitleService();

        var cacheJson  = CPH.GetGlobalVar<string>("xp_leaderboard_cache", false);
        var maxAge     = (long)(config.Leaderboard.IntervalMinutes * 60 * 1.5);
        var rank       = GetRankFromCache(username, cacheJson, maxAge);
        var totalUsers = 0;

        if (rank == 0)
        {
            CPH.LogInfo("[RANK_ShowCommand] Cache absent ou expire — scan complet");
            var allUsers = repo.GetAllUsers();
            var filtered = new List<UserProfile>();
            foreach (var u in allUsers)
                if (!bots.IsExcluded(u.Username))
                    filtered.Add(u);

            totalUsers = filtered.Count;
            rank       = rankService.GetLiveRank(username, filtered);
        }
        var progress = xpService.GetProgress(user);
        var title    = titleService.GetTitle(user.Level, projectPath, config.Theme);

        // 7. Message chat
        CPH.SendMessage(rankService.FormatRankMessage(user, rank, progress, totalUsers, title));
        CPH.SetGlobalVar(cooldownKey, now, false);

        // 8. Exposer
        CPH.SetArgument("rank_skipped",  false);
        CPH.SetArgument("rank_sent",     true);
        CPH.SetArgument("rank_notfound", false);
        CPH.SetArgument("rank_position", rank);
        CPH.SetArgument("rank_level",    user.Level);
        CPH.SetArgument("rank_xp",       user.Xp);
        CPH.SetArgument("rank_messages", user.Messages);
        CPH.SetArgument("rank_watchtime",user.WatchTime);
        CPH.SetArgument("rank_title",    title);

        CPH.LogInfo("[RANK_ShowCommand] " + displayName + " -> Rang #" + rank + "/" + totalUsers + " Niv." + user.Level + " (" + title + ")");

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
