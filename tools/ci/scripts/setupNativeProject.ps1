# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See LICENSE in the project root for license information.

param(
    [switch]$ForceRebuild,
    [switch]$NoDownloads,
    [switch]$ExcludeBlackmagic,
    [Parameter(Mandatory=$false)][ref]$Succeeded
)

Import-Module $PSScriptRoot\spectatorViewHelpers.psm1

Write-Host "Setting up Blackmagic design dependencies"
$BlackMagicResult = "False"
if ($ExcludeBlackmagic)
{
    $BlackmagicResult = $true
}
elseif ($NoDownloads)
{
    SetupExternalDownloads -NoDownloads -Succeeded ([ref]$BlackMagicResult)
}
else
{
    SetupExternalDownloads -Succeeded ([ref]$BlackMagicResult)
}
Write-Host "`nSetting up Blackmagic design dependencies Succeeded: $BlackMagicResult`n"

$ElgatoResult = "False"
if ($BlackMagicResult -eq $true)
{
    Write-Host "Setting up elgato dependencies"
    AddElgatoSubmodule
    $ElgatoResult = $?
    Write-Host "`nAdding elgato dependencies succeeded: $ElgatoResult`n"
}

$OpenCVSucceeded = "False"
if (($BlackMagicResult -eq $true) -And ($ElgatoResult -eq $true))
{
    Write-Host "Building OpenCV"
    if ($ForceRebuild)
    {
        BuildOpenCV -ForceRebuild -Succeeded ([ref]$OpenCVSucceeded)
    }
    else
    {
        BuildOpenCV -Succeeded ([ref]$OpenCVSucceeded)
    }
    Write-Host "`nOpenCV Build Succeeded: $OpenCVSucceeded`n"
}

$success = ($BlackMagicResult -eq $true) -And ($OpenCVSucceeded -eq $true) -And ($ElgatoResult -eq $true)
Write-Host "`nNative Project Setup Suceeded: $success`n"
if ($Succeeded)
{
    $Succeeded.Value = $success
}

if ($success -eq $true)
{
    exit 0;
}
else
{
    exit 1;
}