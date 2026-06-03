# Outils de build

## build-actions.ps1

Assemble les actions Streamer.bot depuis leurs composants.

Usage :
  .\tools\build-actions.ps1              <- toutes les actions
  .\tools\build-actions.ps1 -Action XP_Add  <- une action specifique

Sortie : actions/generated/*.cs
Ces fichiers sont a copier-coller dans Streamer.bot.

## Workflow de modification d'un service

1. Modifier le service dans scripts/ (ex: scripts/XpService.cs)
2. Lancer : .\tools\build-actions.ps1
3. Copier les fichiers generes dans SB et recompiler

## Structure

```
actions/
  _headers/    <- documentation + using statements (une action = un fichier)
  _bodies/     <- classe CPHInline uniquement (une action = un fichier)
  generated/   <- fichiers finaux a copier dans SB (generes par le script)

scripts/
  UserRepository.cs
  ConfigService.cs
  XpService.cs
  ValidationService.cs
  WatchTimeService.cs
  BotExclusionService.cs
  RankService.cs
  TitleService.cs
  RewardService.cs
```

## Hub web (site public)

### push-to-backend.ps1

Pousse le dernier snapshot du bot vers le backend (`POST /api/push`).
Lit `config.json` (secrets : `export.pushUrl`, `export.pushApiKey`), **assemble** le
payload depuis `exports/` (meta + leaderboard + users/*) puis l'envoie. À planifier
dans le Planificateur de tâches Windows.

Usage :
  .\tools\push-to-backend.ps1                                  <- config auto (C:\Stream, sinon depot)
  .\tools\push-to-backend.ps1 -ConfigPath "C:\...\config.json" <- config explicite

Sans `-ConfigPath` : prend `C:\Stream\streamerbot-xp-system\configs\config.json` s'il
existe, sinon le `config.json` relatif au script. Le dossier d'export = `export.path`
du config, ou `<install>\exports`.

### deploy-front.ps1

Build du site (avec tes exports locaux + domaines de prod) puis upload FTP vers
Infomaniak. Demande le mot de passe FTP (jamais stocké). Réessaie chaque fichier
3 fois (réseau instable).

Usage :
  .\tools\deploy-front.ps1
  .\tools\deploy-front.ps1 -RemoteDir "/sites/mondomaine.fr"

### deploy-back.ps1

Resync du dépôt backend dédié (code `backend/`) pour redéploiement Infomaniak.

→ Mise en ligne complète : [../docs/DEPLOY.md](../docs/DEPLOY.md)

---

## migrate-add-reward-fields.ps1

Ajoute les champs V2.6 manquants (XpFromChat, XpFromWatch, XpFromRewards,
ActiveBonusMultiplier, BonusExpiryTimestamp, CheckInCount, LastCheckInDay,
TotalCheckIns) aux profils JSON existants.

Usage :
  .\tools\migrate-add-reward-fields.ps1 -DataPath "C:\...\data\users" -DryRun
  .\tools\migrate-add-reward-fields.ps1 -DataPath "C:\...\data\users"

A lancer UNE SEULE FOIS apres deploiement de V2.6.
