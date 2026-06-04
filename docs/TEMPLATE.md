# Réutiliser le hub comme template (autre streamer)

Le hub est un **template mono-locataire** : tu déploies **ton** site + **ton** backend.
Tout se configure par fichiers — aucun code à toucher. Ce guide vise un·e streamer qui
clone le dépôt pour **son propre** hub.

> Installation complète du bot (Streamer.bot + OBS + export) :
> [INSTALLATION.md](INSTALLATION.md). Leaderboard intégrable : [EMBED.md](EMBED.md).

## 1. Cloner et installer
```bash
git clone <ce-dépôt> mon-hub
cd mon-hub/website
npm install
```

## 2. Configurer ton identité — `website/content/site.json`
```json
{
  "theme": "shibuya",
  "twitchChannel": "ton_pseudo_twitch",
  "siteName": "TonNom",
  "tagline": "Streameur·euse Twitch",
  "description": "Phrase de présentation (SEO / partages).",
  "keywords": ["TonNom", "ta thématique"],
  "alternateNames": [],
  "ogImage": "/og.jpg",
  "features": { "planning": true, "ressources": true, "friends": true, "socials": true }
}
```
- `siteName` / `tagline` / `description` / `keywords` pilotent titres, SEO, footer, manifest.
- Les URLs (canonical, sitemap, OpenGraph) viennent de `NEXT_PUBLIC_SITE_URL`
  (cf. déploiement) — **pas** de `site.json`.

## 3. Choisir tes sections — `features`
Le **cœur** est toujours présent : **Classement** (`/leaderboard`) et **Profils viewer**
(`/viewer/*`). Le reste est optionnel :

| Flag | Effet si `false` |
|---|---|
| `planning` | Retire le lien « Planning » de la nav ; `/planning` renvoie 404. |
| `ressources` | Retire le lien « Ressources » de la nav ; `/ressources` renvoie 404. |
| `friends` | Masque la section « Copains en live » de l'accueil. |
| `socials` | Masque la section « Rejoins-moi » de l'accueil. (Les liens du footer et le bouton Twitch restent.) |

> **Défaut** : un flag absent vaut `true`. Un `site.json` sans bloc `features` =
> toutes les sections activées.

Exemple **minimal** (juste le système XP, sans contenu éditorial) :
```json
"features": { "planning": false, "ressources": false, "friends": false, "socials": false }
```

## 4. Autres contenus (si sections activées)
- `content/schedule.json` — planning des lives.
- `content/downloads.json` — ressources à télécharger.
- `content/friends.json` — copains en live.
- `content/socials.json` — tes réseaux (footer + section accueil).

## 5. Garder tes valeurs hors du dépôt — override `.local` (optionnel)

Le dépôt committe des `content/*.json` **neutres**. Si tu maintiens un fork public (ou
veux éviter de committer ton contenu), crée un **`X.local.json`** à côté : il est
**gitignoré** et **remplace complètement** le fichier committé au build.

```
content/site.json         ← neutre, committé (template)
content/site.local.json   ← TES valeurs, gitignoré (override complet)
```
Vaut pour `site`, `socials`, `friends`, `schedule`, `downloads`. Un exemple est fourni :
`content/site.local.json.example`. (Si tu n'as pas besoin de cette séparation, édite
simplement les `content/*.json` directement.)

**Image OpenGraph** : `public/og.jpg` est **gitignoré** — dépose **ta** propre image
1200×630 à cet emplacement (référencée par `site.json` → `ogImage`).

## 6. ⚠️ Domaine backend dans la CSP

`website/public/.htaccess` et `website/public/_headers` contiennent une
`Content-Security-Policy` avec un `connect-src` pointant un domaine backend. **Remplace
ce domaine par le tien** (`https://api.ton-domaine`) — sinon le navigateur bloquera les
appels à ton API (`/api/leaderboard`, profils, live). Ces fichiers sont déployés tels quels.

## 7. Build & déploiement
```bash
cd website
npx tsc --noEmit && npm run build   # génère out/ (export statique)
```
Voir les scripts `tools/` pour le déploiement (FTP) et le backend live
(`NEXT_PUBLIC_API_URL`). Le **leaderboard intégrable** est documenté dans
[EMBED.md](EMBED.md).
