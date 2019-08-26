. $PSScriptRoot\genericHelpers.ps1

function SetupVSProjects
{
    param(
        [switch]$ForceRebuild,
        [Parameter(Mandatory=$false)][ref]$Succeeded
    )

    $SetupSuceeded = "False"   
    if (Test-Path $PSScriptRoot\setupDependenciesInternal.ps1)
    {
        # This script set up dependencies for Blackmagic Design dependencies
        # The behavior can be manually duplicated by populating an 'external\dependencies\BlackmagicDesign\Blackmagic DeckLink SDK 10.9.11' directory in your repo
        . $PSScriptRoot\setupDependenciesInternal.ps1 -Succeeded ([ref]$SetupSuceeded)
        Write-Host "BlackmagicDesign dependencies found: $SetupSuceeded"
    }
    else
    {
        $SetupSuceeded = Test-Path "$PSScriptRoot\..\..\..\external\dependencies\BlackmagicDesign\Blackmagic DeckLink SDK 10.9.11"
        Write-Host "BlackmagicDesign dependencies found: $SetupSucceeded"
        if (!SetupSucceeded)
        {
            Write-Host "Native build will fail based on a missing BlackmagicDesign dependency."
            Write-Host "To fix this issue, obtain Blackmagic DeckLink SDK 10.9.11 and add it to a external\dependencies\BlackmagicDesign\Blackmagic DeckLink SDK 10.9.11 directory."
        }
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
        $Succeeded.Value = $SetupSuceeded -And $OpenCVSucceeded -And $ElgatoResult
    }
}