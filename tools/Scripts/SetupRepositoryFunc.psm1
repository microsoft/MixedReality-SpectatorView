Import-Module "$PSScriptRoot\SymbolicLinkHelpers.psm1"
Import-Module "$PSScriptRoot\ExternalDependencyHelpers.psm1"

function SetupRepository
{
    param(
        [switch] $NoDownloads,
        [switch] $NoBuilds
    )
    
    $origLoc = Get-Location
    Set-Location $PSScriptRoot
    Write-Host "`n"
    
    ConfigureRepo

    If (!$NoDownloads)
    {
        DownloadQRCodePlugin
    }

    # Ensure that submodules are initialized and cloned.
    Write-Output "Updating spectator view related submodules."
    git submodule sync
    git submodule update --init
    
    FixSymbolicLinksForDirectory -Directory "$PSScriptRoot\..\..\src\SpectatorView.Unity\"

    If (!$NoBuilds)
    {
      & "$PSScriptRoot\..\ci\scripts\buildNativeProjectLocal.ps1"
    }

    Set-Location $origLoc
}