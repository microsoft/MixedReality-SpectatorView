. "$PSScriptRoot\SymbolicLinkHelpers.ps1"

Write-Output "Enabling symbolic links for the repository."
git config core.symlinks true

# Ensure that submodules are initialized and cloned.
Write-Output "Updating all submodules."
git submodule update --init

# If any links were created to the submodules but were broken,
# restore those symlinks now that the submodules are cloned.
Write-Output "Fixing all symbolic links."
dir Get-Location -Recurse -File | ?{$_.LinkType -eq "SymbolicLink" } | Restore-SymbolicLink

# If any links were created before the repository was configured to use
# symlinks, those links need to be restored.
git status --porcelain | Restore-SymbolicLinkFromTypeChange

Write-Host "`n"
Write-Host -NoNewLine 'Symbolic links fixed. Press any key to continue...';
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');
Write-Host "`n"