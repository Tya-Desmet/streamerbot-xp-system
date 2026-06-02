public class CPHInline
{
    public bool Execute()
    {
        // 1. Lire les args du trigger
        object rawIsLive;
        var isLive = args.TryGetValue("isLive", out rawIsLive)
                     && rawIsLive != null
                     && Convert.ToBoolean(rawIsLive);

        object rawIsTest;
        var isTest = args.TryGetValue("isTest", out rawIsTest)
                     && rawIsTest != null
                     && Convert.ToBoolean(rawIsTest);

        // 2. Configuration + chemins (avant le guard offline pour accéder à countOffline)
        var configPath  = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config      = new ConfigService(CPH).LoadConfig(configPath);
        var configDir   = Path.GetDirectoryName(configPath ?? "");
        var projectPath = Path.GetDirectoryName(configDir ?? "");

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[XP_WatchTime_V2] dataPath non configuré");
            return false;
        }

        // 3. Guard offline — config.Watchtime.CountOffline permet d'accumuler hors live
        if (!isLive && !isTest && config.Watchtime.CountOffline != true)
        {
            if (config.Debug.Verbose)
                CPH.LogInfo("[XP_WatchTime_V2] Stream offline — cycle ignoré");
            CPH.SetArgument("watch_processed",      0);
            CPH.SetArgument("watch_xp_distributed", 0);
            CPH.SetArgument("watch_level_ups",      0);
            CPH.SetArgument("watch_skipped",        0);
            CPH.SetArgument("watch_presence_count", 0);
            return true;
        }

        if (config.Watchtime.Enabled == false)
        {
            if (config.Debug.Verbose)
                CPH.LogInfo("[XP_WatchTime_V2] Watchtime désactivé dans config.json");
            return true;
        }

        // 4. Construire le set de présence depuis la liste SB
        var presentSet = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        object rawUsers;
        if (args.TryGetValue("users", out rawUsers) && rawUsers != null)
        {
            var sbList = rawUsers as List<Dictionary<string, object>>;

            // Fallback : re-sérialiser si le cast direct échoue (type interne SB)
            if (sbList == null)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(rawUsers);
                    sbList   = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                }
                catch { }
            }

            if (sbList != null)
            {
                foreach (var u in sbList)
                {
                    var login = ExtractUsername(u);
                    if (!string.IsNullOrEmpty(login))
                        presentSet[login] = true;
                }
            }
            else
            {
                CPH.LogDebug("[XP_WatchTime_V2] Type inattendu pour args[users] : " + rawUsers.GetType().Name + " — mode activité seule");
            }
        }
        else
        {
            CPH.LogDebug("[XP_WatchTime_V2] args[users] absent — mode activité seule");
        }

        // 5. Services
        var bots         = new BotExclusionService(projectPath, config.Bots.BroadcasterName, config.Bots.ExcludeBroadcaster == true);
        var repo         = new UserRepository(config.DataPath);
        var xpService    = new XpService(repo);
        var watchService = new WatchTimeService();
        var rewards      = new RewardService();

        var now                   = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var interval              = config.Watchtime.IntervalMinutes;
        var xpAmount              = config.Xp.PerWatchInterval;
        var activityWindowSeconds = (long)(interval * 60 * 2);
        var streakEnabled         = config.Watchtime.StreakEnabled == true;

        var allUsers = repo.GetAllUsers();

        CPH.LogInfo("[XP_WatchTime_V2] Cycle start — "
            + allUsers.Count + " profils, "
            + presentSet.Count + " présents SB"
            + (isTest ? " [TEST]" : ""));

        if (allUsers.Count == 0)
        {
            CPH.LogInfo("[XP_WatchTime_V2] Aucun profil trouvé — cycle terminé");
            CPH.SetArgument("watch_processed",      0);
            CPH.SetArgument("watch_xp_distributed", 0);
            CPH.SetArgument("watch_level_ups",      0);
            CPH.SetArgument("watch_skipped",        0);
            CPH.SetArgument("watch_presence_count", presentSet.Count);
            return true;
        }

        var processed = 0; var xpTotal = 0; var levelUps = 0; var skipped = 0;

        // 6. Boucle principale
        foreach (var user in allUsers)
        {
            if (bots.IsExcluded(user.Username)) { skipped++; continue; }
            if (!watchService.IsEligible(user, interval, now)) { skipped++; continue; }

            var inPresenceList = presentSet.ContainsKey(user.Username);
            var recentChat     = (now - user.LastMessageTimestamp) <= activityWindowSeconds;

            if (!inPresenceList && !recentChat) { skipped++; continue; }

            if (streakEnabled)
            {
                var prevGap = now - user.LastWatchTimestamp;
                var maxGap  = activityWindowSeconds;
                user.WatchStreak = (user.LastWatchTimestamp > 0 && prevGap <= maxGap)
                                   ? user.WatchStreak + 1
                                   : 1;
            }

            var bonusMultiplier = rewards.GetCurrentMultiplier(user, now);
            var baseXp          = xpAmount + (streakEnabled ? StreakBonus(user.WatchStreak) : 0);
            var effectiveXp     = (int)Math.Round(baseXp * bonusMultiplier);
            var result          = xpService.AddWatchTimeXp(user, effectiveXp, interval, now);
            if (result == null) { skipped++; continue; }

            processed++;
            xpTotal += result.XpAdded;

            if (result.IsLevelUp)
            {
                levelUps++;
                CPH.LogInfo("[XP_WatchTime_V2] LEVEL UP ! "
                    + user.Username + " : Niv. " + result.OldLevel + " -> " + result.NewLevel);
            }
        }

        // 7. Exposer résultats
        CPH.SetArgument("watch_processed",      processed);
        CPH.SetArgument("watch_xp_distributed", xpTotal);
        CPH.SetArgument("watch_level_ups",      levelUps);
        CPH.SetArgument("watch_skipped",        skipped);
        CPH.SetArgument("watch_presence_count", presentSet.Count);

        CPH.LogInfo("[XP_WatchTime_V2] Cycle terminé — "
            + processed + " traités, "
            + xpTotal   + " XP, "
            + levelUps  + " level-ups, "
            + skipped   + " ignorés, "
            + presentSet.Count + " présents SB");

        return true;
    }

    private static string ExtractUsername(Dictionary<string, object> user)
    {
        var keys = new string[] { "login", "userName", "username", "name", "user" };
        foreach (var key in keys)
        {
            object val;
            if (user.TryGetValue(key, out val) && val != null)
            {
                var s = val.ToString().Trim();
                if (!string.IsNullOrEmpty(s)) return s.ToLowerInvariant();
            }
        }
        return null;
    }

    private static int StreakBonus(int streak)
    {
        if (streak >= 24) return 5;
        if (streak >= 12) return 3;
        if (streak >= 6)  return 2;
        if (streak >= 3)  return 1;
        return 0;
    }
}
