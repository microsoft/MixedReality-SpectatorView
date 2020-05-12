# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See LICENSE in the project root for license information.

Import-Module $PSScriptRoot\..\..\Scripts\SetupRepositoryFunc.psm1
Import-Module $PSScriptRoot\..\..\Scripts\ExternalDependencyHelpers.psm1
Import-Module $PSScriptRoot\genericHelpers.psm1

function BuildOpenCV
{
  param
  (
    [switch]$ForceRebuild,
    [Parameter(Mandatory=$true)][ref]$Succeeded
  )

  $origLoc = Get-Location

  if (!(Test-Path "$PSScriptRoot\..\..\..\external\vcpkg"))
  {
    Write-Host "Creating vcpkg submodule"
    AddSubmodule -Repo https://github.com/microsoft/vcpkg.git -DirectoryName vcpkg -Branch master 
    Set-Location $origLoc
  }

  Set-Location "$PSScriptRoot\..\..\..\external\vcpkg"
  Write-Host "Updaing vcpkg submodule to master branch"
  git pull origin master

  if (($ForceRebuild) -And (Test-Path "installed"))
  {
    Write-Host "Removing 'installed' directory to force rebuild"
    Remove-Item -Path "installed" -Force -Recurse
  }

  Write-Host "Preparing vcpkg"
  & .\bootstrap-vcpkg.bat
  Write-Host "Setting vcpkg installs to be available to MSBuild"
  & .\vcpkg integrate install
  Write-Host "Updating vcpkg"
  & .\vcpkg update
  Write-Host "Upgrading vcpkg"
  & .\vcpkg upgrade --no-dry-run

  if (!(Test-Path "installed\x86-windows\lib\libprotobuf.lib"))
  {
    Write-Host "Building OpenCV dependencies"
    & .\vcpkg install protobuf:x86-windows --recurse
    $86Dependencies = $?
  }
  else
  {
    Write-Host "Found install directory for x86 protobuf windows, skipping build"
    $86Dependencies = "True"
  }

  if (!(Test-Path "installed\x86-uwp\lib\opencv_aruco.lib"))
  {
    Write-Host "Building OpenCV x86 UWP"
    & .\vcpkg install opencv[contrib]:x86-uwp --recurse
    $86UWP = $?
  }
  else
  {
    Write-Host "Found install directory for x86 OpenCV UWP, skipping build"
    $86UWP = "True"
  }

  if (!(Test-Path "installed\x64-windows\lib\opencv_aruco.lib"))
  {
    Write-Host "Building OpenCV x64 Windows"
    & .\vcpkg install opencv[contrib]:x64-windows --recurse
    $64Windows = $?
  }
  else
  {
    Write-Host "Found install directory for x64 OpenCV windows, skipping build"
    $64Windows = "True"
  }

  Write-Host "`n`nOpenCV Build Completed:"
  Write-Host "x86 Dependencies Succeeded: $86Dependencies"
  Write-Host "x86 UWP OpenCV Succeeded: $86UWP"
  Write-Host "x64 Desktop OpenCV Succeeded: $64Windows"
  $Succeeded.Value = ($86Dependencies -eq $true) -And ($86UWP -eq $true) -And ($64Windows -eq $true)

  Set-Location $origLoc
}

function AddElgatoSubmodule
{
  AddSubmodule -Repo https://github.com/elgatosf/gamecapture.git -DirectoryName gamecapture -Branch master
}

function SetupExternalDownloads
{
  param
  (
    [switch]$NoDownloads,
    [Parameter(Mandatory=$true)][ref]$Succeeded
  )

  $success = $false;
  if ($NoDownloads)
  {
    SetupDependencyRepoBuildCI -Succeeded ([ref]$success)
  }
  else
  {
    SetupDependencyRepoBuildLocal -Succeeded ([ref]$success)
  }

  $Succeeded.Value = $success
}

