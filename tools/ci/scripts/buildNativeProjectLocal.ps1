param(
    $MSBuild,
    [switch]$ForceRebuild,
    [ref]$LocalBuildSucceeded
)

$MSBuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe"
# Setup Build Tools, change this for your local environment
if ((!$MSBuild) -And (Test-Path -Path $MSBuildPath))
{
    $MSBuild = $MSBuildPath
}
elseif (!$MSBuild)
{
    Write-Error Unable to locate MSBuild.exe
    Write-Host "You can specify a -MSBuild variable specifying the path for MSBuild.exe if it isn't found at $MSBuildPath"
    $LocalBuildSucceeded.Value = $false
    exit 1
}

if (!(Get-Command "nuget"))
{
    Write-Error "NuGet.exe does not seem to be installed as a command on this computer."
    $LocalBuildSucceeded.Value = $false
    exit 1
}

Import-Module $PSScriptRoot\genericHelpers.psm1
Import-Module $PSScriptRoot\spectatorViewHelpers.psm1

$SetupResult = "False"
$ARMResult = "False"
$ARM64Result = "False"
$86Result = "False"
$64Result = "False"
$CopyResult = "False"

$Arg1 = "-None"
if ($ForceRebuild)
{
    . $PSScriptRoot\setupNativeProject.ps1 -ForceRebuild -Succeeded ([ref]$SetupResult)
}
else
{
    . $PSScriptRoot\setupNativeProject.ps1 -Succeeded ([ref]$SetupResult)
}

if ($SetupResult -eq $true)
{
    Write-Host "Attempting to restore NuGet packages for SpectatorView.Native.sln"
    $msbuildpath = Split-Path $MSBuild -parent
    & nuget restore $PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Native.sln -verbosity detail -MSBuildPath $msbuildpath

    BuildProject -MSBuild $MSBuild -VSSolution $PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Native.sln -Configuration "Release" -Platform "ARM" -Succeeded ([ref]$ARMResult)
    BuildProject -MSBuild $MSBuild -VSSolution $PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Native.sln -Configuration "Release" -Platform "ARM64" -Succeeded ([ref]$ARM64Result)    

    $86Includes = "`"$PSScriptRoot\..\..\..\external\vcpkg\installed\x86-uwp\include`""
    $86LibDirs = "`"$PSScriptRoot\..\..\..\external\vcpkg\installed\x86-uwp\lib`""
    $86Libs = "`"$PSScriptRoot\..\..\..\external\vcpkg\installed\x86-uwp\lib\*.lib`""
    BuildProject -MSBuild $MSBuild -VSSolution $PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Native.sln -Configuration "Release" -Platform "x86" -Includes $86Includes -LibDirs $86LibDirs -Libs $86Libs -Succeeded ([ref]$86Result)
    # Building in visual studio automatically copies these binaries
    Copy-Item $PSScriptRoot\..\..\..\external\vcpkg\installed\x86-uwp\bin\* $PSScriptRoot\..\..\..\src\SpectatorView.Native\Release\SpectatorView.OpenCV.UWP\ -Force

    $64Includes = "`"$PSScriptRoot\..\..\..\external\vcpkg\installed\x64-windows\include`""
    $64LibDirs = "`"$PSScriptRoot\..\..\..\external\vcpkg\installed\x64-windows\lib`""
    $64Libs = "`"$PSScriptRoot\..\..\..\external\vcpkg\installed\x64-windows\lib\*.lib`""
    BuildProject -MSBuild $MSBuild -VSSolution $PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Native.sln -Configuration "Release" -Platform "x64" -Includes $64Includes -LibDirs $64LibDirs -Libs $64Libs -Succeeded ([ref]$64Result)
    # Building in visual studio automatically copies these binaries
    Copy-Item $PSScriptRoot\..\..\..\external\vcpkg\installed\x64-windows\bin\* $PSScriptRoot\..\..\..\src\SpectatorView.Native\x64\Release\ -Force

    if (($86Result -eq $true) -And ($64Result -eq $true) -And ($ARMResult -eq $true))
    {
        Write-Host "`n`nCopying native plugins to SpectatorView.Unity"
        & $PSScriptRoot\..\..\Scripts\CopyPluginsToUnity.bat
        $CopyResult = $?
    }

    $SetupResult = $true
}

Write-Host "`n`nSpectatorView.Native Build Results:"
Write-Host "    Setup Succeeded:               $SetupResult"
Write-Host "    x86 Build Succeeded:           $86Result"
Write-Host "    x64 Build Succeeded:           $64Result"
Write-Host "    ARM Build Succeeded:           $ARMResult"
Write-Host "    ARM64 Build Succeeded:         $ARM64Result"
Write-Host "    Copy Native Plugins Succeeded: $CopyResult"

$DependenciesPropsFile = "$PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Compositor\dependencies.props"

if (Test-Path -Path $DependenciesPropsFile -PathType Leaf) {
  [xml]$DependenciesPropsContent = Get-Content $DependenciesPropsFile

  $SolutionDirVariable = "`$(SolutionDir)"
  $SolutionDir = "$PSScriptRoot\..\..\..\src\SpectatorView.Native\"

  Write-Host "`nIncluded Compositor Components:"
  Write-Host "    Blackmagic Decklink:            " (Test-Path $DependenciesPropsContent.Project.PropertyGroup.DeckLink_inc.replace($SolutionDirVariable, $SolutionDir))
  Write-Host "    Elgato:                         " (Test-Path $DependenciesPropsContent.Project.PropertyGroup.Elgato_Filter.replace($SolutionDirVariable, $SolutionDir))
  Write-Host "    Azure Kinect SDK:               " (Test-Path $DependenciesPropsContent.Project.PropertyGroup.AzureKinectSDK.replace($SolutionDirVariable, $SolutionDir))
  Write-Host "    Azure Kinect Body Tracking SDK: " (Test-Path $DependenciesPropsContent.Project.PropertyGroup.AzureKinectBodyTrackingSDK.replace($SolutionDirVariable, $SolutionDir))
}

$success = ($SetupResult -eq $true) -And ($86Result -eq $true) -And ($64Result -eq $true) -And ($ARMResult -eq $true) -And ($CopyResult -eq $true)
$LocalBuildSucceeded.Value = $success

Write-Host "`nBuild Succeeded:" $LocalBuildSucceeded.Value