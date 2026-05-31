# Système de Titres — Guide Utilisateur

Les titres sont affichés automatiquement selon le niveau du viewer.

Exemples d'affichage dans le chat :

```
@Mystya · Compagnon du Stream | Rang #5/127 | Niv.24 | ...
@ZephyrTV · Vétéran | Rang #2/127 | Niv.34 | ...
@TyaPlays · Nouveau venu | Rang #98/127 | Niv.3 | ...
```

---

## Titres inclus par défaut

| Niveau minimum | Titre                    |
|:--------------:|--------------------------|
| 1              | Nouveau venu             |
| 5              | Régulier                 |
| 10             | Adepte                   |
| 20             | Compagnon du Stream      |
| 30             | Vétéran                  |
| 50             | Pilier de la communauté  |
| 75             | Légende                  |
| 100            | Icône                    |

Un viewer de niveau 23 reçoit le titre **Adepte** (palier 10, le plus élevé dont il satisfait la condition).

---

## Comment personnaliser les titres

### Étape 1 — Ouvrir le fichier de surcharge

Ouvrir le fichier :

```
configs/titles.json
```

Il contient par défaut un tableau vide `[]`.
Tant qu'il est vide, le système utilise les titres ci-dessus.

---

### Étape 2 — Écrire ses titres personnalisés

Remplacer le contenu par ceci (adapter les valeurs) :

```json
[
  { "minLevel": 1,  "title": "Petit nouveau" },
  { "minLevel": 5,  "title": "Régulier de Tya" },
  { "minLevel": 15, "title": "Fan de Tya" },
  { "minLevel": 30, "title": "Super Fan" },
  { "minLevel": 50, "title": "Légende du Stream" }
]
```

**Règles :**

- `minLevel` doit être un nombre entier ≥ 1
- `title` doit être une chaîne de texte non vide
- Les paliers n'ont pas besoin d'être dans l'ordre
- Autant de paliers que voulu (2 minimum recommandé)

---

### Étape 3 — Sauvegarder et tester

Sauvegarder le fichier. Aucun redémarrage de Streamer.bot nécessaire.
Le titre se met à jour au prochain `!rank` dans le chat.

---

## Règle de résolution du titre

Le titre affiché est celui dont le `minLevel` est le plus élevé parmi tous ceux inférieurs ou égaux au niveau du viewer.

**Exemple avec les paliers 1 / 10 / 20 / 30 :**

| Niveau viewer | Titre affiché        |
|:-------------:|----------------------|
| 1             | palier 1             |
| 9             | palier 1             |
| 10            | palier 10            |
| 19            | palier 10            |
| 20            | palier 20            |
| 99            | palier 30 (le plus élevé disponible) |

---

## Créer des titres pour un thème

Si le projet utilise plusieurs thèmes, chaque thème peut avoir ses propres titres.

Créer le fichier :

```
themes/{nom-du-theme}/titles.json
```

Exemple pour un thème `rpg` :

```
themes/rpg/titles.json
```

```json
[
  { "minLevel": 1,   "title": "Apprenti Aventurier" },
  { "minLevel": 10,  "title": "Guerrier" },
  { "minLevel": 25,  "title": "Chevalier" },
  { "minLevel": 50,  "title": "Paladin" },
  { "minLevel": 75,  "title": "Légendaire" },
  { "minLevel": 100, "title": "Héros Éternel" }
]
```

Activer ce thème dans `configs/config.json` :

```json
"theme": "rpg"
```

---

## Ordre de priorité du chargement

Le système charge les titres dans cet ordre et s'arrête dès qu'une source valide est trouvée :

```
1. configs/titles.json          ← surcharge utilisateur  (priorité absolue)
2. themes/{thème}/titles.json   ← titres du thème actif
3. themes/default/titles.json   ← titres livrés par défaut
4. "Viewer"                     ← fallback interne (jamais affiché en pratique)
```

**En pratique :**
- Pour tout personnaliser → utiliser `configs/titles.json`
- Pour des titres par thème → utiliser `themes/{nom}/titles.json`
- Pour revenir aux défauts → vider `configs/titles.json` (remettre `[]`)

---

## Erreurs courantes

### Le titre ne change pas

Vérifier que `configs/titles.json` est bien formé (JSON valide).
Un fichier malformé est ignoré silencieusement — le système passe au niveau suivant.

Pour vérifier la validité du JSON : copier le contenu sur [jsonlint.com](https://jsonlint.com).

---

### Tous les viewers ont le même titre

Vérifier que les paliers `minLevel` sont bien différents et couvrent l'échelle de niveaux souhaitée.

---

### Le titre n'apparaît pas dans `!rank`

Vérifier que le thème configuré dans `configs/config.json` correspond bien à un dossier existant dans `themes/`. En cas de doute, utiliser `"theme": "default"`.

---

## Résumé rapide

```
Personnalisation rapide   → éditer configs/titles.json
Titres par thème          → créer themes/{nom}/titles.json
Revenir aux défauts       → remettre [] dans configs/titles.json
Changer de thème          → modifier "theme" dans configs/config.json
```
