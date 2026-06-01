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
```
