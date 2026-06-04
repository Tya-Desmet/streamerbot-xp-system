# Stream Hub — site web (V3.x)

Hub communautaire **statique** (Next.js 16 + React 19, export statique) au style
**Tokyo Neon / Sakura**. Il affiche le leaderboard, les profils viewers, le planning
et les ressources, et fournit un **leaderboard intégrable** (`/embed/leaderboard`).

> **Lecture seule.** Le bot Streamer.bot reste la seule source de vérité. Le site lit
> les JSON exportés au build, et — si un **backend live** est configuré
> (`NEXT_PUBLIC_API_URL`) — rafraîchit classement et profils via polling (sans rebuild).
> Aucune base de données, aucun compte utilisateur.

> **Template distribuable** : l'identité (nom, SEO, branding) et les sections affichées
> sont pilotées par `content/site.json` — aucun branding en dur. Voir
> [docs/TEMPLATE.md](../docs/TEMPLATE.md).

---

## Pages

| Route | Contenu | Source | Optionnelle |
|---|---|---|---|
| `/` | Accueil : hero, copains, socials, podium **live** | `content/*` + données bot | — (cœur) |
| `/leaderboard` | Classement (podium + liste, recherche, « Trouve-toi »), **live** | données bot | — (cœur) |
| `/viewer/[username]` | Profil viewer | données bot | — (cœur) |
| `/embed/leaderboard` | Leaderboard **intégrable** (iframe), classement seul | données bot | — |
| `/planning` | Programme de la semaine + countdown | `content/schedule.json` | `features.planning` |
| `/ressources` | Catalogue de ressources | `content/downloads.json` | `features.ressources` |
| `/admin` | **Éditeur local (dev uniquement)** | — | — |

Les routes sont organisées en **route groups** : `app/(hub)/` (site complet, avec
NavBar/Footer) et `app/(embed)/` (layout racine minimal, isolé). Une 404 personnalisée
(`app/not-found.tsx`) couvre les URLs inconnues et les pages désactivées.

---

## Thèmes

3 thèmes via les pastilles de la nav (choix persistant) :
- **Shibuya** — sakura jour (défaut)
- **Yozakura** — sakura nuit
- **Akihabara** — e-sport violet/bleu

---

## Données du bot

Le site lit les JSON générés par l'action `EXPORT_Snapshot` (voir
`docs/EXPORT_CONTRACT.md`, schemaVersion 1). Ils sont copiés dans `public/data/`
au build par `scripts/copy-exports.mjs`.

- Source par défaut : `../exports`
- Source personnalisée : variable d'environnement `EXPORT_DIR`

> ⚠️ **Setup à deux dossiers** : le bot tourne depuis `C:\Stream\streamerbot-xp-system\`.
> Builde donc le site en pointant ses exports :
>
> ```powershell
> $env:EXPORT_DIR="C:\Stream\streamerbot-xp-system\exports"; npm run build
> ```

`copy-exports` **vide** `public/data` avant de copier → un profil supprimé côté bot
disparaît aussi du site (pas de fichier fantôme).

---

## Identité & sections (`content/site.json`)

Source unique de l'identité du site et des sections affichées :
- **Identité / SEO** : `siteName`, `tagline`, `description`, `keywords`, `alternateNames`,
  `ogImage`, `theme`, `twitchChannel`. (Les URLs canoniques/OG viennent de
  `NEXT_PUBLIC_SITE_URL`, pas de ce fichier.)
- **Sections optionnelles** : `features` = `{ planning, ressources, friends, socials }`.
  Flag absent = activé. Mettre `false` masque la section/lien (cœur classement + profils
  toujours présent). Détails : [docs/TEMPLATE.md](../docs/TEMPLATE.md).

## Contenu éditorial

Fichiers versionnés dans `content/` (indépendants du bot) :
`site.json`, `socials.json`, `friends.json`, `schedule.json`, `downloads.json`.

Deux façons de les éditer :
1. **À la main** dans les fichiers JSON.
2. **Via `/admin` en dev** : `npm run dev` → page `/admin` → édite → **Exporter JSON** →
   remplace le fichier dans `content/` → rebuild. (L'admin n'est jamais déployé.)

---

## Développement

```powershell
npm install
$env:EXPORT_DIR="C:\Stream\streamerbot-xp-system\exports"
npm run dev        # http://localhost:3000  (/admin actif en dev)
```

## Build statique

```powershell
$env:EXPORT_DIR="C:\Stream\streamerbot-xp-system\exports"
npm run build      # copie les exports puis génère out/
```

Le dossier `out/` est un site 100 % statique déployable partout. `/admin` n'y est
pas servi (404).

---

## Déploiement

### Option 1 — Rebuild + redéploiement (le plus simple)
Tout se rafraîchit (classement, profils, nouveaux viewers).
- **Netlify** : `npx netlify deploy --prod --dir=out`
- **Vercel** : `npx vercel --prod` (ou connecter le repo)
- **GitHub Pages** : pousser le contenu de `out/`

Automatisable via une tâche planifiée Windows qui relance build + deploy pendant le live.

### Option 2 — Déploiement unique + sync des JSON (leaderboard « live »)
Déployer `out/` une fois, puis envoyer seulement `exports/*.json` vers le dossier
`/data/` de l'hébergeur (FTP/VPS ou bucket S3/R2). Le leaderboard se rafraîchit via
son polling (45 s) sans rebuild ; les profils se mettent à jour au prochain build.

### Option 3 — Temps réel (implémenté, V3.8)
Backend Node optionnel : `EXPORT_Snapshot` → `tools/push-to-backend.ps1` (tâche
planifiée) → `POST /api/push` → le site lit `/api/leaderboard` et `/api/users/:id` via
polling (45 s) quand `NEXT_PUBLIC_API_URL` est défini au build. Classement + profils +
podium d'accueil + embed se rafraîchissent sans rebuild. Voir
[docs/DEPLOY.md](../docs/DEPLOY.md) et `backend/README.md`.

---

## À venir (différé)

- **Badges** (sub/mod/vip…) et **classements hebdo/mensuel** : nécessitent une
  extension du contrat d'export côté bot (schemaVersion 2). Voir `ai/prompt/v3.5/R10`.
  En attendant : badges masqués, onglets « Ce mois / Cette semaine » désactivés.

---

## Stack

Next.js 16 · React 19 · TypeScript · TailwindCSS · `output: 'export'` · `npm audit` = 0.
