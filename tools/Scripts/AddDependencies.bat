@ECHO OFF

SET arg1=%~1
SET arg2=%~2
PowerShell.exe -NoProfile -Command "& '%~dpn0.ps1' '%arg1%' '%arg2%'"