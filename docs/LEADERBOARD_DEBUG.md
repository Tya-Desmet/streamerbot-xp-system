# LEADERBOARD — GUIDE DE DIAGNOSTIC

> Le leaderboard ne s'affiche plus ? Suivre ce guide étape par étape.
> Chaque étape a une vérification claire : OK ou KO.

---

## ÉTAPE 1 — Vérifier que Streamer.bot tourne

**Action :** Ouvrir Streamer.bot, vérifier qu'il est bien connecté à Twitch.

**Vérification :**
- L'icône SB dans la barre des tâches est présente
- SB affiche "Connected" dans le coin inférieur gauche

**Si KO :** Lancer Streamer.bot et reconnecter le compte Twitch.

---

## ÉTAPE 2 — Vérifier le serveur WebSocket SB

**Action :** Dans Streamer.bot → Settings → WebSocket Server

**Vérification :**
- WebSocket Server est activé (toggle ON)
- Port affiché = **8080** (ou noter le port réel)
- "Auto Start" est coché

**Si le port n'est pas 8080 :**
Modifier `overlays/leaderboard/js/socket.js` ligne 13 :
```javascript
var WS_URL = 'ws://127.0.0.1:VOTRE_PORT/';
```

**Si KO :** Activer le WebSocket Server, redémarrer SB.

---

## ÉTAPE 3 — Vérifier que l'action LEADERBOARD_Update existe dans SB

**Action :** Dans SB → Actions → chercher "LEADERBOARD_Update"

**Vérification :**
- L'action existe
- Elle a un trigger Timer configuré (5 minutes)
- La sub-action "Execute C# Code" est présente

**Si l'action n'existe pas :**
→ La créer depuis `actions/LEADERBOARD_Update.cs` (voir INSTALLATION.md)

**Si le timer n'est pas configuré :**
→ Ajouter le trigger : Actions → LEADERBOARD_Update → Add Trigger → Core → Timer → 5 minutes

---

## ÉTAPE 4 — Vérifier la GlobalVar xp_configPath

**Action :** Dans SB → Settings → Global Variables → chercher "xp_configPath"

**Vérification :**
- La variable existe
- Sa valeur pointe vers un fichier `config.json` qui existe réellement sur le disque
- Exemple valide : `C:\Stream\streamerbot-xp-system\configs\config.json`

**Test rapide :** Dans l'Explorateur Windows, coller ce chemin et vérifier que le fichier s'ouvre.

**Si KO :** Créer la GlobalVar avec le bon chemin. Persistent = OUI.

---

## ÉTAPE 5 — Vérifier config.json

**Action :** Ouvrir `configs/config.json`

**Vérification :**
```json
{
  "dataPath": "C:\\CHEMIN\\VERS\\data\\users",
  ...
}
```
- `dataPath` pointe vers un dossier qui existe
- Le JSON est syntaxiquement valide (pas de virgule manquante, guillemets fermés)

**Tester la validité JSON :** Coller le contenu sur https://jsonlint.com

**Si KO :** Corriger le chemin ou la syntaxe JSON.

---

## ÉTAPE 6 — Vérifier qu'il y a des profils utilisateurs

**Action :** Naviguer vers le dossier `dataPath` configuré

**Vérification :**
- Le dossier existe
- Il contient au moins un fichier `{username}.json`

**Si dossier vide :** Aucun viewer n'a encore chatté. Taper un message dans le chat Twitch et relancer LEADERBOARD_Update manuellement.

**Lancer manuellement :** Dans SB → Actions → LEADERBOARD_Update → clic droit → Test

---

## ÉTAPE 7 — Vérifier la console de la Browser Source OBS

**Action :** Dans OBS → clic droit sur la source Leaderboard → Interact → F12 → onglet Console

**Vérifications attendues au démarrage :**
```
[Leaderboard] ✅ DOM OK
[Leaderboard] ✅ Prêt — connexion WebSocket en cours...
[Leaderboard] ✅ WebSocket connecté
[Leaderboard] 📬 Abonnement General.Custom envoyé
```

**Si "WebSocket connecté" n'apparaît pas :**
→ Le port 8080 est incorrect ou le serveur WS SB n'est pas actif (retour Étape 2)

**Si "DOM OK" n'apparaît pas avec des éléments manquants :**
→ Le HTML est corrompu ou le mauvais fichier est chargé dans OBS

