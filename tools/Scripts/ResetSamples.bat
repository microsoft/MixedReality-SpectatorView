@ECHO OFF
SETLOCAL
SET PowerShellScriptPath=%~dpn0.ps1
PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& '%PowerShellScriptPath%' %1 %2;exit $LASTEXITCODE"