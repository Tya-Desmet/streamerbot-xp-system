# tools/migrate-remove-rank.ps1
# Supprime le champ Rank de tous les profils utilisateurs existants.
# Lancer UNE SEULE FOIS apres avoir mis a jour les actions SB.
#
# Usage :
#   .\tools\migrate-remove-rank.ps1 -DataPath 'C:\Stream\streamerbot-xp-system\data\users' -DryRun
#   .\tools\migrate-remove-rank.ps1 -DataPath 'C:\Stream\streamerbot-xp-system\data\users'

param(
    [string]$DataPath = "",
    [switch]$DryRun   = $false
)

if ([string]::IsNullOrEmpty($DataPath)) {
    Write-Error "Parametre DataPath obligatoire."
    Write-Host "Usage : .\tools\migrate-remove-rank.ps1 -DataPath 'C:\Stream\streamerbot-xp-system\data\users'"
    exit 1
}

if (!(Test-Path $DataPath)) {
    Write-Error "Dossier introuvable : $DataPath"
    exit 1
}

$files = Get-ChildItem $DataPath -Filter "*.json" | Where-Object { $_.Name -ne "_errors.log" }
Write-Host "Profils trouves : $($files.Count)"

$modified = 0
$errors   = 0

foreach ($file in $files) {
    try {
        $json    = Get-Content $file.FullName -Raw -Encoding UTF8
        $obj     = $json | ConvertFrom-Json
        $hasRank = $null -ne $obj.Rank

        if ($hasRank) {
            if (!$DryRun) {
                # Supprimer le champ Rank
                $obj.PSObject.Properties.Remove('Rank')

                # Sauvegarder atomiquement
                $newJson = $obj | ConvertTo-Json -Depth 10
                $tmpPath = $file.FullName + ".tmp"
                $newJson | Set-Content $tmpPath -Encoding UTF8
                if (Test-Path $file.FullName) { Remove-Item $file.FullName }
                Rename-Item $tmpPath $file.FullName

                Write-Host "Migre : $($file.Name)"
            } else {
                Write-Host "[DRY RUN] A migrer : $($file.Name)"
            }
            $modified++
        } else {
            Write-Host "Deja migre (pas de champ Rank) : $($file.Name)"
        }
    } catch {
        Write-Warning "Erreur sur $($file.Name) : $_"
        $errors++
    }
}

Write-Host ""
Write-Host "Termine : $modified profil(s) migre(s), $errors erreur(s)"
if ($DryRun) {
    Write-Host "Mode DRY RUN — aucune modification reelle. Relancer sans -DryRun pour appliquer."
}
