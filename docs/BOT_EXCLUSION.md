# Exclusion des bots — Guide utilisateur

Le système d'exclusion empêche les bots et comptes techniques de polluer le classement XP. Un compte exclu est complètement ignoré : il ne gagne aucun XP, n'apparaît jamais dans le leaderboard, ne peut pas afficher de profile card et ne répond pas à `!rank`.

---

## ⚠ COMPORTEMENT IMPORTANT — Liste de remplacement

Le fichier `configs/excluded-users.json` **REMPLACE** la liste par défaut. Il ne l'étend pas.

Si votre fichier contient `["monbot"]` uniquement,
les bots standard (nightbot, streamelements...) **NE SONT PLUS exclus**.

→ Toujours inclure les bots standard dans votre liste personnalisée.
→ Voir `configs/EXCLUDED-USERS-README.md` pour la liste complète.

---

## Fichier de configuration

**Chemin :** `configs/excluded-users.json`

Ce fichier contient la liste des noms d'utilisateur Twitch à exclure. Un nom par ligne, en minuscules, entre guillemets.

```json
[
  "nightbot",
  "streamelements",
  "streamlabs",
  "moobot",
  "fossabot",
  "wizebot",
  "mixitupbot",
  "streamerbot",
  "mystyabot"
]
```

> La comparaison est **insensible à la casse** : `NightBot`, `nightbot` et `NIGHTBOT` sont identiques.

---

## Exclure un bot

### Exclure un bot courant

Ouvrir `configs/excluded-users.json` et ajouter le nom du bot :

```json
[
  "nightbot",
  "streamelements",
  "monbot"
]
```

### Exclure un bot personnalisé

Même procédure — le nom Twitch (login, pas le display name) en minuscules :

```json
[
  "nightbot",
  "mystyabot",
  "monpersonalbot"
]
```

### Vérifier le nom exact du bot

Dans Twitch Studio ou sur twitch.tv, le login (nom de connexion) est affiché en minuscules dans l'URL du profil : `twitch.tv/nomdubot`.

---

## Exclure plusieurs bots

Ajouter autant d'entrées que nécessaire :

```json
[
  "nightbot",
  "streamelements",
  "streamlabs",
  "moobot",
  "fossabot",
  "wizebot",
  "mixitupbot",
  "streamerbot",
  "monbot1",
  "monbot2",
  "testaccount"
]
```

---

## Exclure le streamer lui-même

Par défaut, le streamer apparaît dans le classement s'il chatte sur son propre stream.

Pour l'exclure, modifier `configs/config.json` :

```json
{
  "excludeBroadcaster": true,
  "broadcasterName":    "monpseudotwitch"
}
```

- `excludeBroadcaster` : `true` pour activer, `false` pour désactiver (défaut)
- `broadcasterName` : ton login Twitch en minuscules (ex: `"mystyaplays"`)

Résultat : le compte du streamer est traité comme un bot — aucun XP, absent du leaderboard.

---

## Comportement si le fichier est absent

Si `configs/excluded-users.json` est supprimé ou absent, le système utilise automatiquement une liste intégrée contenant les bots les plus courants :

| Bot | Description |
|---|---|
| nightbot | Bot de modération populaire |
| streamelements | Bot alerts/overlay |
| streamlabs | Bot alerts/overlay |
| moobot | Bot de modération |
| fossabot | Bot de commandes |
| wizebot | Bot de modération |
| mixitupbot | Bot d'interaction |
| streamerbot | Bot Streamer.bot lui-même |

---

## Comportement si un compte est exclu

| Action | Résultat |
|---|---|
| Message chat | Ignoré silencieusement, aucun log visible |
| Watchtime | Non comptabilisé |
| Leaderboard | N'apparaît pas dans le Top 10 |
| `!rank` | Commande ignorée silencieusement |
| Profile card | Ne s'affiche pas |
| Fichier JSON | Aucun fichier créé dans `data/users/` |

---

## Erreurs courantes

**Le bot continue d'apparaître dans le leaderboard**

Le fichier `data/users/{nomdubot}.json` a probablement déjà été créé avant l'exclusion. Supprimer manuellement le fichier correspondant dans `data/users/`.

**Le JSON est invalide**

Vérifier la syntaxe : chaque nom entre guillemets, séparés par des virgules, sans virgule après le dernier élément. Utiliser [jsonlint.com](https://jsonlint.com) pour valider.

**Le streamer apparaît toujours dans le classement**

Vérifier que `broadcasterName` correspond exactement au login Twitch (pas le display name). Le login est visible dans l'URL de ton profil Twitch.
