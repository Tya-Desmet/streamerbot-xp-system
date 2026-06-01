# tools/build-actions.ps1
# Usage : .\tools\build-actions.ps1
# Genere les fichiers dans actions/generated/

param(
    [string]$Action = "all"  # "all" ou le nom d'une action specifique
)

$Root    = Split-Path -Parent $PSScriptRoot
$Scripts = "$Root\scripts"
$Headers = "$Root\actions\_headers"
$Bodies  = "$Root\actions\_bodies"
$Output  = "$Root\actions\generated"

if (!(Test-Path $Output)) { New-Item -ItemType Directory -Path $Output | Out-Null }

# Definition des dependances de chaque action
$ActionDeps = @{
    "XP_Add" = @(
        "UserRepository",
        "ValidationService",
        "XpService",
        "ConfigService"
    )
    "XP_WatchTime_V2" = @(
        "UserRepository",
        "WatchTimeService",
        "XpService",
        "BotExclusionService",
        "ConfigService"
    )
    "USER_GetOrCreate" = @(
        "UserRepository",
        "BotExclusionService",
        "ConfigService"
    )
    "LEADERBOARD_Update" = @(
        "UserRepository",
        "XpService",
        "BotExclusionService",
        "ConfigService"
    )
    "CARD_ShowProfile" = @(
        "UserRepository",
        "XpService",
        "TitleService",
        "BotExclusionService",
        "ConfigService"
    )
    "RANK_ShowCommand" = @(
        "UserRepository",
        "XpService",
        "RankService",
        "TitleService",
        "BotExclusionService",
        "ConfigService"
    )
    "SYSTEM_Validate" = @(
        "ConfigService"
    )
}

function Build-Action {
    param([string]$Name)

    Write-Host "Assemblage de $Name..."

    $content = ""

    # 1. Header (documentation + using)
    $headerPath = "$Headers\$Name.cs"
    if (Test-Path $headerPath) {
        $content += Get-Content $headerPath -Raw -Encoding UTF8
        $content += "`n`n"
    } else {
        Write-Warning "Header manquant : $headerPath"
    }

    # 2. Services (depuis scripts/)
    $deps = $ActionDeps[$Name]
    foreach ($dep in $deps) {
        $servicePath = "$Scripts\$dep.cs"
        if (Test-Path $servicePath) {
            $content += "// ----- $dep (source : scripts/$dep.cs) -----`n`n"
            $content += Get-Content $servicePath -Raw -Encoding UTF8
            $content += "`n`n"
        } else {
            Write-Warning "Service manquant : $servicePath"
        }
    }

    # 3. Corps de l'action (CPHInline)
    $bodyPath = "$Bodies\$Name.cs"
    if (Test-Path $bodyPath) {
        $content += "// ----- Action Streamer.bot -----`n`n"
        $content += Get-Content $bodyPath -Raw -Encoding UTF8
    } else {
        Write-Warning "Body manquant : $bodyPath"
    }

    # Ecriture dans generated/
    $outputPath = "$Output\$Name.cs"
    $content | Set-Content $outputPath -Encoding UTF8

    Write-Host "OK : $outputPath"
}

# Assemblage
if ($Action -eq "all") {
    foreach ($name in $ActionDeps.Keys) {
        Build-Action $name
    }
} else {
    if ($ActionDeps.ContainsKey($Action)) {
        Build-Action $Action
    } else {
        Write-Error "Action inconnue : $Action. Options : $($ActionDeps.Keys -join ', ')"
    }
}

Write-Host "`nAssemblage termine. Copier les fichiers de actions/generated/ dans Streamer.bot."
