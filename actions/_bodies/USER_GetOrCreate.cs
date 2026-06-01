public class CPHInline
{
    public bool Execute()
    {
        // 1. Identité du viewer
        if (!args.ContainsKey("userName") || args["userName"] == null)
        {
            CPH.LogWarn("[USER_GetOrCreate] userName absent des args");
            return false;
        }

        var username    = args["userName"].ToString().Trim();
        var displayName = args.ContainsKey("userDisplayName") && args["userDisplayName"] != null
                              ? args["userDisplayName"].ToString().Trim()
                              : username;

        if (string.IsNullOrEmpty(username))
        {
            CPH.LogWarn("[USER_GetOrCreate] userName vide");
            return false;
        }

        // 2. Configuration
        var configPath  = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config      = new ConfigService(CPH).LoadConfig(configPath);
        var configDir   = Path.GetDirectoryName(configPath ?? "");
        var projectPath = Path.GetDirectoryName(configDir ?? "");

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[USER_GetOrCreate] dataPath non configuré dans configs/config.json");
            return false;
        }

        // 3. Vérification exclusion — AVANT toute création de profil
        var bots = new BotExclusionService(
            projectPath,
            config.Bots.BroadcasterName,
            config.Bots.ExcludeBroadcaster == true);

        if (bots.IsExcluded(username))
        {
            CPH.SetArgument("user_excluded", true);
            return true;
        }

        CPH.SetArgument("user_excluded", false);

        // 4. Charger ou créer le profil
        var repo  = new UserRepository(config.DataPath);
        var isNew = false;
        var user  = repo.LoadUser(username);

        if (user == null)
        {
            user  = repo.CreateUser(username, displayName);
            isNew = true;
            CPH.LogInfo("[USER_GetOrCreate] Nouveau joueur créé : " + displayName + " (" + username + ")");
        }

        // 5. Exposer le profil aux sub-actions suivantes
        CPH.SetArgument("user_username",      user.Username);
        CPH.SetArgument("user_displayName",   user.DisplayName ?? displayName);
        CPH.SetArgument("user_xp",            user.Xp);
        CPH.SetArgument("user_level",         user.Level);
        CPH.SetArgument("user_messages",      user.Messages);
        CPH.SetArgument("user_watchtime",     user.WatchTime);
        CPH.SetArgument("user_lastTimestamp", user.LastMessageTimestamp);
        CPH.SetArgument("user_isNew",         isNew);

        return true;
    }
}
