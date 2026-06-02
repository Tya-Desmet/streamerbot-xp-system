# Stream Hub — site web (V3 / V3.5)

Hub communautaire **statique** (Next.js 16 + React 19, export statique) au style
**Tokyo Neon / Sakura**. Il affiche le leaderboard, les profils viewers, le planning
et les ressources à télécharger.

> **Lecture seule.** Aucun backend, aucune base de données, aucun compte utilisateur.
> Le bot Streamer.bot reste la seule source de vérité (cf. `ai/website.md`).

---

## Pages

| Route | Contenu | Source |
|---|---|---|
| `/` | Accueil : hero, copains en live, socials, Top 3 | `content/*` + données bot |
| `/leaderboard` | Classement (podium + liste, recherche, « Trouve-toi ») | données bot |
| `/viewer/[username]` | Profil viewer | données bot |
| `/planning` | Programme de la semaine + countdown | `content/schedule.json` |
| `/downloads` | Catalogue de ressources | `content/downloads.json` |
| `/admin` | **Éditeur local (dev uniquement)** — exclu du build prod | — |

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

## Contenu éditorial

Fichiers versionnés dans `content/` (indépendants du bot) :
`socials.json`, `friends.json`, `schedule.json`, `downloads.json`.

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

### Option 3 — Temps réel
Backend optionnel (lot **P10** de la V3, différé) : le bot POST ses données à une API
que le site consomme. Hors du périmètre statique.

---

## À venir (différé)

- **Badges** (sub/mod/vip…) et **classements hebdo/mensuel** : nécessitent une
  extension du contrat d'export côté bot (schemaVersion 2). Voir `ai/prompt/v3.5/R10`.
  En attendant : badges masqués, onglets « Ce mois / Cette semaine » désactivés.

---

## Stack

Next.js 16 · React 19 · TypeScript · TailwindCSS · `output: 'export'` · `npm audit` = 0.
