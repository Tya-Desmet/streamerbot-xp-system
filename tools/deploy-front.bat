@echo off
REM Double-clic pour deployer le site (build + upload FTP).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0deploy-front.ps1" %*
echo.
pause
