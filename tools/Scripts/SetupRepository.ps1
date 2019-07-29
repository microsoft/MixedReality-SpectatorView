. "$PSScriptRoot\SymbolicLinkHelpers.ps1"

Set-Location (Split-Path $MyInvocation.MyCommand.Path)
Write-Host "`n"

ConfigureRepo

# Ensure that submodules are initialized and cloned.
Write-Output "Updating spectator view related submodules."
git submodule update --init

FixSymbolicLinks

Write-Host "`n"
Write-Host -NoNewLine 'Setup Completed. Press any key to continue...';
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');
Write-Host "`n"