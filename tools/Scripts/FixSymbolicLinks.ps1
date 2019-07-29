. "$PSScriptRoot\SymbolicLinkHelpers.ps1"

ConfigureRepo

# Ensure that submodules are initialized and cloned.
Write-Output "Updating all submodules."
git submodule update --init --recursive

FixSymbolicLinks

Write-Host "`n"
Write-Host -NoNewLine 'Symbolic links fixed. Press any key to continue...';
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');
Write-Host "`n"