public class CPHInline
{
    public bool Execute()
    {
        // 1. Identité
        if (!args.ContainsKey("userName") || args["userName"] == null)
        {
            CPH.LogWarn("[DAILY_CheckIn] userName absent");
            return false;
        }

        var username    = args["userName"].ToString().Trim().ToLowerInvariant();
        var displayName = args.ContainsKey("userDisplayName") && args["userDisplayName"] != null
                              ? args["userDisplayName"].ToString().Trim()
                              : username;

        // 2. Configuration
        var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);
        var config     = new ConfigService(CPH).LoadConfig(configPath);

        if (config.CheckIn == null || config.CheckIn.Enabled != true)
        {
            CPH.LogInfo("[DAILY_CheckIn] Feature desactivee dans config.json");
            return true;
        }

        if (string.IsNullOrEmpty(config.DataPath))
        {
            CPH.LogWarn("[DAILY_CheckIn] dataPath non configure");
            return false;
        }

        // 3. Exclusion bots
        var configDir   = Path.GetDirectoryName(configPath ?? "");
        var projectPath = Path.GetDirectoryName(configDir ?? "");
        var bots = new BotExclusionService(
            projectPath,
            config.Bots.BroadcasterName,
            config.Bots.ExcludeBroadcaster == true);

        if (bots.IsExcluded(username)) return true;

        // 4. Charger profil
        var repo = new UserRepository(config.DataPath);
        var user = repo.LoadUser(username);

        if (user == null)
        {
            CPH.SendMessage("@" + displayName + " — Tu n'as pas encore de profil ! Envoie un message dans le chat d'abord. PogChamp");
            CPH.SetArgument("checkin_done",          false);
            CPH.SetArgument("checkin_already_done",  false);
            CPH.SetArgument("checkin_xp",            0);
            CPH.SetArgument("checkin_count",         0);
            CPH.SetArgument("checkin_card_complete", false);
            return true;
        }

        // 5. Guard unicité jour
        var todayDay = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400);

        if (user.LastCheckInDay == todayDay)
        {
            CPH.SendMessage("@" + displayName + " — Check-in deja fait aujourd'hui ! Reviens demain. PauseChamp (" + user.CheckInCount + "/" + config.CheckIn.CardSize + " cases)");
            CPH.SetArgument("checkin_done",          false);
            CPH.SetArgument("checkin_already_done",  true);
            CPH.SetArgument("checkin_xp",            0);
            CPH.SetArgument("checkin_count",         user.CheckInCount);
            CPH.SetArgument("checkin_card_complete", false);
            return true;
        }

        // 6. Cocher une case
        user.CheckInCount++;
        user.TotalCheckIns++;
        user.LastCheckInDay = todayDay;

        var cardSize     = config.CheckIn.CardSize > 0 ? config.CheckIn.CardSize : 10;
        var cardComplete = user.CheckInCount >= cardSize;
        var xpGained     = cardComplete ? config.CheckIn.XpCardComplete : config.CheckIn.XpPerCheckin;

        if (cardComplete)
            user.CheckInCount = 0;

        // 7. Attribuer XP
        var xpService = new XpService(repo);
        var xpResult  = xpService.AddXp(user, xpGained, "reward");

        // 8. Message chat
        string message;
        if (cardComplete)
        {
            message = "@" + displayName + " — CARTE COMPLETE ! +" + xpGained + " XP bonus ! La carte repart a zero. SeemsGood";
        }
        else
        {
            var progress = user.CheckInCount + "/" + cardSize;
            message = "@" + displayName + " — Check-in ! +" + xpGained + " XP (" + progress + " cases) PogChamp";
        }

        CPH.SendMessage(message);

        // 9. Animation WebSocket overlay dédié
        var progressForOverlay = cardComplete ? cardSize : user.CheckInCount;
        CPH.WebsocketBroadcastJson(JsonConvert.SerializeObject(new Dictionary<string, object>
        {
            { "event",         cardComplete ? "checkIn_cardComplete" : "checkIn_normal" },
            { "username",      displayName },
            { "xpGained",      xpGained },
            { "count",         progressForOverlay },
            { "cardSize",      cardSize },
            { "totalCheckins", user.TotalCheckIns },
            { "durationMs",    config.CheckIn.AnimationDurationMs }
        }));

        CPH.LogInfo("[DAILY_CheckIn] " + username + " — +" + xpGained + " XP, case " + progressForOverlay + "/" + cardSize + (cardComplete ? " (carte complete!)" : ""));

        // 10. Exposer
        CPH.SetArgument("checkin_done",          true);
        CPH.SetArgument("checkin_already_done",  false);
        CPH.SetArgument("checkin_xp",            xpGained);
        CPH.SetArgument("checkin_count",         progressForOverlay);
        CPH.SetArgument("checkin_card_complete", cardComplete);

        return true;
    }
}
