// ============================================================
// ValidationService.cs — Streamer.bot XP System
// ============================================================
// UTILISATION DANS STREAMER.BOT :
//   Coller ValidationResult + ValidationService au-dessus de CPHInline
//   dans chaque action C# qui en a besoin.
//   Passer CPH au constructeur pour la gestion du cooldown.
//
// Exemple d'action Streamer.bot :
//   [ValidationResult class]
//   [ValidationService class]
//   public class CPHInline {
//       public bool Execute() {
//           var validation = new ValidationService(CPH, cooldownSeconds: 30, minLength: 2);
//           var result = validation.ValidateMessage(userName, rawInput);
//           if (!result.IsValid) { CPH.LogInfo($"Rejeté : {result.Reason}"); return true; }
//           // continuer vers XP_Add, watchtime, etc.
//           return true;
//       }
//   }
// ============================================================

using System;

// Résultat de validation — IsValid + code raison pour logging et réactions futures
public class ValidationResult
{
    public bool   IsValid { get; set; }
    public string Reason  { get; set; }

    // Raisons possibles : "command" | "too_short" | "cooldown" | ""
}

// Service de validation — anti-spam, cooldown, commandes, longueur
// Aucune logique XP, aucune logique overlay
// Réutilisable par : XP chat, watchtime, rewards, mini-jeux
public class ValidationService
{
    private readonly IInlineInvokeProxy _CPH;
    private readonly int _cooldownSeconds;
    private readonly int _minLength;

    public ValidationService(IInlineInvokeProxy CPH, int cooldownSeconds = 30, int minLength = 2)
    {
        _CPH             = CPH;
        _cooldownSeconds = cooldownSeconds;
        _minLength       = minLength;
    }

    // Point d'entrée — valide un message de chat Twitch
    public ValidationResult ValidateMessage(string username, string message)
    {
        if (IsCommand(message))
            return Reject("command");

        if (IsTooShort(message))
            return Reject("too_short");

        if (IsOnCooldown(username))
            return Reject("cooldown");

        SetCooldown(username);
        return Accept();
    }

    // Détecte les commandes Twitch (!command, /me, .color...)
    private bool IsCommand(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return true;

        var first = message.TrimStart()[0];
        return first == '!' || first == '/' || first == '.';
    }

    // Vérifie la longueur minimale après nettoyage des espaces
    private bool IsTooShort(string message)
    {
        return message.Trim().Length < _minLength;
    }

    // Vérifie si l'utilisateur est encore en cooldown via Global Variables Streamer.bot
    // GetGlobalVar retourne 0 si la clé n'existe pas — premier message toujours autorisé
    private bool IsOnCooldown(string username)
    {
        var lastTime = _CPH.GetGlobalVar<long>("cooldown_" + username, false);
        var now      = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return (now - lastTime) < _cooldownSeconds;
    }

    // Enregistre le timestamp Unix du dernier message valide
    private void SetCooldown(string username)
    {
        _CPH.SetGlobalVar("cooldown_" + username, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), false);
    }

    private ValidationResult Accept()
        => new ValidationResult { IsValid = true, Reason = "" };

    private ValidationResult Reject(string reason)
        => new ValidationResult { IsValid = false, Reason = reason };
}
