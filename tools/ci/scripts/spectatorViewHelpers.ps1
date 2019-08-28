. $PSScriptRoot\..\..\Scripts\SetupRepositoryFunc.ps1
. $PSScriptRoot\..\..\Scripts\ExternalDependencyHelpers.ps1
. $PSScriptRoot\genericHelpers.ps1


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

  if (!(Test-Path vcpkg.exe))
  {
    Write-Host "Preparing vcpkg"
    & .\bootstrap-vcpkg.bat
  }

  Write-Host "Setting vcpkg installs to be available to MSBuild"
  & .\vcpkg integrate install
  Write-Host "Updating vcpkg"
  & .\vcpkg update
  Write-Host "Upgrading vcpkg"
  & .\vcpkg upgrade --no-dry-run

  if (!(Test-Path "installed\x86-windows"))
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

  if (!(Test-Path "installed\x86-uwp"))
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

  if (!(Test-Path "installed\x64-windows"))
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
  Write-Host "x86 Desktop OpenCV Succeeded: $64Windows"
  $Succeeded.Value = ($86Dependencies -eq $true) -And ($86UWP -eq $true) -And ($64Windows -eq $true)

  Set-Location $origLoc
}

function AddElgatoSubmodule
{
  AddSubmodule -Repo https://github.com/elgatosf/gamecapture.git -DirectoryName gamecapture -Branch master
}

function HideAndroidAssets
{
   param
   (
     $ProjectPath
   )

   HideUnityAssetsDirectory -Path "$ProjectPath\Assets\GoogleARCore"
}

function IncludeAndroidAssets
{
   param
   (
     $ProjectPath
   )

   IncludeUnityAssetsDirectory -Path "$ProjectPath\Assets\.GoogleARCore"
}

function HideIOSAssets
{
   param
   (
     $ProjectPath
   )

   HideUnityAssetsDirectory -Path "$ProjectPath\Assets\ARKit-Unity-Plugin"
}

function IncludeIOSAssets
{
   param
   (
     $ProjectPath
   )

   IncludeUnityAssetsDirectory -Path "$ProjectPath\Assets\.ARKit-Unity-Plugin"
}

function HideASAAssets
{
   param
   (
     $ProjectPath
   )

   HideUnityAssetsDirectory -Path "$ProjectPath\Assets\AzureSpatialAnchorsPlugin"
   HideUnityAssetsDirectory -Path "$ProjectPath\Assets\AzureSpatialAnchors.Resources"
}

function IncludeASAAssets
{
   param
   (
     $ProjectPath
   )

   IncludeUnityAssetsDirectory -Path "$ProjectPath\Assets\.AzureSpatialAnchorsPlugin"
   IncludeUnityAssetsDirectory -Path "$ProjectPath\Assets\.AzureSpatialAnchors.Resources"
}

function HideQRCodePlugin
{
   param
   (
     $ProjectPath
   )

   HideUnityAssetsDirectory -Path "$ProjectPath\Assets\MixedReality-QRCodePlugin"
}

function IncludeQRCodePlugin
{
   param
   (
     $ProjectPath
   )

   IncludeUnityAssetsDirectory -Path "$ProjectPath\Assets\.MixedReality-QRCodePlugin"
}

function SetupExternalDownloads
{
  param
  (
    [switch]$NoDownloads,
    [Parameter(Mandatory=$true)][ref]$Succeeded
  )

  $success = "False";
  if ($NoDownloads)
  {
    # This script set up dependencies for Blackmagic Design dependencies
    # The behavior can be manually duplicated by populating an 'external\dependencies\BlackmagicDesign\Blackmagic DeckLink SDK 10.9.11' directory in your repo
    # and by populating the external\MixedReality-QRCodePlugin and external\ARKit-Unity-Plugin directories
    SetupDependencyRepoBuildCI -Succeeded ([ref]$success)
    Write-Host "BlackmagicDesign dependencies found: $success"
  }
  else
  {
    DownloadQRCodePlugin
    DownloadARKitPlugin
    $success = Test-Path "$PSScriptRoot\..\..\..\external\dependencies\BlackmagicDesign\Blackmagic DeckLink SDK 10.9.11"
    Write-Host "BlackmagicDesign dependencies found: $success"
    if ($success -eq $false)
    {
      Write-Host "Native build will fail based on a missing BlackmagicDesign dependency."
      Write-Host "To fix this issue, obtain Blackmagic DeckLink SDK 10.9.11 and add it to an 'external\dependencies\BlackmagicDesign\Blackmagic DeckLink SDK 10.9.11' directory."
    }
  }

  $Succeeded.Value = $success
}

function SetupDependencyRepoBuildCI
{
  param(
    [Parameter(Mandatory=$true)][ref]$Succeeded
)

  Copy-Item -Path "$PSScriptRoot\..\..\..\external\dependencies\ARKit-Unity-Plugin\Unity-Technologies-unity-arkit-plugin-94e47eae5954\*" -Destination "$PSScriptRoot\..\..\..\external\ARKit-Unity-Plugin" -Force
  Copy-Item -Path "$PSScriptRoot\..\..\..\external\dependencies\MixedReality-QRCodePlugin\*" -Destination "$PSScriptRoot\..\..\..\external\MixedReality-QRCodePlugin" -Force
  Copy-Item -Path "$PSScriptRoot\dependencies.props" -Destination "$PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Compositor\dependencies.props" -Force
  $Succeeded.Value = $?
}