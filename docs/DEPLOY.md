# Déploiement — Stream Hub (site + backend) @ Infomaniak

Guide de mise en ligne complet. Architecture :

```
Bot (PC, Streamer.bot) ──exports/──▶ push-to-backend.ps1 ──▶ https://api.<domaine> (backend Node)
                                       (tâche planifiée)              ▲ GET /api/*
Visiteurs ──────────────────────────▶ https://<domaine>  (site statique) ──fetch──┘
```

> Deux composants : le **site statique** (dossier `out/`) et le **backend Node** (`backend/`).
> Recommandé : site sur `https://<domaine>`, backend sur le sous-domaine `https://api.<domaine>`.

---

## 0. Prérequis

- Hébergement Infomaniak **Web** (pour le site statique) + **Node.js managé** (pour le backend).
- Un domaine (ex. `mystya.tld`) + un sous-domaine `api.mystya.tld` pour le backend.
- Node 20+ en local pour builder le site.
- Le bot tourne et **`export.enabled = true`** dans son `config.json` (génère `exports/`).

---

## 1. Générer les secrets (une fois)

```powershell
# Mot de passe admin (à retenir) + secret JWT + clé de push (aléatoires)
[guid]::NewGuid().ToString("N")   # ex. pour JWT_SECRET
[guid]::NewGuid().ToString("N")   # ex. pour PUSH_API_KEY
```
Note-les : `ADMIN_PASSWORD`, `JWT_SECRET`, `PUSH_API_KEY`.

---

## 2. Backend (Infomaniak — Node.js managé)

1. **Déployer le code** `backend/` (Git recommandé, sinon SFTP/ZIP) sur l'hébergement Node.
2. **Build** : commande `npm install && npm run build`, **démarrage** : `npm start`
   (l'app écoute sur `process.env.PORT` fourni par Infomaniak).
3. **Variables d'environnement** (panel Infomaniak → Node → Variables) :
   ```
   ADMIN_PASSWORD = <ton mot de passe>
   JWT_SECRET     = <aléatoire long>
   PUSH_API_KEY   = <aléatoire>
   CORS_ORIGIN    = https://<domaine>        (le domaine EXACT du site)
   DATA_DIR       = /chemin/persistant/data  (hors de l'arbre redéployé si possible)
   ```
   > ⚠️ **Persistance** : `DATA_DIR` doit survivre aux redéploiements (c'est là que vivent
   > friends/schedule/resources/site + le live). Prévois une **sauvegarde** régulière.
4. **Domaine** : pointer `api.<domaine>` vers ce service (HTTPS fourni par Infomaniak).
5. **Vérifier** : `https://api.<domaine>/health` → `{"ok":true,...}`.

---

## 3. Site statique

1. **Régler la CSP** : dans `website/public/.htaccess` ET `website/public/_headers`,
   remplacer `https://api.EXEMPLE.tld` par `https://api.<domaine>`
   (ou retirer si tu n'utilises pas le backend).
2. **(Optionnel) Image de partage** : déposer `website/public/og.jpg` (1200×630, visuel sakura).
3. **Build** :
   ```powershell
   cd website
   $env:NEXT_PUBLIC_SITE_URL = "https://<domaine>"
   $env:NEXT_PUBLIC_API_URL  = "https://api.<domaine>"   # vide si pas de backend
   $env:EXPORT_DIR           = "C:\Stream\streamerbot-xp-system\exports"
   npm install
   npm run build      # génère website/out/
   ```
4. **Déployer `website/out/`** sur l'hébergement Web Infomaniak (racine du domaine),
   via SFTP/Git. Le `.htaccess` (en-têtes) part avec.
5. **Vérifier** : ouvrir `https://<domaine>` ; tester un changement dans `/admin` (login →
   publier) → visible sur le site sans rebuild.

> Sans backend : laisse `NEXT_PUBLIC_API_URL` vide → site 100% statique (contenu figé au build,
> admin en mode export local). C'est une mise en ligne valable aussi.

---

## 4. Temps réel (optionnel — leaderboard + profils live)

Le bot ne peut pas faire de HTTP fiable lui-même : une tâche Windows pousse les exports
vers le backend. Les secrets vivent dans `config.json` (jamais en arguments de tâche).

### 4a. Activer le push dans `config.json` (PC du bot)

```json
"export": {
  "enabled":     true,
  "pushEnabled": true,
  "pushUrl":     "https://api.<domaine>",
  "pushApiKey":  "<PUSH_API_KEY>"
}
```
> `pushApiKey` doit être **identique** au `PUSH_API_KEY` du backend (étape 2).

### 4b. Créer la tâche planifiée

`tools\push-to-backend.ps1` lit `config.json`, **assemble** le payload depuis
`exports/` (meta + leaderboard + users) et le POST sur `/api/push`.

```
Planificateur de tâches → Créer une tâche
  Déclencheur : à intervalle régulier (ex. toutes les 1 h, ou 2 min pendant le live)
  Action      : Démarrer un programme
    Programme/script : powershell.exe
    Arguments        : -NonInteractive -WindowStyle Hidden -NoProfile -ExecutionPolicy Bypass
                       -File "C:\Stream\streamerbot-xp-system\tools\push-to-backend.ps1"
```
> Pointer le `-File` vers la copie de **ton** install (à côté de `configs/` et `exports/`) :
> sans `-ConfigPath`, le script trouve seul le `config.json` et les `exports/` voisins.

### 4c. Vérifier

```powershell
powershell -ExecutionPolicy Bypass -File ".\tools\push-to-backend.ps1"
# attendu : [push] OK - backend mis a jour (users: N).
```
Le leaderboard (polling 45 s) et les profils viewer du site se rafraîchissent ensuite
via l'API.

---

## 5. Vérifications post-déploiement

- [ ] `https://api.<domaine>/health` → ok ; `…/api/content` → contenu.
- [ ] Site en ligne : nav, leaderboard, profils, planning, ressources.
- [ ] `/admin` : login (ADMIN_PASSWORD) → ajouter un copain → Publier → visible sur l'accueil.
- [ ] Console navigateur : **aucune violation CSP** (decapi, miniatures Twitch, API passent).
- [ ] `https://securityheaders.com` sur ton domaine → note A.
- [ ] Partage d'un lien (Discord/Twitter) → vignette OG (si `og.jpg` ajouté).
- [ ] `https://<domaine>/robots.txt` et `/sitemap.xml` accessibles.
- [ ] (Temps réel) après un run du pusher : `…/api/leaderboard` renvoie tes joueurs.

---

## 6. Rappels

- **Opt-out RGPD** : pour retirer un viewer, ajoute son pseudo dans
  `configs/excluded-users.json` (bot) → il disparaît de l'export, donc du site.
- **Mises à jour de contenu** : via `/admin` (si backend) ou en éditant `content/*.json`
  + rebuild (si statique).
- **Désactiver l'export** : `export.enabled=false` dans le config du bot → plus aucun export.
