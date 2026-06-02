# tools/migrate-add-reward-fields.ps1
# Ajoute les champs rewards manquants aux profils existants.
# Sécurisé : ne touche pas les champs déjà présents.
# Lancer UNE SEULE FOIS après déploiement de V2.6 dans SB.

param(
    [string]$DataPath = "",
    [switch]$DryRun   = $false
)

if ([string]::IsNullOrEmpty($DataPath)) {
    Write-Error "Parametre DataPath obligatoire."
    Write-Host "Usage : .\tools\migrate-add-reward-fields.ps1 -DataPath 'C:\Stream\...\data\users'"
    exit 1
}

if (!(Test-Path $DataPath)) {
    Write-Error "Dossier introuvable : $DataPath"
    exit 1
}

$files    = Get-ChildItem $DataPath -Filter "*.json" | Where-Object { $_.Name -ne "_errors.log" }
$modified = 0
$skipped  = 0
$errors   = 0

Write-Host "Profils trouves : $($files.Count)"

foreach ($file in $files) {
    try {
        $json = Get-Content $file.FullName -Raw -Encoding UTF8
        $obj  = $json | ConvertFrom-Json

        $needsMigration = ($null -eq $obj.XpFromChat) -or
                          ($null -eq $obj.XpFromWatch) -or
                          ($null -eq $obj.XpFromRewards) -or
                          ($null -eq $obj.ActiveBonusMultiplier) -or
                          ($null -eq $obj.BonusExpiryTimestamp) -or
                          ($null -eq $obj.CheckInCount) -or
                          ($null -eq $obj.LastCheckInDay) -or
                          ($null -eq $obj.TotalCheckIns)

        if (!$needsMigration) {
            Write-Host "Deja migre : $($file.Name)"
            $skipped++
            continue
        }

        if (!$DryRun) {
            if ($null -eq $obj.XpFromChat)            { $obj | Add-Member -NotePropertyName XpFromChat            -NotePropertyValue 0   -Force }
            if ($null -eq $obj.XpFromWatch)           { $obj | Add-Member -NotePropertyName XpFromWatch           -NotePropertyValue 0   -Force }
            if ($null -eq $obj.XpFromRewards)         { $obj | Add-Member -NotePropertyName XpFromRewards         -NotePropertyValue 0   -Force }
            if ($null -eq $obj.ActiveBonusMultiplier) { $obj | Add-Member -NotePropertyName ActiveBonusMultiplier -NotePropertyValue 1.0 -Force }
            if ($null -eq $obj.BonusExpiryTimestamp)  { $obj | Add-Member -NotePropertyName BonusExpiryTimestamp  -NotePropertyValue 0   -Force }
            if ($null -eq $obj.CheckInCount)          { $obj | Add-Member -NotePropertyName CheckInCount          -NotePropertyValue 0   -Force }
            if ($null -eq $obj.LastCheckInDay)        { $obj | Add-Member -NotePropertyName LastCheckInDay        -NotePropertyValue 0   -Force }
            if ($null -eq $obj.TotalCheckIns)         { $obj | Add-Member -NotePropertyName TotalCheckIns         -NotePropertyValue 0   -Force }

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
    } catch {
        Write-Warning "Erreur sur $($file.Name) : $_"
        $errors++
    }
}

Write-Host ""
Write-Host "Termine : $modified migre(s), $skipped deja ok, $errors erreur(s)"
if ($DryRun) { Write-Host "Mode DRY RUN — relancer sans -DryRun pour appliquer." }
