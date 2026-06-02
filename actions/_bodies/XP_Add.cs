public class CPHInline
{
    public bool Execute()
    {
        // 1. Vérifier le flag d'exclusion posé par USER_GetOrCreate
        var excluded = args.ContainsKey("user_excluded")
                       && args["user_excluded"] != null
                       && (bool)args["user_excluded"] == true;

        if (excluded)
        {
            CPH.SetArgument("xp_skipped",    true);
            CPH.SetArgument("xp_skipReason", "excluded");
            return true;
        }

        // 2. Username posé par USER_GetOrCreate
        if (!args.ContainsKey("user_username") || args["user_username"] == null)
        {
            CPH.LogWarn("[XP_Add] user_username absent — USER_GetOrCreate doit être la sub-action précédente");
            return false;
        }

        var username = args["user_username"].ToString();

        // 3. Message brut
        if (!args.ContainsKey("rawInput") || args["rawInput"] == null)
        {
            CPH.LogWarn("[XP_Add] rawInput absent — vérifier le déclencheur Twitch Chat Message");
            return false;
        }

        var rawInput = args["rawInput"].ToString();

        // 4. Configuration
        var configPath   = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config       = new ConfigService(CPH).LoadConfig(configPath);

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[XP_Add] dataPath non configuré dans configs/config.json");
            return false;
        }

        // 5. Validation anti-spam
        var validation = new ValidationService(CPH, config.Xp.CooldownSeconds, config.Xp.MinMessageLength);
        var check      = validation.ValidateMessage(username, rawInput);

        if (!check.IsValid)
        {
            if (config.Debug.Verbose)
                CPH.LogInfo("[XP_Add] Skip : " + check.Reason + " pour " + username);
            CPH.SetArgument("xp_skipped",    true);
            CPH.SetArgument("xp_skipReason", check.Reason);
            return true;
        }

        // 6. Charger profil (une seule fois)
        var repo = new UserRepository(config.DataPath);
        var user = repo.LoadUser(username);

        if (user == null)
        {
            CPH.LogWarn("[XP_Add] Profil introuvable pour '" + username + "' malgré USER_GetOrCreate");
            return false;
        }

        // 7. Vérifier bonus actif + calculer XP effectif
        var rewards     = new RewardService();
        var now         = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var multiplier  = rewards.GetCurrentMultiplier(user, now);
        var effectiveXp = (int)Math.Round(config.Xp.PerMessage * multiplier);

        var xpService = new XpService(repo);
        var xpResult  = xpService.AddXp(user, effectiveXp, "chat");

        // 8. Progression XP dans le niveau (user déjà en mémoire — zéro I/O)
        var progress = xpService.GetProgress(user);

        // 9. Exposer les résultats
        CPH.SetArgument("xp_skipped",      false);
        CPH.SetArgument("xp_skipReason",   "");
        CPH.SetArgument("xp_added",        xpResult.XpAdded);
        CPH.SetArgument("xp_total",        xpResult.TotalXp);
        CPH.SetArgument("xp_oldLevel",     xpResult.OldLevel);
        CPH.SetArgument("xp_newLevel",     xpResult.NewLevel);
        CPH.SetArgument("xp_isLevelUp",    xpResult.IsLevelUp);
        CPH.SetArgument("xp_xpIntoLevel",  progress.XpIntoLevel);
        CPH.SetArgument("xp_xpForNext",    progress.XpForNext);
        CPH.SetArgument("xp_percentage",   progress.Percentage);
        CPH.SetArgument("xp_multiplier",   multiplier);
        CPH.SetArgument("xp_effective",    effectiveXp);
        CPH.SetArgument("xp_from_chat",    user.XpFromChat);
        CPH.SetArgument("xp_from_watch",   user.XpFromWatch);
        CPH.SetArgument("xp_from_rewards", user.XpFromRewards);
        CPH.SetArgument("xp_bonus_active", multiplier > 1.0f);

        if (xpResult.IsLevelUp)
            CPH.LogInfo("[XP_Add] LEVEL UP ! " + username + " : Niv. " + xpResult.OldLevel + " -> " + xpResult.NewLevel);

        return true;
    }
}
