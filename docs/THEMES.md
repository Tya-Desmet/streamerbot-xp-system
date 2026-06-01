# Thèmes — Personnalisation visuelle

## Vue d'ensemble

Les deux overlays (leaderboard et carte de profil) supportent un système de thèmes basé sur des variables CSS. Chaque thème est un fichier `theme.css` indépendant qui redéfinit les couleurs, polices et effets visuels.

---

## Thèmes disponibles

| Thème | Style | Polices |
|---|---|---|
| `default` | Violet Twitch, moderne | Orbitron + Rajdhani |
| `rpg` | Or et parchemin, médiéval | Cinzel + IM Fell English |
| `cyber` | Cyan néon, futuriste | Share Tech Mono |
| `minimal` | Blanc épuré, sobre | Inter |
| `tokyo` | Rose et bleu nuit, japonisant | Noto Sans JP |
| `sakura` | Rose pastel, délicat | Noto Sans JP |
| `myastya-samourai` | Nuit pourpre + magenta cerisier, **pétales/étincelles animés + son** | Shippori Mincho + Noto Sans JP |

<!-- TODO: Ajouter screenshots de chaque thème sur leaderboard et card -->

> **Note** — `myastya-samourai` va au-delà d'un simple `theme.css` : il embarque
> aussi un module d'effets (`effects.js`) qui ajoute un canvas de pétales de
> cerisier, des étincelles synchronisées aux animations, une fleur décorative et
> des effets sonores. Voir la section [Thème myastya-samourai](#thème-myastya-samourai--effets--son).

---

## Structure des fichiers de thèmes

Les thèmes sont séparés par overlay :

```
overlays/
├── leaderboard/
│   └── themes/
│       ├── default/theme.css
│       ├── rpg/theme.css
│       ├── cyber/theme.css
│       ├── minimal/theme.css
│       ├── tokyo/theme.css
│       ├── sakura/theme.css
│       └── myastya-samourai/        ← thème "augmenté" (canvas + son)
│           ├── theme.css
│           ├── effects.js
│           ├── sounds.json
│           └── sounds/
├── card/
│   └── themes/                      ← mêmes thèmes (dont myastya-samourai/…)
└── checkin/
    └── themes/                      ← mêmes thèmes (dont myastya-samourai/…)
```

---

## Changer de thème

### Méthode 1 — Modification directe du HTML (recommandée)

Ouvre le fichier HTML de l'overlay et modifie la balise `<link>` du thème :

**overlays/leaderboard/leaderboard.html**
```html
<!-- Remplacer "default" par le nom du thème voulu -->
<link id="theme-css" rel="stylesheet" href="themes/default/theme.css">
```

**overlays/card/card.html**
```html
<link id="theme-css" rel="stylesheet" href="themes/default/theme.css">
```

Après modification, clique **Refresh cache** sur chaque Browser Source OBS concernée.

---

### Méthode 2 — Changement à chaud via JavaScript

Depuis la console OBS (clic droit sur la source → **Interact** → F12) :

```javascript
window.setTheme("rpg");
```

Valeurs acceptées : `default`, `rpg`, `cyber`, `minimal`, `tokyo`, `sakura`, `myastya-samourai`

---

## Thème myastya-samourai — effets & son

Thème « nuit pourpre & cerisier » inspiré d'un widget de cagnotte : fond indigo
profond, dégradé magenta cerisier, **pétales de cerisier** qui tombent en continu,
**étincelles** déclenchées par les animations, une **fleur de cerisier** décorative
et des **effets sonores**. Disponible sur les trois overlays (card, leaderboard, check-in).

### Activer

```
?theme=myastya-samourai          ← paramètre d'URL de la Browser Source
window.setTheme('myastya-samourai')  ← à chaud (console OBS Interact / F12)
```

Contrairement aux autres thèmes, `setTheme` charge **deux** ressources :
`themes/myastya-samourai/theme.css` (couleurs) **et**
`themes/myastya-samourai/effects.js` (canvas + son). Le tout est nettoyé
automatiquement quand on repasse à un autre thème.

### Architecture du thème

```
overlays/<overlay>/themes/myastya-samourai/
├── theme.css        ← couleurs, polices, fleur (classes .mys-*) — comme un thème classique
├── effects.js       ← canvas pétales + étincelles + fleur + moteur audio
├── sounds.json      ← mapping optionnel évènement → fichier audio
└── sounds/          ← (optionnel) tes propres .mp3 / .ogg
```

Le chargement du module d'effets est géré par un mécanisme générique des overlays
(`EFFECTS_THEMES` + `loadThemeEffects` dans le JS de chaque overlay) : un thème
n'a besoin d'un `effects.js` que s'il veut du canvas/son ; sinon un simple
`theme.css` suffit (rétro-compatible avec tous les thèmes existants).

### Son

- **Activé par défaut.** Couper avec `?mute`, ou `window.sfx.mute()` /
  `window.sfx.unmute()` / `window.sfx.setVolume(0..1)` dans la console.
- **Synthétisé** en Web Audio par défaut : **aucun fichier requis**, ça marche
  immédiatement et hors-ligne.
- **Remplaçable par tes fichiers** : dépose un `.mp3`/`.ogg` dans
  `themes/myastya-samourai/sounds/` puis mappe-le dans `sounds.json`, ex. :

  ```json
  { "complete": { "src": "sounds/complete.mp3", "volume": 0.9 } }
  ```

  Toute clé non mappée reste synthétisée ; un fichier manquant retombe sur la
  synthèse. Voir le `README.md` dans chaque dossier `sounds/`.

Évènements sonores par overlay :

| Overlay      | Évènements → clés                                                  |
|--------------|--------------------------------------------------------------------|
| Card         | entrée → `appear`, barre XP → `fill`, re-affichage → `tick`         |
| Leaderboard  | panneau → `reveal`, n°1 → `milestone`, rangs 2-10 → `tick`          |
| Check-in     | apparition → `appear`, chaque case → `petal`, carte complète → `complete` |

> ⚠️ **Audio en OBS** — la source navigateur OBS doit être audible (icône
> haut-parleur non coupée, monitoring/mixage activé selon ton setup). En
> prévisualisation dans un navigateur classique, l'audio démarre au premier
> clic/touche (politique d'autoplay).

### Performance

Les pétales et étincelles sont dessinés sur un seul `<canvas>` plein cadre
(`requestAnimationFrame`, plafonné à ~46 pétales). Le canvas et le contexte audio
sont détruits dès qu'on change de thème, donc aucun coût quand le thème n'est pas actif.

---

## Créer un thème personnalisé

### Étape 1 — Créer les dossiers

```
overlays/leaderboard/themes/montheme/theme.css
overlays/card/themes/montheme/theme.css
```

### Étape 2 — Copier un thème existant comme base

Copie le fichier `default/theme.css` dans ton nouveau dossier pour partir d'une base fonctionnelle.

### Étape 3 — Modifier les variables CSS

**Variables principales du leaderboard :**

```css
:root {
  /* Arrière-plan */
  --color-bg:           #0e0e1a;

  /* Textes */
  --color-text:         #ffffff;
  --color-accent:       #9147ff;

  /* Podium — rangs 1, 2, 3 */
  --color-gold:         #FFD700;
  --color-silver:       #C0C0C0;
  --color-bronze:       #CD7F32;

  /* Lignes rangs 4-10 */
  --color-row-bg:       rgba(255,255,255,0.05);
  --color-row-border:   rgba(255,255,255,0.1);

  /* Typographie */
  --font-primary:       'Orbitron', sans-serif;
  --font-secondary:     'Rajdhani', sans-serif;
}
```

**Variables principales de la carte de profil :**

```css
:root {
  /* Arrière-plan */
  --color-bg:           #0e0e1a;

  /* Textes */
  --color-text:         #ffffff;
  --color-accent:       #9147ff;

  /* Barre XP */
  --color-xp-bar:       #9147ff;
  --color-xp-bar-bg:    rgba(255,255,255,0.1);

  /* Typographie */
  --font-primary:       'Orbitron', sans-serif;
  --font-secondary:     'Rajdhani', sans-serif;
}
```

### Étape 4 — Charger les polices personnalisées

Si ton thème utilise une police Google Fonts, ajoute la balise `<link>` dans le fichier HTML de l'overlay (avant le chargement du thème CSS) :

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link href="https://fonts.googleapis.com/css2?family=MaPolice:wght@400;700&display=swap" rel="stylesheet">
```

### Étape 5 — Appliquer le thème

Mets à jour la balise `<link id="theme-css">` dans les fichiers HTML pour pointer vers ton thème, puis clique **Refresh cache** sur la Browser Source OBS.

---

## Conseils de design

**Teste les deux overlays.** Le leaderboard et la carte de profil ont des mises en page différentes — un thème adapté à l'un peut mal rendre sur l'autre.

**Contraste.** Les overlays s'affichent par-dessus le stream. Assure-toi que le texte reste lisible même avec un fond semi-transparent.

**N'anime pas dans theme.css.** Les animations de révélation et de transition sont gérées par les fichiers JS de chaque overlay. Ajouter des animations CSS dans `theme.css` peut créer des conflits.

**Polices hors ligne.** Si ton internet coupe pendant le stream, les polices Google Fonts ne chargeront pas. Pour un stream professionnel, envisage de télécharger les fichiers de police et de les référencer localement.
