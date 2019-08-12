. "$PSScriptRoot\SymbolicLinkHelpers.ps1"

function SetupRepository
{
    $temp = Get-Location
    Set-Location $PSScriptRoot
    Write-Host "`n"
    
    ConfigureRepo
    
    # Ensure that submodules are initialized and cloned.
    Write-Output "Updating spectator view related submodules."
    git submodule update --init
    
    FixSymbolicLinks
    Set-Location $temp
}