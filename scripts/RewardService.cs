// ============================================================
// RewardService.cs — Streamer.bot XP System
// ============================================================
// RESPONSABILITÉ :
//   Gérer le cycle de vie du multiplicateur XP temporaire.
//   Ne lit et n'écrit jamais le disque directement.
//   Délègue la persistance à l'action appelante.
//
// UTILISATION :
//   var rewards = new RewardService();
//   var multiplier = rewards.GetCurrentMultiplier(user, now);
//   var result = rewards.ApplyBonus(user, 2.0f, 30, now);
//   repo.SaveUser(user); // ← sauvegarder APRÈS ApplyBonus
//
// AUCUNE logique XP — AUCUNE écriture disque — AUCUN overlay
// ============================================================

public class BonusResult
{
    public float MultiplierApplied { get; set; }
    public long  ExpiresAt         { get; set; }
    public bool  WasAlreadyActive  { get; set; }
    public int   ExpiresInMinutes  { get; set; }
}

public class RewardService
{
    // Retourne le multiplicateur actif.
    // Si le bonus est expiré, remet le profil à 1.0 (lazy cleanup) — NE SAUVEGARDE PAS.
    // L'action appelante doit sauvegarder si le profil a été modifié.
    public float GetCurrentMultiplier(UserProfile user, long nowSeconds)
    {
        if (user == null) return 1.0f;
        if (user.BonusExpiryTimestamp <= 0) return 1.0f;
        if (nowSeconds >= user.BonusExpiryTimestamp)
        {
            user.ActiveBonusMultiplier = 1.0f;
            user.BonusExpiryTimestamp  = 0;
            return 1.0f;
        }
        return user.ActiveBonusMultiplier > 1.0f ? user.ActiveBonusMultiplier : 1.0f;
    }

    public bool IsBonusActive(UserProfile user, long nowSeconds)
    {
        if (user == null) return false;
        return user.ActiveBonusMultiplier > 1.0f
            && user.BonusExpiryTimestamp > 0
            && nowSeconds < user.BonusExpiryTimestamp;
    }

    // Active un multiplicateur sur le profil. Ne sauvegarde pas.
    // L'action appelante doit appeler SaveUser() après.
    public BonusResult ApplyBonus(UserProfile user, float multiplier, int durationMinutes, long nowSeconds)
    {
        var wasActive = IsBonusActive(user, nowSeconds);
        user.ActiveBonusMultiplier = multiplier;
        user.BonusExpiryTimestamp  = nowSeconds + (long)(durationMinutes * 60);

        var minutesRemaining = (int)((user.BonusExpiryTimestamp - nowSeconds) / 60);

        return new BonusResult
        {
            MultiplierApplied = multiplier,
            ExpiresAt         = user.BonusExpiryTimestamp,
            WasAlreadyActive  = wasActive,
            ExpiresInMinutes  = minutesRemaining
        };
    }

    public int GetMinutesRemaining(UserProfile user, long nowSeconds)
    {
        if (!IsBonusActive(user, nowSeconds)) return 0;
        var seconds = user.BonusExpiryTimestamp - nowSeconds;
        return seconds > 0 ? (int)(seconds / 60) : 0;
    }
}
