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
            CPH.LogWarn("[REWARD_BonusXp] user_username absent — USER_GetOrCreate requis");
            return false;
        }
        var username = args["user_username"].ToString();

        // 3. Configuration
        var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config     = new ConfigService(CPH).LoadConfig(configPath);

        if (config.Rewards.BonusXpEnabled != true)
        {
            CPH.LogInfo("[REWARD_BonusXp] Feature desactivee dans config.json");
            CPH.SetArgument("reward_bonus_applied", false);
            return true;
        }

        // 4. Charger profil
        var repo = new UserRepository(config.DataPath);
        var user = repo.LoadUser(username);

        if (user == null)
        {
            CPH.LogWarn("[REWARD_BonusXp] Profil introuvable pour '" + username + "'");
            return false;
        }

        // 5. Appliquer le bonus
        var rewards = new RewardService();
        var now     = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var result  = rewards.ApplyBonus(
            user,
            config.Rewards.BonusXpMultiplier,
            config.Rewards.BonusXpDurationMinutes,
            now);

        repo.SaveUser(user);

        // 6. Message chat
        var displayName = string.IsNullOrEmpty(user.DisplayName) ? username : user.DisplayName;
        var message     = result.WasAlreadyActive
            ? "@" + displayName + " — Double XP prolonge ! x" + result.MultiplierApplied + " pendant encore " + result.ExpiresInMinutes + " min"
            : "@" + displayName + " — Double XP active ! x" + result.MultiplierApplied + " pendant " + result.ExpiresInMinutes + " min";

        CPH.SendMessage(message);
        CPH.LogInfo("[REWARD_BonusXp] " + username + " — x" + result.MultiplierApplied + " pendant " + result.ExpiresInMinutes + " min");

        // 7. Exposer
        CPH.SetArgument("reward_bonus_applied",      true);
        CPH.SetArgument("reward_multiplier",         result.MultiplierApplied);
        CPH.SetArgument("reward_expires_in_minutes", result.ExpiresInMinutes);
        CPH.SetArgument("reward_was_already_active", result.WasAlreadyActive);
        CPH.SetArgument("reward_message",            message);

        return true;
    }
}
