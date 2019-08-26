. $PSScriptRoot\genericHelpers.ps1

function SetupVSProjects
{
    param(
        [switch]$ForceRebuild,
        [Parameter(Mandatory=$false)][ref]$Succeeded
    )

    $SetupSucceeded = "False"   
    SetupExternalDownloads -Succeeded $SetupSucceeded

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