**Quand LEADERBOARD_Update se déclenche, on doit voir :**
```
[Leaderboard] 📩 Message brut : {...}
[Leaderboard] Format A détecté (payload direct)
[Leaderboard] 🎬 Déclenchement leaderboard — X joueurs
[Leaderboard] 🎨 Rendu podium — X entrée(s) : [...]
```

**Si "Message brut" apparaît mais pas "Déclenchement" :**
→ Le format du payload ne correspond à aucun des 4 formats A/B/C/D gérés
→ Copier le contenu de "Message brut" et l'analyser (voir Étape 8)

**Si rien n'apparaît après le test de l'action :**
→ La Browser Source n'est pas connectée au WebSocket ou LEADERBOARD_Update a échoué

---

## ÉTAPE 8 — Analyser le format du payload WebSocket

**Action :** Déclencher LEADERBOARD_Update manuellement (SB → Test action)
Observer dans la console OBS le message brut affiché.

**Format attendu (Format A — direct) :**
```json
{
  "event": "updateLeaderboard",
  "players": [
    { "rank": 1, "username": "Viewer1", "level": 10, "xp": 5000, "avatar": "" }
  ]
}
```

**Si le message n'a pas de champ `event` :**
→ L'action LEADERBOARD_Update.cs dans SB est une version V1 (sans le champ event)
→ Solution : recopier `actions/LEADERBOARD_Update.cs` dans SB et recompiler

**Si le message a un champ `event` mais le nom est différent :**
→ Vérifier que la ligne dans LEADERBOARD_Update.cs est bien :
```csharp
CPH.WebsocketBroadcastJson(JsonConvert.SerializeObject(new {
    @event  = "updateLeaderboard",
    players = payload
}));
```

---

## ÉTAPE 9 — Vérifier que leaderboard.html charge les bons fichiers

**Action :** Ouvrir `overlays/leaderboard/leaderboard.html` dans un éditeur

**Vérification — les 6 scripts chargés doivent être EXACTEMENT :**
```html
<script src="js/animation.js"></script>    ← animation (sans s)
<script src="js/state.js"></script>
<script src="js/podium.js"></script>
<script src="js/top10.js"></script>
<script src="js/leaderboard.js"></script>
<script src="js/socket.js"></script>
```

**ATTENTION aux confusions de noms :**
- `animation.js` (sans 's') = CORRECT — doit être chargé
- `animations.js` (avec 's') = NE PAS CHARGER — fichier mort

**Si `animations.js` est chargé dans le HTML :**
→ Il redéfinit `revealEl()` avec une signature incompatible
→ Retirer `animations.js` du HTML — ne charger que `animation.js`

---

## ÉTAPE 10 — Test complet end-to-end

**Séquence de test :**

1. OBS → ouvrir la source Leaderboard → vérifier "DOM OK" en console
2. SB → Actions → LEADERBOARD_Update → clic droit → Test
3. Observer la console OBS

**Résultat attendu :**
- La console affiche "Déclenchement leaderboard — X joueurs"
- Le leaderboard s'anime et affiche les joueurs

**Si ça fonctionne maintenant :** Super ! Appliquer les corrections des sprints suivants.
**Si ça ne fonctionne toujours pas :** Créer un ticket avec le contenu exact de la console OBS.

---

## CAUSES CONNUES POST-MERGE (v2.0)

Le merge `6a0cc0a` (stratégie "ours") a pu introduire les problèmes suivants :

### Cause A — LEADERBOARD_Update.cs version V1 dans SB
**Symptôme :** Le payload n'a pas de champ `event`
**Solution :** Recopier `actions/LEADERBOARD_Update.cs` dans SB et recompiler

### Cause B — leaderboard.html charge animations.js au lieu de animation.js
**Symptôme :** "DOM OK" apparaît mais animations bloquées
**Solution :** Corriger les balises `<script>` dans leaderboard.html

### Cause C — socket.js version V1 (attend un format de payload différent)
**Symptôme :** "Message brut" affiché mais pas de "Déclenchement"
**Solution :** Recopier `overlays/leaderboard/js/socket.js` depuis le dépôt

### Cause D — xp_configPath non défini ou incorrect
**Symptôme :** LEADERBOARD_Update se termine sans log dans SB
**Solution :** Vérifier et reconfigurer la GlobalVar (Étape 4)

---

## VÉRIFICATION RAPIDE FINALE

Si après toutes ces étapes le leaderboard ne fonctionne toujours pas, fournir :
1. Le contenu exact de la console OBS (F12 → Console)
2. Les logs SB de l'action LEADERBOARD_Update
3. Le contenu du message brut WebSocket si visible
