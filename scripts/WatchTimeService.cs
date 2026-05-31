// ============================================================
// WatchTimeService.cs — Streamer.bot XP System
// ============================================================
// RESPONSABILITÉ : Vérifier l'éligibilité watchtime d'un viewer.
//                  Ne lit et n'écrit jamais le disque directement.
//
// UTILISATION DANS STREAMER.BOT :
//   Coller WatchTimeResult + WatchTimeService au-dessus de CPHInline
//   dans l'action XP_WatchTime.
//
// FLUX DANS XP_WatchTime :
//   1. UserRepository.LoadUser() ou CreateUser()   → UserProfile
//   2. WatchTimeService.IsEligible()               → bool
//   3. Si true : XpService.AddWatchTimeXp()        → XpResult
//
// AUCUNE logique XP — AUCUNE écriture disque — AUCUN overlay
// ============================================================

// Résultat d'un cycle watchtime pour un viewer — pour logging et debug
public class WatchTimeResult
{
    public string Username    { get; set; }
    public int    XpAwarded   { get; set; }
    public bool   WasEligible { get; set; }
    public string SkipReason  { get; set; }  // vide si éligible
}

public class WatchTimeService
{
    // Vérifie si le viewer peut recevoir de l'XP watchtime ce cycle.
    //
    // Retourne false si :
    //   - user est null
    //   - LastWatchTimestamp est trop récent (< intervalMinutes - TOLERANCE)
    //
    // Tolérance de 10 secondes pour absorber les imprécisions des timers
    // Streamer.bot (un timer de 5 min peut déclencher à 4 min 52 sec).
    private const int TimerToleranceSeconds = 10;

    public bool IsEligible(UserProfile user, int intervalMinutes, long nowSeconds)
    {
        if (user == null)
            return false;

        var intervalSeconds  = intervalMinutes * 60;
        var elapsed          = nowSeconds - user.LastWatchTimestamp;

        return elapsed >= (intervalSeconds - TimerToleranceSeconds);
    }

    // Construit un WatchTimeResult pour logging
    public WatchTimeResult BuildResult(string username, int xpAwarded, bool eligible, string skipReason = "")
    {
        return new WatchTimeResult
        {
            Username    = username,
            XpAwarded   = eligible ? xpAwarded : 0,
            WasEligible = eligible,
            SkipReason  = skipReason
        };
    }
}
