@echo off
set PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe
set SCRIPT=%~dp0push-to-backend.ps1
set LOG=%~dp0push-backend.log

echo [%date% %time%] Lancement... >> "%LOG%"
"%PS%" -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" >> "%LOG%" 2>&1
echo [%date% %time%] Code sortie: %ERRORLEVEL% >> "%LOG%"
