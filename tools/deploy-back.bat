@echo off
REM Double-clic pour resync le backend vers stream-hub-backend (puis Build + Redemarrer dans le panel).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0deploy-back.ps1" %*
echo.
pause
