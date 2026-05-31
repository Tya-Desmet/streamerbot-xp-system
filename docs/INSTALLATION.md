# Guide d'installation

Streamer.bot XP System — de zéro à fonctionnel en moins de 20 minutes.

---

## Prérequis

- **Streamer.bot 1.0.4** (version stable actuelle)
- **OBS Studio 29.0+**
- **Windows** (chemins avec backslashs)
- Compte Twitch connecté dans Streamer.bot

---

## 1. Préparer le projet

1. Placer le dossier `streamerbot-xp-system` dans un emplacement stable (ex : `C:\Stream\`)
2. Créer manuellement `data/users/` à l'intérieur du projet

```
streamerbot-xp-system/
└── data/
    └── users/    ← créer ce dossier vide
```

---

## 2. Configurer config.json

Ouvrir `configs/config.json` et renseigner `dataPath` :

```json
{
  "dataPath": "C:\\Stream\\streamerbot-xp-system\\data\\users"
}
```

⚠ Les backslashs doivent être **doublés** (`\\`). Le dossier `data/users/` doit exister.

Le reste du fichier peut être laissé intact — toutes les valeurs ont des défauts fonctionnels.

→ Référence complète : [CONFIGURATION.md](CONFIGURATION.md)

---

## 3. Configurer Streamer.bot

### 3a. Activer le serveur WebSocket

```
Servers/Clients → WebSocket Server
  ☑ Enabled
  Port : 8080
```

### 3b. Créer la variable globale

```
Settings → Global Variables → Add
  Nom     : xp_configPath
  Valeur  : C:\Stream\streamerbot-xp-system\configs\config.json
  Persist : ☑ (obligatoire)
```

C'est **la seule variable globale** nécessaire. Toute la configuration est dans `config.json`.

---

## 4. Importer les 6 actions C#

Pour chaque action :
`Actions → Add Action → [Nom] → Add Sub-Action → Execute C# Code → coller le fichier → Compile → Save`

---

### `USER_GetOrCreate`
- Fichier : `actions/USER_GetOrCreate.cs`
- Trigger : **aucun** (sous-action interne)

---

### `XP_Add`
- Fichier : `actions/XP_Add.cs`
- Trigger : `Twitch → Chat Message`
- Sub-actions dans l'ordre :
  1. Run Action → `USER_GetOrCreate`
  2. Execute C# Code → `XP_Add.cs`

---

### `LEADERBOARD_Update`
- Fichier : `actions/LEADERBOARD_Update.cs`
- Trigger : `Core → Timer`
  - Intervalle : **5 minutes** (ou valeur de `leaderboard.intervalMinutes` dans config.json)
  - Ajouter un **Delay de 30 000 ms** si `XP_WatchTime_V2` tourne aussi (décalage anti-collision)

---

### `CARD_ShowProfile`
- Fichier : `actions/CARD_ShowProfile.cs`
- Trigger : `Twitch → Channel Point Redemption` (choisir ta récompense de canal)

---

### `RANK_ShowCommand`
- Fichier : `actions/RANK_ShowCommand.cs`
- Trigger : `Twitch → Chat Command → !rank`

---

### `XP_WatchTime_V2`
- Fichier : `actions/XP_WatchTime_V2.cs`
- Trigger : `Twitch → General → Present Viewers`
  - ☑ Activer **"Live Update"**
  - Intervalle : **5 minutes**

> Si l'ancienne action `XP_WatchTime` existe (Timer), la **désactiver** — elle est remplacée.

---

## 5. Configurer OBS

### Browser Source — Leaderboard

```
Sources → Add → Browser
  Nom    : Leaderboard
  URL    : file:///C:/Stream/streamerbot-xp-system/overlays/leaderboard/leaderboard.html
  Width  : 800
  Height : 400
  ☑ Refresh browser when scene becomes active
```

### Browser Source — ProfileCard

```
Sources → Add → Browser
  Nom    : ProfileCard
  URL    : file:///C:/Stream/streamerbot-xp-system/overlays/card/card.html
  Width  : 500
  Height : 200
  ☑ Refresh browser when scene becomes active
```

⚠ Les noms (`Leaderboard`, `ProfileCard`) doivent correspondre exactement aux valeurs dans `config.json` → section `obs`.

---

## 6. Vérification

### Test 1 — Créer un profil

Envoyer un message dans le chat Twitch. Vérifier qu'un fichier apparaît dans `data/users/`.

Log SB attendu :
```
[USER_GetOrCreate] Nouveau joueur créé : ViewerName (viewerlogin)
```

### Test 2 — XP watchtime

Déclencher `XP_WatchTime_V2` via `Test Trigger` dans SB.

Log attendu :
```
[XP_WatchTime_V2] Cycle start — 1 profils, 0 présents SB [TEST]
[XP_WatchTime_V2] Cycle terminé — 1 traités, 5 XP, ...
```

### Test 3 — Leaderboard OBS

Déclencher `LEADERBOARD_Update`. Dans OBS, clic droit sur la Browser Source → Interact → F12.

Log console attendu :
```
[Leaderboard] WebSocket connected
[Leaderboard] updateLeaderboard received — 1 players
```

### Test 4 — Commande !rank

Écrire `!rank` dans le chat. Le bot répond avec rang, niveau, XP, titre et watchtime.

### Test 5 — Profile Card

Racheter la récompense de canal. La card apparaît dans OBS.

---

## 7. Configuration optionnelle

### Bots supplémentaires

Éditer `configs/excluded-users.json` :
```json
["nightbot", "streamelements", "monsuperbot"]
```

### Exclure le broadcaster

Dans `config.json` :
```json
"bots": {
  "excludeBroadcaster": true,
  "broadcasterName": "monlogintwitch"
}
```

### Changer le thème

Dans `config.json` :
```json
"theme": "rpg"
```

Thèmes : `default` · `rpg` · `cyber` · `minimal` · `tokyo` · `sakura`

---

## Dépannage rapide

| Symptôme | Cause probable |
|---|---|
| Aucun fichier dans `data/users/` | `dataPath` incorrect ou dossier inexistant |
| Erreur compile C# `HashSet` ou `Linq` | Utiliser les fichiers V2 (déjà corrigés) |
| Overlay noir dans OBS | WebSocket inactif, ou rafraîchir le cache OBS |
| `!rank` ne répond pas | Trigger manquant, ou viewer sans profil |
| Watchtime non distribué | `XP_WatchTime_V2` non créée, ou Live Update non activé |

→ Guide détaillé : [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