function SetupDependencyRepoBuildLocal
{
  param(
    [Parameter(Mandatory=$true)][ref]$Succeeded
)

  DownloadQRCodePlugin

  $BlackMagicPath = "$PSScriptRoot\..\..\..\external\Blackmagic DeckLink SDK 10.9.11"
  if (!(Test-Path $BlackMagicPath))
  {
    Write-Warning -message "Blackmagic decklink resources weren't found at $BlackMagicPath, Blackmagic decklink filming won't be included in the build."
  }

  $AzureSDKPath = "C:\Program Files\Azure Kinect SDK v1.3.0"
  if (!(Test-Path $AzureSDKPath))
  {
    Write-Warning -message "Azure Kinect SDK wasn't found at $AzureSDKPath, Azure Kinect filming won't be included in the build."
  }
  else
  {
    New-Item -ItemType Directory -Force -Path "$PSScriptRoot\..\..\..\external\Azure Kinect SDK v1.3.0"
    Copy-Item -Recurse -Path "C:\Program Files\Azure Kinect SDK v1.3.0\*" -Destination "$PSScriptRoot\..\..\..\external\Azure Kinect SDK v1.3.0" -Force
  }

  $AzureBodyTrackingSDKExpectedPath = "C:\Program Files\Azure Kinect Body Tracking SDK"
  if (!(Test-Path $AzureBodyTrackingSDKExpectedPath))
  {
    Write-Warning -message "Azure Kinect Body Tracking SDK wasn't found at $AzureBodyTrackingSDKExpectedPath, Azure Kinect Body Tracking filming  won't be included in the build."
  }
  else
  {
    New-Item -ItemType Directory -Force -Path "$PSScriptRoot\..\..\..\external\Azure Kinect Body Tracking SDK"
    Copy-Item -Recurse -Path "$AzureBodyTrackingSDKExpectedPath\*" -Destination "$PSScriptRoot\..\..\..\external\Azure Kinect Body Tracking SDK" -Force 
  }

  $Succeeded.Value = $?
}

function SetupDependencyRepoBuildCI
{
  param(
    [Parameter(Mandatory=$true)][ref]$Succeeded
)

  $QRCodePath = "$PSScriptRoot\..\..\..\external\dependencies\MixedReality-QRCodePlugin\"
  if (Test-Path $QRCodePath)
  {
    Copy-Item -Recurse -Path "$QRCodePath\*" -Destination "$PSScriptRoot\..\..\..\external\MixedReality-QRCodePlugin" -Force
  }
  else
  {
    Write-Host "QR Code Dependencies not found, QR Code detection excluded from the build."
  }

  $AzureSDKPath = "$PSScriptRoot\..\..\..\external\dependencies\Azure Kinect SDK v1.3.0\"
  if (Test-Path $AzureSDKPath)
  {
      Copy-Item -Recurse -Path "$AzureSDKPath\*" -Destination "$PSScriptRoot\..\..\..\external\Azure Kinect SDK v1.3.0" -Force
  }
  else
  {
    Write-Host "Azure kinect sdk not found. Azure kinect sdk excluded from the build."
  }

  $AzureBodyTrackingPath = "$PSScriptRoot\..\..\..\external\dependencies\Azure Kinect Body Tracking SDK 1.0.0\"
  if (Test-Path $AzureBodyTrackingPath)
  {
    Copy-Item -Recurse -Path "$AzureBodyTrackingPath\*" -Destination "$PSScriptRoot\..\..\..\external\Azure Kinect Body Tracking SDK 1.0.0" -Force
  }
  else
  {
    Write-Host "Azure kinect body tracking sdk not found. Azure kinect body tracking sdk excluded from the build."
  }

  if (Test-Path "$PSScriptRoot\dependencies.props")
  {
    Copy-Item -Recurse -Path "$PSScriptRoot\dependencies.props" -Destination "$PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Compositor\dependencies.props" -Force
  }
  else
  {
    Write-Host "dependencies.props not found in ci script directory. Build may fail."
    $Succeeded.Value = $false
  }

  $Succeeded.Value = $?
}