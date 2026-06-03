# tools/deploy-front.ps1
# Déploie le site (mystya.fr) : build local (avec tes vraies données) puis upload FTP.
# Lancement : double-clic sur deploy-front.bat, ou :
#   .\tools\deploy-front.ps1 -RemoteDir "/web"
#
# ⚠️ Règle RemoteDir sur le dossier où est servi mystya.fr (visible dans FileZilla).

param(
  [string]$FtpHost   = "ou2fa0.ftp.infomaniak.com",
  [string]$FtpUser   = "ou2fa0_mystya",
  [string]$RemoteDir = "/",                                  # <-- À ADAPTER (ex. /web, /sites/mystya.fr)
  [string]$ExportDir = "C:\Stream\streamerbot-xp-system\exports",
  [switch]$NoTls                                             # par défaut FTPS explicite ; -NoTls = FTP simple
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$web  = Join-Path $root "website"

# 1) Build du site avec les domaines de prod + tes exports locaux
Write-Host "== 1/3  Build du site ==" -ForegroundColor Cyan
Push-Location $web
$env:NEXT_PUBLIC_SITE_URL = "https://mystya.fr"
$env:NEXT_PUBLIC_API_URL  = "https://api.mystya.fr"
$env:EXPORT_DIR           = $ExportDir
npm run build
if ($LASTEXITCODE -ne 0) { Pop-Location; Write-Error "Build echoue."; exit 1 }
Pop-Location

# 2) Mot de passe FTP (saisie masquee, jamais stocke)
$sec = Read-Host "Mot de passe FTP pour $FtpUser" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
$pwd  = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
$userArg = "${FtpUser}:${pwd}"

# 3) Upload recursif de out/ (curl cree les dossiers distants au besoin)
$outDir = Join-Path $web "out"
$base   = (Resolve-Path $outDir).Path
$files  = Get-ChildItem $outDir -Recurse -File
$rd     = $RemoteDir.Trim('/')
$tls    = if ($NoTls) { @() } else { @('--ssl-reqd') }

Write-Host ("== 2/3  Upload de {0} fichiers vers {1}/{2} ==" -f $files.Count, $FtpHost, $rd) -ForegroundColor Cyan
$i = 0; $fail = 0
foreach ($f in $files) {
  $rel    = $f.FullName.Substring($base.Length + 1) -replace '\\', '/'
  $remote = "ftp://$FtpHost/" + ((@($rd, $rel) | Where-Object { $_ }) -join '/')
  curl.exe @tls --ftp-create-dirs -s -S -T $f.FullName $remote --user $userArg
  if ($LASTEXITCODE -ne 0) { $fail++; Write-Warning "Echec : $rel" }
  $i++
  if ($i % 25 -eq 0) { Write-Host ("  ... {0}/{1}" -f $i, $files.Count) }
}

Write-Host ("== 3/3  Termine : {0} fichiers, {1} echec(s) ==" -f $i, $fail) -ForegroundColor Green
if ($fail -gt 0) { Write-Warning "Certains fichiers ont echoue. Verifie RemoteDir / TLS (-NoTls) / mot de passe." }
