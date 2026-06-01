# Sons du thème myastya-samourai — Card

Par défaut, **aucun fichier n'est nécessaire** : tous les effets sonores sont
**synthétisés** en Web Audio (le thème marche tout de suite, son **activé**).

## Ajouter tes propres sons (override)

1. Dépose tes fichiers audio ici (`.mp3` ou `.ogg`), p. ex. `fill.mp3`.
2. Déclare-les dans `../sounds.json` :

```json
{
  "fill":   { "src": "sounds/fill.mp3",   "volume": 0.9 },
  "appear": { "src": "sounds/appear.mp3", "volume": 1.0 }
}
```

- `src` est **relatif au dossier du thème** (donc commence par `sounds/`).
- `volume` va de `0` à `1` (défaut `1`).
- Toute clé absente du JSON reste **synthétisée**.
- Si un fichier est illisible/introuvable, le thème **retombe sur la synthèse**.

## Événements → sons (Card)

| Clé      | Déclencheur                          |
|----------|--------------------------------------|
| `appear` | Entrée de la carte                   |
| `fill`   | Remplissage de la barre d'XP         |
| `tick`   | Flash de re-affichage (carte déjà visible) |

Autres clés disponibles (partagées entre overlays) : `petal`, `spark`,
`levelup`, `milestone`, `complete`, `reveal`.

## Couper / régler le son

- URL : `card.html?theme=myastya-samourai&mute`
- Console OBS (Interact / F12) : `window.sfx.mute()`, `window.sfx.unmute()`,
  `window.sfx.setVolume(0.4)`.
