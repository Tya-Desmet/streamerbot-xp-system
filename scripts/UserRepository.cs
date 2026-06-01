// ============================================================
// UserRepository.cs — Streamer.bot XP System
// ============================================================
// UTILISATION DANS STREAMER.BOT :
//   Ce fichier est une référence source.
//   Coller UserProfile + UserRepository au-dessus de CPHInline
//   dans chaque action C# qui en a besoin.
//
// Exemple d'action Streamer.bot :
//   [UserProfile class]
//   [UserRepository class]
//   public class CPHInline {
//       public bool Execute() {
//           var repo = new UserRepository(CPH.GetGlobalVar<string>("dataPath", true));
//           ...
//           return true;
//       }
//   }
// ============================================================

using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

// Modèle utilisateur — structure exacte du JSON sur disque
// Champs alignés avec /data/users/{username}.json (voir README)
public class UserProfile
{
    public string Username             { get; set; }
    public string DisplayName          { get; set; }
    public int    Xp                   { get; set; }
    public int    Level                { get; set; }
    public int    Messages             { get; set; }
    public int    WatchTime            { get; set; }   // minutes cumulées regardées
    public long   LastMessageTimestamp { get; set; }
    public long   LastWatchTimestamp   { get; set; }   // Unix — dernier cycle watchtime reçu
    public int    WatchStreak          { get; set; }   // cycles consécutifs — bonus fidélité
}

// Repository — lecture et écriture JSON uniquement
// Aucune logique XP, aucune logique overlay, aucun calcul
public class UserRepository
{
    private readonly string _dataPath;

    public UserRepository(string dataPath)
    {
        _dataPath = dataPath;
        Directory.CreateDirectory(_dataPath);
    }

    // Vérifie si le fichier {username}.json existe
    public bool UserExists(string username)
    {
        return File.Exists(GetFilePath(username));
    }

    // Crée un profil vierge pour un nouveau viewer
    public UserProfile CreateUser(string username, string displayName = null)
    {
        var user = new UserProfile
        {
            Username             = username,
            DisplayName          = displayName ?? username,
            Xp                   = 0,
            Level                = 1,
            Messages             = 0,
            WatchTime            = 0,
            Rank                 = 0,
            LastMessageTimestamp = 0,
            LastWatchTimestamp   = 0,
            WatchStreak          = 0
        };

        SaveUser(user);
        return user;
    }

    // Charge le profil depuis le JSON — retourne null si introuvable
    public UserProfile LoadUser(string username)
    {
        var path = GetFilePath(username);

        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<UserProfile>(json);
    }

    // Persiste le profil sur le disque
    public void SaveUser(UserProfile user)
    {
        var json    = JsonConvert.SerializeObject(user, Formatting.Indented);
        var path    = GetFilePath(user.Username);
        var tmpPath = path + ".tmp";

        try
        {
            File.WriteAllText(tmpPath, json);

            if (File.Exists(path))
                File.Delete(path);

            File.Move(tmpPath, path);
        }
        catch (Exception ex)
        {
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
            throw;
        }
    }

    // Charge tous les profils du dossier — pour leaderboard, export, stats globales
    public List<UserProfile> GetAllUsers()
    {
        var users = new List<UserProfile>();
        var files = Directory.GetFiles(_dataPath, "*.json");

        foreach (var file in files)
        {
            if (file.EndsWith(".tmp")) continue;
            try
            {
                var json = File.ReadAllText(file);
                var user = JsonConvert.DeserializeObject<UserProfile>(json);
                if (user != null) users.Add(user);
            }
            catch (Exception ex)
            {
                try
                {
                    var errPath = Path.Combine(_dataPath, "_errors.log");
                    File.AppendAllText(errPath,
                        "[" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "] "
                        + "Fichier corrompu : " + Path.GetFileName(file)
                        + " — " + ex.Message + "\n");
                }
                catch { }
            }
        }

        return users;
    }

    private string GetFilePath(string username)
    {
        return Path.Combine(_dataPath, username + ".json");
    }
}
