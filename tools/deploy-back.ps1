# tools/deploy-back.ps1
# Resynchronise le dossier backend/ vers le depot dedie stream-hub-backend (branche main).
# Ensuite : panel Infomaniak (api.mystya.fr) -> Build puis Redemarrer.
#
# Lancement : double-clic sur deploy-back.bat (ou .\tools\deploy-back.ps1)
# Pre-requis : avoir COMMITE tes changements backend/ (le resync se base sur l'historique git).

param(
  [string]$Remote = "https://github.com/Tya-Desmet/stream-hub-backend.git",
  [string]$Branch = "main"
)

$root = Split-Path -Parent $PSScriptRoot
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

Write-Host ("== 2/2  Envoi vers {0} ({1}) ==" -f $Remote, $Branch) -ForegroundColor Cyan
git push --force $Remote ("backend-deploy:" + $Branch)
$pushOk = ($LASTEXITCODE -eq 0)
git branch -D backend-deploy 2>$null

if ($pushOk) {
  Write-Host "== OK : depot backend a jour ==" -ForegroundColor Green
  Write-Host ""
  Write-Host "-> Va sur le panel Infomaniak (api.mystya.fr) :" -ForegroundColor Yellow
  Write-Host "   1) clique BUILD   2) clique REDEMARRER" -ForegroundColor Yellow
} else {
  Write-Error "Echec du push (verifie ta connexion / tes droits GitHub)."
}
