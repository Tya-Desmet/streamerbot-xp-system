# Leaderboard intégrable (iframe)

Le hub expose une route **`/embed/leaderboard`** : un classement autonome, sans
navigation ni pied de page, à poser sur **n'importe quel site externe** (Wix,
WordPress, site perso, overlay…) via une simple `<iframe>`.

> **Mono-locataire** : un backend = un streamer. L'iframe pointe vers **ton** domaine
> (celui où le hub est déployé). Il n'y a pas de service partagé multi-streamers.

## Snippet à coller

```html
<iframe src="https://<ton-domaine>/embed/leaderboard?theme=hara&limit=10"
        style="width:100%;max-width:480px;height:600px;border:0"
        loading="lazy" title="Classement"></iframe>
```

Remplace `<ton-domaine>` par l'URL de ton hub (la même que `NEXT_PUBLIC_SITE_URL`).

## Options (query string)

| Paramètre | Valeurs | Défaut | Effet |
|---|---|---|---|
| `theme` | `shibuya`, `akiba`, `hara` | `shibuya` | Palette de couleurs (design system du hub). |
| `limit` | `1`–`50` | `10` | Nombre de joueurs affichés (top N). |
| `bg` | `transparent` ou rien | (fond plein) | `transparent` = fond transparent (idéal en overlay OBS / par-dessus une page). |
| `accent` | couleur CSS | (couleur du thème) | Surcharge la couleur d'accent. **Encode le `#`** en `%23` (ex. `accent=%23ff66cc`). |
| `title` | `off` ou rien | (titre affiché) | `title=off` masque le titre « Classement · <nom> ». |

Exemple complet (overlay transparent, 5 joueurs, accent rose, sans titre) :

```
/embed/leaderboard?theme=hara&limit=5&bg=transparent&accent=%23ff66cc&title=off
```

## Données & fraîcheur

- L'embed lit **`/api/leaderboard`** (backend live) si `NEXT_PUBLIC_API_URL` est
  configuré au build, avec rafraîchissement toutes les **45 s** (garde `generatedAt` :
  on n'affiche jamais un snapshot plus ancien). Sinon il sert le classement figé du
  dernier build (`/data/leaderboard.json`).
- **Affichage uniquement** : aucun calcul XP/niveau/rang côté client. Le classement
  seul (rang, pseudo, niveau, titre, XP) — **pas de lien vers les profils viewer**.

## CORS

- Si l'iframe est servie depuis **le même domaine** que le backend (cas standard,
  `NEXT_PUBLIC_API_URL` sur ton domaine), aucune requête cross-origin : rien à faire.
- Si le backend est sur un **autre domaine**, le `fetch` vers `/api/leaderboard` est
  cross-origin : le backend renvoie déjà `Access-Control-Allow-Origin`, donc l'appel
  passe. (L'iframe elle-même charge la page depuis ton domaine, ce n'est pas du CORS.)

## Notes

- La route est en `noindex` (elle vit en iframe, pas une page à référencer seule).
- Branding neutre : le nom affiché vient de `meta.streamer.name`
  (`config.export.streamerName`), jamais codé en dur.
- CSS isolé : le thème s'applique sur un conteneur interne (variables CSS héritées),
  sans polluer la page hôte.
