# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See LICENSE in the project root for license information.

param(
    [switch]$ForceRebuild,
    [switch]$NoDownloads,
    [Parameter(Mandatory=$false)][ref]$Succeeded
)

Import-Module $PSScriptRoot\spectatorViewHelpers.psm1

$ExternalSetupResult = "False"
if ($NoDownloads)
{
    SetupExternalDownloads -NoDownloads -Succeeded ([ref]$ExternalSetupResult)
}
else
{
    SetupExternalDownloads -Succeeded ([ref]$ExternalSetupResult)
}
Write-Host "`nSetting up external dependencies Succeeded: $ExternalSetupResult`n"

$ElgatoResult = "False"
if ($ExternalSetupResult -eq $true)
{
    Write-Host "Setting up elgato dependencies"
    AddElgatoSubmodule
    $ElgatoResult = $?
    Write-Host "`nAdding elgato dependencies succeeded: $ElgatoResult`n"
}

$OpenCVSucceeded = $true
# if (($ExternalSetupResult -eq $true) -And ($ElgatoResult -eq $true))
# {
#     Write-Host "Building OpenCV"
#     if ($ForceRebuild)
#     {
#         BuildOpenCV -ForceRebuild -Succeeded ([ref]$OpenCVSucceeded)
#     }
#     else
#     {
#         BuildOpenCV -Succeeded ([ref]$OpenCVSucceeded)
#     }
#     Write-Host "`nOpenCV Build Succeeded: $OpenCVSucceeded`n"
# }

$success = ($ExternalSetupResult -eq $true) -And ($OpenCVSucceeded -eq $true) -And ($ElgatoResult -eq $true)
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