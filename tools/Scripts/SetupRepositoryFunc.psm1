Import-Module "$PSScriptRoot\SymbolicLinkHelpers.psm1"
Import-Module "$PSScriptRoot\ExternalDependencyHelpers.psm1"

function SetupRepository
{
    param(
        [switch] $NoDownloads,
        [switch] $NoBuilds,
        $MSBuild,
        [Parameter(Mandatory=$true)][ref]$Succeeded
    )

    $origLoc = Get-Location
    Set-Location $PSScriptRoot
    Write-Host "`n"
    
    ConfigureRepo

    If (!$NoDownloads -And $NoBuilds)
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
        $nativeBuildSuccess = $false
        if ($MSBuild)
        {
            . "$PSScriptRoot\..\ci\scripts\buildNativeProjectLocal.ps1" -MSBuild $MSBuild -Succeeded ([ref]$nativeBuildSuccess)
        }
        else
        {
            . "$PSScriptRoot\..\ci\scripts\buildNativeProjectLocal.ps1" -Succeeded ([ref]$nativeBuildSuccess)
        }

        $Succeeded.Value = $nativeBuildSuccess
    }
    else
    {
        $Succeeded.Value = $true
    }

    Set-Location $origLoc
}