# Sons du thème myastya-samourai — Check-in

Par défaut, **aucun fichier n'est nécessaire** : tous les effets sonores sont
**synthétisés** en Web Audio (le thème marche tout de suite, son **activé**).

## Ajouter tes propres sons (override)

1. Dépose tes fichiers audio ici (`.mp3` ou `.ogg`), p. ex. `complete.mp3`.
2. Déclare-les dans `../sounds.json` :

```json
{
  "complete": { "src": "sounds/complete.mp3", "volume": 0.9 },
  "petal":    { "src": "sounds/petal.mp3",    "volume": 0.7 }
}
```

- `src` est **relatif au dossier du thème** (donc commence par `sounds/`).
- `volume` va de `0` à `1` (défaut `1`).
- Toute clé absente du JSON reste **synthétisée**.
- Si un fichier est illisible/introuvable, le thème **retombe sur la synthèse**.

## Événements → sons (Check-in)

| Clé        | Déclencheur                          |
|------------|--------------------------------------|
| `appear`   | Apparition de la fenêtre             |
| `petal`    | Remplissage de chaque case           |
| `complete` | Carte complète                       |

Autres clés disponibles (partagées entre overlays) : `spark`, `fill`, `tick`,
`levelup`, `milestone`, `reveal`.

## Couper / régler le son

- URL : `checkin.html?theme=myastya-samourai&mute`
- Console OBS (Interact / F12) : `window.sfx.mute()`, `window.sfx.unmute()`,
  `window.sfx.setVolume(0.4)`.
