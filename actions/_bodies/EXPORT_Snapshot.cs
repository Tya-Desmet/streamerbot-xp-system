public class CPHInline
{
    public bool Execute()
    {
        // 1. Configuration
        var configPath  = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config      = new ConfigService(CPH).LoadConfig(configPath);
        var configDir   = Path.GetDirectoryName(configPath ?? "");
        var projectPath = Path.GetDirectoryName(configDir ?? "");

        // 2. Garde-fou d'inocuité — désactivé par défaut
        if (config.Export == null || config.Export.Enabled != true)
        {
            CPH.LogInfo("[EXPORT_Snapshot] export.enabled=false — aucun export (no-op)");
            CPH.SetArgument("export_enabled", false);
            return true;
        }

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[EXPORT_Snapshot] dataPath non configuré — export annulé");
            return false;
        }

        var exp       = new ExportService();
        var exportDir = exp.ResolveExportDir(config.Export.Path, projectPath);
        var now       = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var season    = config.Export.Season;

        // 3. Charger + filtrer (lecture seule)
        var repo     = new UserRepository(config.DataPath);
        var allUsers = repo.GetAllUsers();

        var bots = new BotExclusionService(
                       projectPath,
                       config.Bots.BroadcasterName,
                       config.Bots.ExcludeBroadcaster == true);

        var filtered = new List<UserProfile>();
        foreach (var u in allUsers)
            if (!bots.IsExcluded(u.Username))
                filtered.Add(u);

        // 4. Trier (Level↓ XP↓ WatchTime↓) — réutilise la gateway de tri
        var xp     = new XpService(repo);
        var ranked = xp.PrepareLeaderboard(filtered);

        var titles = new TitleService();

        // 5. meta.json
        exp.WriteJson(
            Path.Combine(exportDir, "meta.json"),
            exp.BuildMeta(1, now, season, config.Export.StreamerName));

        // 6. leaderboard.json (Top N)
        var topCount = ranked.Count < config.Export.TopCount ? ranked.Count : config.Export.TopCount;
        var players  = new List<PublicLeaderboardEntry>();
        for (var i = 0; i < topCount; i++)
        {
            var e     = ranked[i];
            var title = titles.GetTitle(e.Level, projectPath, config.Theme);
            players.Add(exp.BuildLeaderboardEntry(e, title));
        }
        exp.WriteJson(
            Path.Combine(exportDir, "leaderboard.json"),
            exp.BuildLeaderboardFile(players, now, season));

        // 7. profils publics (optionnel)
        var profilesWritten = 0;
        if (config.Export.WriteProfiles == true)
        {
            // rang issu du classement complet (ranked[i].Rank), pas seulement du Top N
            for (var i = 0; i < ranked.Count; i++)
            {
                var e    = ranked[i];
                var user = repo.LoadUser(e.Username);
                if (user == null) continue;

                var progress = xp.GetProgress(user);
                var title    = titles.GetTitle(user.Level, projectPath, config.Theme);
                var profile  = exp.BuildPublicProfile(user, progress, e.Rank, title);

                var ok = exp.WriteJson(
                    Path.Combine(exportDir, "users", user.Username + ".json"),
                    profile);
                if (ok) profilesWritten++;
            }
        }

        // 8. Exposer le résultat
        CPH.SetArgument("export_enabled", true);
        CPH.SetArgument("export_dir", exportDir);
        CPH.SetArgument("export_players", players.Count);
        CPH.SetArgument("export_profiles", profilesWritten);
        CPH.LogInfo("[EXPORT_Snapshot] OK — " + players.Count + " joueurs, "
                    + profilesWritten + " profils → " + exportDir);

        return true;
    }
}
