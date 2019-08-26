param(
    $UnityPath,
    [Parameter(Mandatory=$false)][ref]$Succeeded
)

. $PSScriptRoot\buildUnityProjectShared.ps1
. $PSScriptRoot\spectatorViewHelpers.ps1

# NOTE - For this script to succeed, you need to have no preprocessor directives declared in the SpectatorView.Examples.Unity project prior to building.
# The below logic will configure things correctly, but builds will fail if user defined preprocessor directives conflict with files included in the project when launching to build.

# Setup Build Tools, change this for your local environment
if (!$UnityPath)
{
   $UnityPath = "C:\Program Files\Unity\Hub\Editor\2018.3.14f1\Editor\Unity.exe"
}

$projPath = "$PSScriptRoot\..\..\..\samples\SpectatorView.Example.Unity"
$toolsAssets = "$PSScriptRoot\..\src\BuildTools.Unity\Assets"
$BuildArtifactStagingDirectory = Get-Location
$UnityCacheServerAddress = "127.0.0.1"
If (!(Test-Path $projPath) -OR !(Test-Path $toolsAssets) -OR !(Test-Path $UnityPath))
{
   Write-Error "`nSetup script not updated for local environment.`nProject Path: $projPath`nBuild Tools Assets Path: $toolsAssets`nEditor: $UnityPath"
   exit
}

SetupRepository

# Make sure the tools are included in your local project
SetupToolsPath -ProjectPath $projPath -ToolsAssetsPath $toolsAssets

$result = "true"
Write-Host "`n`n############################ Android ############################"
Write-Host "Running Android Builds"
IncludeAndroidAssets -ProjectPath $projPath
HideIOSAssets -ProjectPath $projPath

# Android, IL2CPP, No ASA
$Platform = "Android"
$Arch = "ARM"
$ScriptingBackend = "IL2CPP"
$sceneList = "Assets\MixedReality-SpectatorView\SpectatorView\Scenes\SpectatorView.Android.unity"
$unityArgs = ""
$define = ""
HideASAAssets -ProjectPath $projPath
BuildUnityProject -OutDirExt "NO_ASA" -ProjectPath $projPath -SceneList $sceneList -EditorPath $UnityPath -Platform $Platform -Arch $Arch -ScriptingBackend $ScriptingBackend -UnityArgs $unityArgs -Define $define -BuildArtifactStagingDirectory $BuildArtifactStagingDirectory -UnityCacheServerAddress $UnityCacheServerAddress -Succeeded ([ref]$result)
$successAndroidIL2CPPNoAsa = $result

# Android, IL2CPP, ASA
$Platform = "Android"
$Arch = "ARM"
$ScriptingBackend = "IL2CPP"
$sceneList = "Assets\MixedReality-SpectatorView\SpectatorView\Scenes\SpectatorView.Android.unity"
$unityArgs = ""
$define = "SPATIALALIGNMENT_ASA"
IncludeASAAssets -ProjectPath $projPath
BuildUnityProject -OutDirExt "ASA" -ProjectPath $projPath -SceneList $sceneList -EditorPath $UnityPath -Platform $Platform -Arch $Arch -ScriptingBackend $ScriptingBackend -UnityArgs $unityArgs -Define $define -BuildArtifactStagingDirectory $BuildArtifactStagingDirectory -UnityCacheServerAddress $UnityCacheServerAddress -Succeeded ([ref]$result)
$successAndroidIL2CPPAsa = $result

Write-Host "`n`n############################ iOS ############################"
Write-Host "Running iOS Builds"
HideAndroidAssets -ProjectPath $projPath
IncludeIOSAssets -ProjectPath $projPath

# iOS, IL2CPP, No ASA
$Platform = "iOS"
$Arch = "ARM"
$ScriptingBackend = "IL2CPP"
$sceneList = "Assets\MixedReality-SpectatorView\SpectatorView\Scenes\SpectatorView.iOS.unity"
$unityArgs = ""
$define = ""
HideASAAssets -ProjectPath $projPath
ConfirmEditorCompiles -OutDirExt "NO_ASA" -ProjectPath $projPath -SceneList $sceneList -EditorPath $UnityPath -Platform $Platform -Arch $Arch -ScriptingBackend $ScriptingBackend -UnityArgs $unityArgs -Define $define -BuildArtifactStagingDirectory $BuildArtifactStagingDirectory -UnityCacheServerAddress $UnityCacheServerAddress -Succeeded ([ref]$result)
$successiOSIL2CPPNoAsa = $result

