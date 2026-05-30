# Troubleshooting — Résolution de problèmes

## Diagnostic rapide

Avant de chercher le problème spécifique, vérifie ces 5 points :

1. Streamer.bot est lancé et connecté à Twitch (`Platforms → Twitch → Connected`)
2. Le serveur WebSocket est activé (`Servers/Clients → WebSocket Server → Enabled`)
3. La variable globale `xp_configPath` existe et est cochée **Persisted**
4. `configs/config.json` est syntaxiquement valide (JSON correct)
5. Le dossier `data/users/` existe physiquement sur le disque

---

## L'overlay leaderboard est noir ou vide

**Symptômes :** la Browser Source OBS reste noire, ne charge pas, ou affiche un fond sans données.

### Cause 1 — WebSocket non connecté

Ouvre la console OBS : clic droit sur la source → **Interact** → appuie sur **F12**.

Cherche ce message :
```
[Leaderboard] WebSocket connecté à Streamer.bot
```

S'il est absent ou si tu vois une erreur :
1. Vérifie que Streamer.bot est lancé
2. Active le serveur WebSocket : `Servers/Clients → WebSocket Server → Enabled`
3. Clique **Refresh cache** sur la Browser Source OBS

### Cause 2 — Aucun profil viewer existant

Le leaderboard n'affiche que les données existantes. Si `data/users/` est vide, il n'y a rien à afficher. Déclenche d'abord `XP_Add` via un message de test pour créer des profils (voir [INSTALLATION.md](INSTALLATION.md#6-tester-le-système)).

### Cause 3 — Mauvaise URL dans OBS

Vérifie que la Browser Source pointe vers le bon fichier HTML. Le chemin doit ressembler à :
```
file:///C:/StreamerTools/streamerbot-xp-system/overlays/leaderboard/leaderboard.html
```

### Cause 4 — Cache OBS non actualisé

Après toute modification des fichiers JS ou CSS, clique **Refresh cache** sur la Browser Source OBS. Sans ça, l'ancien cache est utilisé.

---

## La carte de profil ne s'affiche pas

**Symptômes :** le Channel Point est racheté, Streamer.bot traite la rédemption, mais rien n'apparaît dans OBS.

### Cause 1 — Viewer sans profil

Si le viewer n'a jamais chatté (ou chatté avant l'installation du système), il n'a pas de fichier dans `data/users/`. L'action log indique :
```
[CARD_ShowProfile] Profil introuvable pour 'nomviewer'
```
**Solution :** le viewer doit envoyer au moins un message valide en chat pour que son profil soit créé.

### Cause 2 — WebSocket non connecté

Même vérification que pour le leaderboard. Ouvre la console OBS de la source card (clic droit → Interact → F12) et cherche le message de connexion.

### Cause 3 — Nom de source incorrect

La valeur `obsCardSource` dans `config.json` doit correspondre **exactement** au nom de la Browser Source dans OBS. Vérifie la casse et les espaces.

```json
"obsCardSource": "ProfileCard"
```
Dans OBS, la source doit s'appeler exactement `ProfileCard`.

---

## L'XP ne s'incrémente pas

**Symptômes :** les fichiers JSON existent dans `data/users/` mais le champ `Xp` reste à 0, ou les fichiers ne sont pas créés du tout.

### Cause 1 — Variable globale manquante ou non persistante

Dans Streamer.bot : `Settings → Global Variables`. La variable `xp_configPath` doit exister, être de type **String**, et avoir **Persisted** coché.

### Cause 2 — dataPath incorrect ou dossier inexistant

Le dossier `data/users/` doit exister physiquement. Crée-le manuellement dans l'explorateur Windows si besoin.

### Cause 3 — Message normalement filtré

Ces messages sont ignorés intentionnellement (aucun log, comportement voulu) :
- Commandes commençant par `!`, `/` ou `.`
- Messages plus courts que `minMessageLength` caractères
- Messages envoyés dans la fenêtre de cooldown du viewer

Consulte les logs Streamer.bot (`View → Log`) et filtre par `[XP_Add]` pour identifier la raison.

### Cause 4 — USER_GetOrCreate absent ou mal positionné

`USER_GetOrCreate` doit être la **première sous-action** de `XP_Add`. Si elle est absente ou placée après le script C#, le profil n'est pas chargé et l'XP ne peut pas être sauvegardé.

Structure correcte :
```
Action : XP_Add
  [1] Run Action > USER_GetOrCreate   ← doit être en premier
  [2] Execute C# Code > XP_Add.cs
```

---

## Erreur de compilation C# dans Streamer.bot

**Symptômes :** après copier-coller du code, le bouton Compile retourne des erreurs rouges.

### Cause 1 — Copie incomplète du fichier

Le fichier `.cs` contient des classes de service au-dessus de la classe principale. Tu dois copier **l'intégralité** du fichier, pas seulement la méthode `Execute()`.

Sélectionne tout avec **Ctrl+A** dans l'éditeur texte avant de copier.

### Cause 2 — Caractères corrompus

Si le code a transité par un éditeur riche (Word, client mail), des guillemets typographiques ou des caractères invisibles ont pu remplacer les guillemets droits et apostrophes du code.

**Solution :** copie directement depuis l'explorateur Windows → clic droit sur le fichier `.cs` → ouvrir avec **Notepad**.

### Cause 3 — Version .NET incompatible

Streamer.bot 0.2.0+ utilise .NET 6. Vérifie ta version de Streamer.bot dans `Help → About`.

---

## L'action ne se déclenche pas

**Symptômes :** XP_Add ne réagit pas aux messages, LEADERBOARD_Update ne se met pas à jour automatiquement.

**Vérifications :**

1. L'action est-elle activée ? Le bouton **Enable** doit être actif dans Streamer.bot.
2. Le bon Trigger est-il attaché ? Vérifie dans l'onglet **Triggers** de l'action.

| Action | Trigger attendu |
|---|---|
| `XP_Add` | Twitch → Chat Message |
| `LEADERBOARD_Update` | Timer (intervalle configuré) |
| `CARD_ShowProfile` | Twitch → Channel Point Redemption → récompense sélectionnée |

3. Streamer.bot est-il connecté à Twitch ? Vérifie dans `Platforms → Twitch`.

---

## Problème de chemin dataPath

**Symptômes :** le log affiche `dataPath non configuré` ou `dossier introuvable`.

**Checklist :**

1. Syntaxe JSON correcte — utilise `\\` ou `/`, jamais `\` seul :
```json
"dataPath": "C:\\StreamerTools\\streamerbot-xp-system\\data\\users"
```

2. Le chemin pointe vers `data\users\`, pas vers `data\` :
```json
"dataPath": "C:\\...\\data\\users"   ← correct
"dataPath": "C:\\...\\data"          ← trop haut d'un niveau
```

3. La variable globale `xp_configPath` pointe vers `configs\config.json` et non vers le dossier `configs\` :
```
xp_configPath = C:\StreamerTools\streamerbot-xp-system\configs\config.json
```

4. Le dossier `data\users\` existe physiquement sur le disque.

---

## Animations saccadées ou overlay lent

**Solutions :**

1. Active l'accélération matérielle dans OBS : `Settings → Advanced → Enable browser source hardware acceleration`
2. Réduis la fréquence de mise à jour : augmente `leaderboardIntervalMinutes` dans `config.json`
3. Si plusieurs Browser Sources sont actives en même temps, vérifie que tu n'as pas activé **Shutdown source when not visible** par erreur sur une source qui doit rester active
