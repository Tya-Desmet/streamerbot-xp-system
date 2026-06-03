# tools/deploy-front.ps1
# Deploie le site : build local (avec tes vraies donnees) puis upload FTP.
# Lancement : double-clic sur deploy-front.bat, ou : .\tools\deploy-front.ps1
#
# Config de deploiement : voir tools\deploy.local.ps1 (gitignore).
# Priorite : defaults neutres < deploy.local.ps1 < arguments explicites.
#   ex. override ponctuel : .\tools\deploy-front.ps1 -RemoteDir "/web"

param(
  [string]$FtpHost,
  [string]$FtpUser,
  [string]$RemoteDir,
  [string]$SiteUrl,
  [string]$ApiUrl,
  [string]$ExportDir,
  [switch]$NoTls            # par defaut FTPS explicite ; -NoTls = FTP simple
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$web  = Join-Path $root "website"

# --- Resolution de la config (defaults neutres < deploy.local.ps1 < arguments) ---
$cfg = @{
  FtpHost   = 'ftp.exemple.fr'
  FtpUser   = 'user_exemple'
  RemoteDir = '/sites/exemple.fr'
  SiteUrl   = 'https://exemple.fr'
  ApiUrl    = 'https://api.exemple.fr'
  ExportDir = (Join-Path $root 'exports')
}
$localCfg = Join-Path $PSScriptRoot 'deploy.local.ps1'
if (Test-Path $localCfg) {
  . $localCfg
  if ($DeployConfig) { foreach ($k in $DeployConfig.Keys) { if ($cfg.ContainsKey($k)) { $cfg[$k] = $DeployConfig[$k] } } }
} else {
  Write-Warning "tools\deploy.local.ps1 absent -> valeurs d'exemple."
  Write-Warning "Copie deploy.local.ps1.example en deploy.local.ps1 et renseigne tes valeurs."
}
# Arguments explicites prioritaires
if ($FtpHost)   { $cfg.FtpHost   = $FtpHost }
if ($FtpUser)   { $cfg.FtpUser   = $FtpUser }
if ($RemoteDir) { $cfg.RemoteDir = $RemoteDir }
if ($SiteUrl)   { $cfg.SiteUrl   = $SiteUrl }
if ($ApiUrl)    { $cfg.ApiUrl    = $ApiUrl }
if ($ExportDir) { $cfg.ExportDir = $ExportDir }

# 1) Build du site avec les domaines de prod + tes exports locaux
Write-Host "== 1/3  Build du site ==" -ForegroundColor Cyan
Push-Location $web
# Install des dependances si absentes (premiere install / 'next' introuvable)
if (-not (Test-Path (Join-Path $web "node_modules\.bin\next*"))) {
  Write-Host "  node_modules absent -> npm install ..." -ForegroundColor Yellow
  npm install
  if ($LASTEXITCODE -ne 0) { Pop-Location; Write-Error "npm install echoue."; exit 1 }
}
$env:NEXT_PUBLIC_SITE_URL = $cfg.SiteUrl
$env:NEXT_PUBLIC_API_URL  = $cfg.ApiUrl
$env:EXPORT_DIR           = $cfg.ExportDir
npm run build
if ($LASTEXITCODE -ne 0) { Pop-Location; Write-Error "Build echoue."; exit 1 }
Pop-Location

# 2) Mot de passe FTP (saisie masquee, jamais stocke)
$sec = Read-Host ("Mot de passe FTP pour " + $cfg.FtpUser) -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
$ftpPwd = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
$userArg = "$($cfg.FtpUser):${ftpPwd}"

# 3) Upload recursif de out/ (curl cree les dossiers distants au besoin)
$outDir = Join-Path $web "out"
$base   = (Resolve-Path $outDir).Path
$files  = Get-ChildItem $outDir -Recurse -File
$rd     = $cfg.RemoteDir.Trim('/')

Write-Host ("== 2/3  Upload de {0} fichiers vers {1}/{2} ==" -f $files.Count, $cfg.FtpHost, $rd) -ForegroundColor Cyan
$i = 0; $fail = 0
foreach ($f in $files) {
  $rel    = $f.FullName.Substring($base.Length + 1) -replace '\\', '/'
  $remote = "ftp://$($cfg.FtpHost)/" + ((@($rd, $rel) | Where-Object { $_ }) -join '/')

  # Tableau d'arguments explicite (evite tout souci de parsing)
  $cargs = [System.Collections.Generic.List[string]]::new()
  if (-not $NoTls) { $cargs.Add('--ssl-reqd') }
  $cargs.Add('--ftp-create-dirs')
  $cargs.Add('--silent')
  $cargs.Add('--show-error')
  $cargs.Add('--user');        $cargs.Add($userArg)
  $cargs.Add('--upload-file'); $cargs.Add($f.FullName)
  $cargs.Add($remote)

  $ok = $false
  for ($try = 1; $try -le 3; $try++) {
    & curl.exe $cargs.ToArray()
    if ($LASTEXITCODE -eq 0) { $ok = $true; break }
    if ($try -lt 3) { Start-Sleep -Seconds 2 }
  }
  if (-not $ok) { $fail++; if ($fail -le 5) { Write-Warning "Echec : $rel" } }
  $i++
  if ($i % 25 -eq 0) { Write-Host ("  ... {0}/{1}" -f $i, $files.Count) }
}

Write-Host ("== 3/3  Termine : {0} fichiers, {1} echec(s) ==" -f $i, $fail) -ForegroundColor Green
if ($fail -gt 0) { Write-Warning "Certains fichiers ont echoue. Verifie RemoteDir / TLS (-NoTls) / mot de passe." }
