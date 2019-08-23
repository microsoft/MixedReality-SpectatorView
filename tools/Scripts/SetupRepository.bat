@ECHO OFF
SETLOCAL
PowerShell.exe -NoProfile -ExecutionPolicy Bypass -Command "& '%~dpn0.ps1' -args %1%"