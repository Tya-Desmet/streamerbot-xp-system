# tools/deploy-back.ps1
# Resynchronise le dossier backend/ vers ton depot backend dedie (branche de deploiement).
# Ensuite : panel d'hebergement Node (ton sous-domaine api) -> Build puis Redemarrer.
#
# Config de deploiement : voir tools\deploy.local.ps1 (gitignore) -> BackendRemote / BackendBranch.
# Priorite : defaults neutres < deploy.local.ps1 < arguments explicites.
#
# Lancement : double-clic sur deploy-back.bat (ou .\tools\deploy-back.ps1)
# Pre-requis : avoir COMMITE tes changements backend/ (le resync se base sur l'historique git).

param(
  [string]$Remote,
  [string]$Branch
)

$root = Split-Path -Parent $PSScriptRoot

# --- Resolution de la config (defaults neutres < deploy.local.ps1 < arguments) ---
$backendRemote = 'https://github.com/exemple/stream-hub-backend.git'
$backendBranch = 'main'
$localCfg = Join-Path $PSScriptRoot 'deploy.local.ps1'
if (Test-Path $localCfg) {
  . $localCfg
  if ($DeployConfig) {
    if ($DeployConfig.BackendRemote) { $backendRemote = $DeployConfig.BackendRemote }
    if ($DeployConfig.BackendBranch) { $backendBranch = $DeployConfig.BackendBranch }
  }
}
if ($Remote) { $backendRemote = $Remote }
if ($Branch) { $backendBranch = $Branch }

Set-Location $root

# Avertir si des changements backend/ ne sont pas commites
$dirty = git status --porcelain -- backend
if ($dirty) {
  Write-Warning "Des changements dans backend/ ne sont PAS commites (ils ne seront pas deployes) :"
  Write-Host $dirty
  $ans = Read-Host "Continuer quand meme ? (o/N)"
  if ($ans -ne 'o') { Write-Host "Annule."; exit 1 }
}

Write-Host "== 1/2  Extraction de backend/ ==" -ForegroundColor Cyan
git branch -D backend-deploy 2>$null
git subtree split --prefix=backend -b backend-deploy
if ($LASTEXITCODE -ne 0) { Write-Error "Echec du split."; exit 1 }

Write-Host ("== 2/2  Envoi vers {0} ({1}) ==" -f $backendRemote, $backendBranch) -ForegroundColor Cyan
git push --force $backendRemote ("backend-deploy:" + $backendBranch)
$pushOk = ($LASTEXITCODE -eq 0)
git branch -D backend-deploy 2>$null

if ($pushOk) {
  Write-Host "== OK : depot backend a jour ==" -ForegroundColor Green
  Write-Host ""
  Write-Host "-> Va sur le panel de ton hebergement Node (sous-domaine api) :" -ForegroundColor Yellow
  Write-Host "   1) clique BUILD   2) clique REDEMARRER" -ForegroundColor Yellow
} else {
  Write-Error "Echec du push (verifie ta connexion / tes droits GitHub)."
}