# iOS, IL2CPP, ASA
$Platform = "iOS"
$Arch = "ARM"
$ScriptingBackend = "IL2CPP"
$sceneList = "Assets\MixedReality-SpectatorView\SpectatorView\Scenes\SpectatorView.iOS.unity"
$unityArgs = ""
$define = "SPATIALALIGNMENT_ASA"
IncludeASAAssets -ProjectPath $projPath
ConfirmEditorCompiles -OutDirExt "ASA" -ProjectPath $projPath -SceneList $sceneList -EditorPath $UnityPath -Platform $Platform -Arch $Arch -ScriptingBackend $ScriptingBackend -UnityArgs $unityArgs -Define $define -BuildArtifactStagingDirectory $BuildArtifactStagingDirectory -UnityCacheServerAddress $UnityCacheServerAddress -Succeeded ([ref]$result)
$successiOSIL2CPPAsa = $result

Write-Host "`n`n############################ UWP ############################"
Write-Host "Running UWP Builds"
HideAndroidAssets -ProjectPath $projPath
HideIOSAssets -ProjectPath $projPath

$Platform = "UWP"
$Arch = "ARM"
$ScriptingBackend = "IL2CPP"
$sceneList = "Assets\MixedReality-SpectatorView\SpectatorView\Scenes\SpectatorView.HoloLens.unity"
$unityArgs = ""
$define = ""
HideASAAssets -ProjectPath $projPath
HideQRCodePlugin -ProjectPath $projPath
BuildUnityProject -OutDirExt "NONE" -ProjectPath $projPath -SceneList $sceneList -EditorPath $UnityPath -Platform $Platform -Arch $Arch -ScriptingBackend $ScriptingBackend -UnityArgs $unityArgs -Define $define -BuildArtifactStagingDirectory $BuildArtifactStagingDirectory -UnityCacheServerAddress $UnityCacheServerAddress -Succeeded ([ref]$result)
$sucessUWPIL2CPP = $result

$Platform = "UWP"
$Arch = "ARM"
$ScriptingBackend = "IL2CPP"
$sceneList = "Assets\MixedReality-SpectatorView\SpectatorView\Scenes\SpectatorView.HoloLens.unity"
$unityArgs = ""
$define = "QRCODESTRACKER_BINARY_AVAILABLE;SPATIALALIGNMENT_ASA"
IncludeASAAssets -ProjectPath $projPath
IncludeQRCodePlugin -ProjectPath $projPath
BuildUnityProject -OutDirExt "ASA_QR" -ProjectPath $projPath -SceneList $sceneList -EditorPath $UnityPath -Platform $Platform -Arch $Arch -ScriptingBackend $ScriptingBackend -UnityArgs $unityArgs -Define $define -BuildArtifactStagingDirectory $BuildArtifactStagingDirectory -UnityCacheServerAddress $UnityCacheServerAddress -Succeeded ([ref]$result)
$sucessUWPIL2CPPASAQR = $result

Write-Host "`n`n############################ Results ############################"
Write-Host "`n`nAndroid Build Results:"
Write-Host "IL2CPP, No ASA Succeeded:   $successAndroidIL2CPPNoAsa"
Write-Host "IL2CPP, ASA Succeeded:      $successAndroidIL2CPPAsa"
Write-Host "`n`iOS Build Results:"
Write-Host "IL2CPP, No ASA Succeeded:   $successiOSIL2CPPNoAsa"
Write-Host "IL2CPP, ASA Succeeded:      $successiOSIL2CPPAsa"
Write-Host "`n`UWP Build Results:"
Write-Host "IL2CPP Succeeded:           $sucessUWPIL2CPP"
Write-Host "IL2CPP, ASA & QR Succeeded: $sucessUWPIL2CPPASAQR`n`n"
Write-Host "#################################################################"

IncludeAndroidAssets -ProjectPath $projPath
IncludeIOSAssets -ProjectPath $projPath
IncludeASAAssets -ProjectPath $projPath
IncludeQRCodePlugin -ProjectPath $projPath

$success = $successAndroidIL2CPPNoAsa -And $successAndroidIL2CPPAsa -And $successiOSIL2CPPNoAsa -And $successiOSIL2CPPAsa -And $sucessUWPIL2CPP -And $sucessUWPIL2CPPASAQR
if ($Succeeded)
{
   $Succeeded.Value = $success
}
exit $success