public class CPHInline
{
    public bool Execute()
    {
        CPH.LogInfo("=== SYSTEM_Validate : demarrage ===");

        var ok = true;

        // 1. Vérifier xp_configPath
        var configPath = CPH.GetGlobalVar<string>("xp_configPath", true);

        if (string.IsNullOrEmpty(configPath))
        {
            CPH.LogWarn("[SYSTEM] FAIL — xp_configPath non defini dans les GlobalVars SB");
            CPH.LogWarn("[SYSTEM] Action requise : Settings → Global Variables → Ajouter xp_configPath");
            ok = false;
        }
        else
        {
            CPH.LogInfo("[SYSTEM] OK   — xp_configPath : " + configPath);
        }

        // 2. Vérifier que config.json existe
        if (!string.IsNullOrEmpty(configPath))
        {
            if (!File.Exists(configPath))
            {
                CPH.LogWarn("[SYSTEM] FAIL — config.json introuvable : " + configPath);
                ok = false;
            }
            else
            {
                CPH.LogInfo("[SYSTEM] OK   — config.json existe");
            }
        }

        // 3. Vérifier que config.json est valide JSON
        Config config = null;
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                config   = JsonConvert.DeserializeObject<Config>(json);
                if (config == null) throw new Exception("Deserialisation retourne null");
                CPH.LogInfo("[SYSTEM] OK   — config.json valide (JSON correct)");
            }
            catch (Exception ex)
            {
                CPH.LogWarn("[SYSTEM] FAIL — config.json JSON invalide : " + ex.Message);
                ok = false;
            }
        }

        // 4. Vérifier dataPath
        if (config != null)
        {
            if (string.IsNullOrEmpty(config.DataPath))
            {
                CPH.LogWarn("[SYSTEM] FAIL — dataPath non configure dans config.json");
                ok = false;
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(config.DataPath);
                    CPH.LogInfo("[SYSTEM] OK   — dataPath accessible : " + config.DataPath);

                    var profileCount = Directory.GetFiles(config.DataPath, "*.json").Length;
                    CPH.LogInfo("[SYSTEM] INFO — " + profileCount + " profil(s) dans dataPath");
                }
                catch (Exception ex)
                {
                    CPH.LogWarn("[SYSTEM] FAIL — dataPath inaccessible : " + ex.Message);
                    ok = false;
                }
            }
        }

        // 5. Vérifier excluded-users.json
        if (!string.IsNullOrEmpty(configPath))
        {
            var configDir    = Path.GetDirectoryName(configPath ?? "");
            var projectPath  = Path.GetDirectoryName(configDir ?? "");
            var excludedPath = Path.Combine(projectPath, "configs", "excluded-users.json");

            if (File.Exists(excludedPath))
            {
                try
                {
                    var list  = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(excludedPath));
                    var count = (list != null) ? list.Count : 0;
                    CPH.LogInfo("[SYSTEM] OK   — excluded-users.json : " + count + " entree(s)");
                    if (count == 0)
                        CPH.LogWarn("[SYSTEM] WARN — excluded-users.json vide → fallback bots standard");
                }
                catch (Exception ex)
                {
                    CPH.LogWarn("[SYSTEM] WARN — excluded-users.json invalide : " + ex.Message);
                }
            }
            else
            {
                CPH.LogWarn("[SYSTEM] WARN — excluded-users.json absent → fallback bots standard");
            }

            // 6. Vérifier titles.json
            var titlesPath = Path.Combine(projectPath, "configs", "titles.json");
            if (File.Exists(titlesPath))
            {
                try
                {
                    var titles = JsonConvert.DeserializeObject<List<object>>(File.ReadAllText(titlesPath));
                    var count  = (titles != null) ? titles.Count : 0;
                    if (count == 0)
                        CPH.LogWarn("[SYSTEM] WARN — configs/titles.json vide → themes/default/titles.json sera utilise");
                    else
                        CPH.LogInfo("[SYSTEM] OK   — configs/titles.json : " + count + " palier(s)");
                }
                catch (Exception ex)
                {
                    CPH.LogWarn("[SYSTEM] WARN — configs/titles.json invalide : " + ex.Message);
                }
            }
        }

        // 7. Vérifier le cache leaderboard
        var cacheJson = CPH.GetGlobalVar<string>("xp_leaderboard_cache", false);
        if (!string.IsNullOrEmpty(cacheJson))
            CPH.LogInfo("[SYSTEM] INFO — Cache leaderboard present en GlobalVar SB");
        else
            CPH.LogInfo("[SYSTEM] INFO — Cache leaderboard absent (normal si LEADERBOARD_Update n'a pas encore tourne)");

        // 8. Résumé
        CPH.LogInfo("=== SYSTEM_Validate : " + (ok ? "SUCCES — installation valide" : "ECHEC — voir les FAIL ci-dessus") + " ===");

        return ok;
    }
}
