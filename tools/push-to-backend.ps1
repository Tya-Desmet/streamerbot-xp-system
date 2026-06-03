#Requires -Version 5.1
# push-to-backend.ps1 - Streamer.bot XP System (Hub web V3)
#
# Pousse le dernier snapshot du bot vers le backend (POST /api/push).
# Transport fiable bot -> backend, independant du HTTP de Streamer.bot.
#
# Le payload est ASSEMBLE depuis les fichiers du contrat d'export
# (meta.json + leaderboard.json + users/*.json) -> aucune dependance a un
# fichier pre-assemble cote C#. Les secrets sont lus dans config.json
# (jamais passes en arguments de la tache planifiee).
#
# USAGE (Planificateur de taches Windows) :
#   powershell.exe -NonInteractive -WindowStyle Hidden -NoProfile -ExecutionPolicy Bypass
#     -File "<install>\tools\push-to-backend.ps1"
#     -ConfigPath "<install>\configs\config.json"
#
# -ConfigPath omis : on prend l'install live C:\Stream\streamerbot-xp-system
# si elle existe, sinon le config relatif au script (depot dev).

param(
    [string]$ConfigPath = ""
)

$ErrorActionPreference = 'Stop'

function Read-Json($path) {
    if (Test-Path $path) {
        try { return Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json } catch { return $null }
    }
    return $null
}

# 1. Localiser config.json
#    Priorite : -ConfigPath explicite > install live C:\Stream > config relatif au script.
if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $liveConfig = 'C:\Stream\streamerbot-xp-system\configs\config.json'
    if (Test-Path $liveConfig) {
        $ConfigPath = $liveConfig
    } else {
        $scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Definition
        $projectDir = Split-Path -Parent $scriptDir
        $ConfigPath = Join-Path $projectDir 'configs\config.json'
    }
}

if (-not (Test-Path $ConfigPath)) {
    Write-Host "[push] ERREUR config.json introuvable : $ConfigPath"
    exit 1
}

$cfg         = Get-Content -Raw -Path $ConfigPath | ConvertFrom-Json
$pushEnabled = $cfg.export.pushEnabled
$pushUrl     = ($cfg.export.pushUrl -replace '/+$', '')
$pushApiKey  = $cfg.export.pushApiKey

if (-not $pushEnabled) {
    Write-Host "[push] pushEnabled=false, rien a envoyer."
    exit 0
}
if ([string]::IsNullOrWhiteSpace($pushUrl)) {
    Write-Host "[push] ERREUR pushUrl vide dans config.json"
    exit 1
}
if ([string]::IsNullOrWhiteSpace($pushApiKey)) {
    Write-Host "[push] ERREUR pushApiKey vide dans config.json"
    exit 1
}

# 2. Localiser le dossier d'export (config.export.path, sinon <install>\exports)
$projectDir = Split-Path -Parent (Split-Path -Parent $ConfigPath)
$exportDir  = if ($cfg.export.path) { $cfg.export.path } else { Join-Path $projectDir 'exports' }

if (-not (Test-Path $exportDir)) {
    Write-Host "[push] ERREUR dossier exports introuvable : $exportDir"
    exit 1
}

# 3. Assembler le payload depuis les fichiers du contrat d'export
$meta        = Read-Json (Join-Path $exportDir 'meta.json')
$leaderboard = Read-Json (Join-Path $exportDir 'leaderboard.json')

if (-not $leaderboard) {
    Write-Host "[push] ERREUR leaderboard.json absent ou illisible dans : $exportDir"
    exit 1
}

$users    = @()
$usersDir = Join-Path $exportDir 'users'
if (Test-Path $usersDir) {
    foreach ($f in Get-ChildItem $usersDir -Filter *.json -ErrorAction SilentlyContinue) {
        $u = Read-Json $f.FullName
        if ($u) { $users += $u }
    }
}

$payload = @{ meta = $meta; leaderboard = $leaderboard; users = $users } | ConvertTo-Json -Depth 12

# 4. POST vers le backend
$endpoint = "$pushUrl/api/push"
$headers  = @{ 'X-Api-Key' = $pushApiKey }

Write-Host "[push] Envoi vers $endpoint ($($users.Count) profils) ..."

try {
    $r = Invoke-RestMethod -Uri $endpoint -Method POST -Headers $headers `
            -ContentType 'application/json; charset=utf-8' -Body $payload
    Write-Host "[push] OK - backend mis a jour (users: $($r.users))."
    exit 0
} catch {
    $code = $null
    if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
    Write-Host "[push] ECHEC HTTP $code : $($_.Exception.Message)"
    exit 1
}
