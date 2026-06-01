# Sons du thème myastya-samourai — Leaderboard

Par défaut, **aucun fichier n'est nécessaire** : tous les effets sonores sont
**synthétisés** en Web Audio (le thème marche tout de suite, son **activé**).

## Ajouter tes propres sons (override)

1. Dépose tes fichiers audio ici (`.mp3` ou `.ogg`), p. ex. `milestone.mp3`.
2. Déclare-les dans `../sounds.json` :

```json
{
  "milestone": { "src": "sounds/milestone.mp3", "volume": 0.9 },
  "reveal":    { "src": "sounds/reveal.mp3",    "volume": 0.8 }
}
```

- `src` est **relatif au dossier du thème** (donc commence par `sounds/`).
- `volume` va de `0` à `1` (défaut `1`).
- Toute clé absente du JSON reste **synthétisée**.
- Si un fichier est illisible/introuvable, le thème **retombe sur la synthèse**.

## Événements → sons (Leaderboard)

| Clé         | Déclencheur                             |
|-------------|-----------------------------------------|
| `reveal`    | Apparition du panneau                   |
| `milestone` | Révélation du n°1 (podium)              |
| `tick`      | Révélation des rangs 2-3 et lignes 4-10 |

Autres clés disponibles (partagées entre overlays) : `appear`, `petal`,
`spark`, `fill`, `levelup`, `complete`.

## Couper / régler le son

- URL : `leaderboard.html?theme=myastya-samourai&mute`
- Console OBS (Interact / F12) : `window.sfx.mute()`, `window.sfx.unmute()`,
  `window.sfx.setVolume(0.4)`.
