. $PSScriptRoot\..\..\Scripts\SetupRepositoryFunc.ps1
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
    Set-Location $PSScriptRoot
    git submodule add https://github.com/microsoft/vcpkg.git "../../../external/vcpkg"
    Set-Location $origLoc
  }

  Set-Location "$PSScriptRoot\..\..\..\external\vcpkg"
  Write-Host "Updaing vcpkg submodule to master branch"
  git pull origin master
  & .\vcpkg update
  & .\vcpkg upgrade --no-dry-run

  if (($ForceRebuild) -And (Test-Path "installed"))
  {
    Write-Host "Removing 'installed' directory to force rebuild"
    Remove-Item -Path "installed" -Force -Recurse
  }

  if (!(Test-Path "installed"))
  {
    Write-Host "Preparing vcpkg"
    & .\bootstrap-vcpkg.bat
    Write-Host "Setting vcpkg installs to be available to MSBuild"
    & .\vcpkg integrate install
    Write-Host "Building OpenCV dependencies"
    & .\vcpkg install protobuf:x86-windows --recurse
    $86Dependencies = $?
    Write-Host "Building OpenCV x86 UWP"
    & .\vcpkg install opencv[contrib]:x86-uwp --recurse
    $86UWP = $?
    Write-Host "Building OpenCV x64 Windows"
    & .\vcpkg install opencv[contrib]:x64-windows --recurse
    $64Windows = $?

    Write-Host "`n`nOpenCV Build Completed:"
    Write-Host "x86 Dependencies Succeeded: $86Dependencies"
    Write-Host "x86 UWP OpenCV Succeeded: $86UWP"
    Write-Host "x86 Desktop OpenCV Succeeded: $64Windows"
    $Succeeded.Value = $86Dependencies -And $86UWP -And $64Windows
  }
  else
  {
    $Succeeded.Value = "True"  
  }

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