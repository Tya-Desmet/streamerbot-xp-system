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

<!-- TODO: Ajouter screenshots de chaque thème sur leaderboard et card -->

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
│       └── sakura/theme.css
└── card/
    └── themes/
        ├── default/theme.css
        ├── rpg/theme.css
        ├── cyber/theme.css
        ├── minimal/theme.css
        ├── tokyo/theme.css
        └── sakura/theme.css
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

Valeurs acceptées : `default`, `rpg`, `cyber`, `minimal`, `tokyo`, `sakura`

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
