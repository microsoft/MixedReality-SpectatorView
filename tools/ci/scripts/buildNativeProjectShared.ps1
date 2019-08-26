. $PSScriptRoot\genericHelpers.ps1
. $PSScriptRoot\spectatorViewHelpers.ps1

function SetupVSProjects
{
    param(
        [switch]$ForceRebuild,
        [switch]$Remote,
        [Parameter(Mandatory=$false)][ref]$Succeeded
    )

    $SetupSucceeded = "False"
    if ($Remote)
    {
        SetupExternalDownloads -Succeeded ([ref]$SetupSucceeded) -Remote
    }
    else
    {
        SetupExternalDownloads -Succeeded ([ref]$SetupSucceeded)
    }

    $OpenCVSucceeded = "False"
    if ($ForceRebuild)
    {
        BuildOpenCV -ForceRebuild -Succeeded ([ref]$OpenCVSucceeded)
    }
    else
    {
        BuildOpenCV -Succeeded ([ref]$OpenCVSucceeded)
    }
    Write-Host "OpenCV Build Succeeded: $OpenCVSucceeded"

    AddElgatoSubmodule
    $ElgatoResult = $?
    Write-Host "Adding elgato submodule succeeded: $ElgatoResult"

    if ($Succeeded)
    {
        $Succeeded.Value = $SetupSucceeded -And $OpenCVSucceeded -And $ElgatoResult
    }
}