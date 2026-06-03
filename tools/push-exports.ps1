# tools/push-exports.ps1
# Pousse les exports du bot (exports/*.json) vers le backend (POST /api/push).
# Transport fiable bot -> backend, indépendant du HTTP de Streamer.bot.
# À lancer sur une tâche planifiée Windows (ex. toutes les 1-2 min pendant le live).
#
# Usage :
#   .\tools\push-exports.ps1 -ApiUrl "https://api.exemple.tld" -ApiKey "..." `
#       -ExportDir "C:\Stream\streamerbot-xp-system\exports"

param(
  [Parameter(Mandatory = $true)][string]$ApiUrl,
  [Parameter(Mandatory = $true)][string]$ApiKey,
  [string]$ExportDir = "C:\Stream\streamerbot-xp-system\exports"
)

function Read-Json($path) {
  if (Test-Path $path) {
    try { return Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json } catch { return $null }
  }
  return $null
}

if (-not (Test-Path $ExportDir)) {
  Write-Warning "[push-exports] dossier exports introuvable : $ExportDir"
  exit 1
}

$meta = Read-Json (Join-Path $ExportDir "meta.json")
$leaderboard = Read-Json (Join-Path $ExportDir "leaderboard.json")

$users = @()
$usersDir = Join-Path $ExportDir "users"
if (Test-Path $usersDir) {
  foreach ($f in Get-ChildItem $usersDir -Filter *.json -ErrorAction SilentlyContinue) {
    $u = Read-Json $f.FullName
    if ($u) { $users += $u }
  }
}

$payload = @{ meta = $meta; leaderboard = $leaderboard; users = $users } | ConvertTo-Json -Depth 12

try {
  $uri = $ApiUrl.TrimEnd('/') + "/api/push"
  $r = Invoke-RestMethod -Uri $uri -Method POST `
        -Headers @{ "X-Api-Key" = $ApiKey } `
        -ContentType "application/json; charset=utf-8" -Body $payload
  Write-Host ("[push-exports] OK -> " + $uri + " (users: " + $r.users + ")")
} catch {
  Write-Warning ("[push-exports] echec : " + $_.Exception.Message)
  exit 1
}
