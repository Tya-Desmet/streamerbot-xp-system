public class CPHInline
{
    public bool Execute()
    {
        // 1. Vérifier exclusion
        var excluded = args.ContainsKey("user_excluded")
                       && args["user_excluded"] != null
                       && (bool)args["user_excluded"] == true;
        if (excluded) return true;

        // 2. Username
        if (!args.ContainsKey("user_username") || args["user_username"] == null)
        {
            CPH.LogWarn("[REWARD_GrantXp] user_username absent — USER_GetOrCreate requis");
            return false;
        }
        var username = args["user_username"].ToString();

        // 3. Configuration
        var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config     = new ConfigService(CPH).LoadConfig(configPath);

        if (config.Rewards.GrantXpEnabled != true)
        {
            CPH.LogInfo("[REWARD_GrantXp] Feature desactivee dans config.json");
            CPH.SetArgument("reward_xp_granted", 0);
            return true;
        }

        // 4. Charger profil
        var repo = new UserRepository(config.DataPath);
        var user = repo.LoadUser(username);

        if (user == null)
        {
            CPH.LogWarn("[REWARD_GrantXp] Profil introuvable pour '" + username + "'");
            return false;
        }

        // 5. Octroyer XP
        var xpService = new XpService(repo);
        var xpResult  = xpService.AddXp(user, config.Rewards.GrantXpAmount, "reward");

        // 6. Message chat
        var displayName = string.IsNullOrEmpty(user.DisplayName) ? username : user.DisplayName;
        var message     = "@" + displayName + " — Bonus XP ! +" + config.Rewards.GrantXpAmount + " XP PogChamp";

        CPH.SendMessage(message);
        CPH.LogInfo("[REWARD_GrantXp] " + username + " — +" + config.Rewards.GrantXpAmount + " XP");

        // 7. Exposer
        CPH.SetArgument("reward_xp_granted", xpResult.XpAdded);
        CPH.SetArgument("reward_xp_total",   xpResult.TotalXp);
        CPH.SetArgument("reward_isLevelUp",  xpResult.IsLevelUp);
        CPH.SetArgument("reward_message",    message);

        return true;
    }
}
