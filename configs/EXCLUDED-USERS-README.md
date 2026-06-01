# excluded-users.json — Guide

## COMPORTEMENT IMPORTANT

Ce fichier **REMPLACE INTÉGRALEMENT** la liste des bots intégrée au système.
Il ne l'étend pas.

Si vous ajoutez votre propre bot, vous **DEVEZ** aussi inclure les bots standard.

## Liste minimum recommandée

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
  "votre-bot-ici"
]
```

## Règles

- Noms en minuscules
- Insensible à la casse : "NightBot" et "nightbot" sont identiques
- Un nom par ligne (format tableau JSON)
- Sauvegarder et redémarrer l'action SB pour que le changement prenne effet

## Bots exclus si le fichier est ABSENT ou VIDE

Si `excluded-users.json` est absent ou vide (`[]`), le système utilise
automatiquement cette liste intégrée :

- nightbot
- streamelements
- streamlabs
- moobot
- fossabot
- wizebot
- mixitupbot
- streamerbot

## Comment exclure le broadcaster

Dans `configs/config.json` :

```json
"bots": {
  "excludeBroadcaster": true,
  "broadcasterName": "votre_pseudo_twitch"
}
```

Cette option fonctionne indépendamment de `excluded-users.json`.
