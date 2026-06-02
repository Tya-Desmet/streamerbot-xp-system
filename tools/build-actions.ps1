# tools/build-actions.ps1
# Usage : .\tools\build-actions.ps1
# Usage : .\tools\build-actions.ps1 -Action XP_Add
# Genere les fichiers dans actions/generated/

param(
    [string]$Action = "all"
)

$Root    = Split-Path -Parent $PSScriptRoot
$Scripts = "$Root\scripts"
$Headers = "$Root\actions\_headers"
$Bodies  = "$Root\actions\_bodies"
$Output  = "$Root\actions\generated"

if (!(Test-Path $Output)) { New-Item -ItemType Directory -Path $Output | Out-Null }

$ActionDeps = @{
    "XP_Add" = @(
        "UserRepository",
        "ValidationService",
        "XpService",
        "RewardService",
        "ConfigService"
    )
    "XP_WatchTime_V2" = @(
        "UserRepository",
        "WatchTimeService",
        "XpService",
        "RewardService",
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
        "ConfigService",
        "RewardService"
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
    "REWARD_BonusXp" = @(
        "UserRepository",
        "RewardService",
        "ConfigService"
    )
    "DAILY_CheckIn" = @(
        "UserRepository",
        "XpService",
        "BotExclusionService",
        "ConfigService"
    )
    "REWARD_GrantXp" = @(
        "UserRepository",
        "XpService",
        "ConfigService"
    )
    "EXPORT_Snapshot" = @(
        "UserRepository",
        "XpService",
        "TitleService",
        "BotExclusionService",
        "ConfigService",
        "ExportService"
    )
}

function Build-Action {
    param([string]$Name)

    Write-Host "Assemblage de $Name..."

    # Construire la liste ordonnee des fichiers a assembler
    $files = @()

    $headerPath = "$Headers\$Name.cs"
    if (Test-Path $headerPath) {
        $files += [PSCustomObject]@{ Path = $headerPath; Label = "" }
    } else {
        Write-Warning "Header manquant : $headerPath"
    }

    foreach ($dep in $ActionDeps[$Name]) {
        $sp = "$Scripts\$dep.cs"
        if (Test-Path $sp) {
            $files += [PSCustomObject]@{ Path = $sp; Label = "$dep (source : scripts/$dep.cs)" }
        } else {
            Write-Warning "Service manquant : $sp"
        }
    }

    $bodyPath = "$Bodies\$Name.cs"
    if (Test-Path $bodyPath) {
        $files += [PSCustomObject]@{ Path = $bodyPath; Label = "Action Streamer.bot" }
    } else {
        Write-Warning "Body manquant : $bodyPath"
    }

    # Passe 1 : collecter tous les "using" uniques dans l'ordre d'apparition
    $seenUsings = @{}
    $allUsings  = @()
    foreach ($f in $files) {
        $lines = Get-Content $f.Path -Encoding UTF8
        foreach ($line in $lines) {
            if ($line -match '^\s*using\s+\S') {
                $u = $line.Trim()
                if (-not $seenUsings.ContainsKey($u)) {
                    $seenUsings[$u] = $true
                    $allUsings += $u
                }
            }
        }
    }

    # Passe 2 : assembler le corps sans les lignes "using"
    $bodyParts = @()
    foreach ($f in $files) {
        $lines     = Get-Content $f.Path -Encoding UTF8
        $bodyLines = @()
        foreach ($line in $lines) {
            if ($line -notmatch '^\s*using\s+\S') {
                $bodyLines += $line
            }
        }
        $block = ($bodyLines -join "`n").Trim()
        if ($block -ne "") {
            if ($f.Label -ne "") {
                $bodyParts += "// ----- $($f.Label) -----`n`n$block"
            } else {
                $bodyParts += $block
            }
        }
    }

    # Assemblage final : usings en tete, puis le code
    $final = ($allUsings -join "`n") + "`n`n" + ($bodyParts -join "`n`n")

    $outputPath = "$Output\$Name.cs"
    [System.IO.File]::WriteAllText($outputPath, $final, [System.Text.Encoding]::UTF8)
    Write-Host "OK : $outputPath"
}

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
