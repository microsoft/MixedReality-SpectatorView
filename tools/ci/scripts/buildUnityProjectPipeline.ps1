param(
    [string]$ProjectPath,
    [string]$ToolsAssetsPath,
    [string]$Platform,
    [string]$Arch,
    [string]$ScriptingBackend,
    [string]$SceneList,
    [string]$Define,
    [string]$UnityArgs
)

$repoLocation = Get-Location
$ProjectPath = "$repoLocation\$ProjectPath"
$ToolsAssetsPath = "$repoLocation\$ToolsAssetsPath"

. $PSScriptRoot\buildUnityProjectShared.ps1
. $PSScriptRoot\spectatorViewHelpers.ps1

# Find unity.exe as Start-UnityEditor currently doesn't support arbitrary parameters
$Editor = Get-ChildItem ${Env:$(UnityVersion)} -Filter 'Unity.exe' -Recurse | Select-Object -First 1 -ExpandProperty FullName

SetupDependencies
SetupToolsPath -ProjectPath "$ProjectPath" -ToolsAssetsPath "$ToolsAssetsPath"
$OutDirExt = ""

if ($Platform -eq "Android" )
{
    IncludeAndroidAssets -ProjectPath $ProjectPath
}
else
{
    HideAndroidAssets -ProjectPath $ProjectPath
}

if ($Platform -eq "iOS" )
{
    IncludeIOSAssets -ProjectPath $ProjectPath
}
else
{
    HideIOSAssets -ProjectPath $ProjectPath
}

if ($Define -contains 'SPATIALALIGNMENT_ASA')
{
    IncludeASAAssets -ProjectPath $ProjectPath
    $OutDirExt += "ASA"
}
else
{
    HideASAAssets -ProjectPath $ProjectPath
}

if ($Define -contains 'QRCODESTRACKER_BINARY_AVAILABLE')
{
    IncludeQRCodePlugin -ProjectPath $ProjectPath
    $OutDirExt += "_QR"
}
else
{
    HideQRCodePlugin -ProjectPath $ProjectPath
}

$Result = "False"
if ($Platform -eq "iOS" )
{
    ConfirmEditorCompiles -OutDirExt $OutDirExt -ProjectPath $ProjectPath -SceneList $SceneList -EditorPath $Editor -Platform $Platform -Arch $Arch -ScriptingBackend $ScriptingBackend -UnityArgs $UnityArgs -Define $Define -BuildArtifactStagingDirectory $(Build.ArtifactStagingDirectory) -UnityCacheServerAddress $(Unity.CacheServer.Address) -Succeeded ([ref]$Result)
}
else
{
    BuildUnityProject -OutDirExt $OutDirExt -ProjectPath $ProjectPath -SceneList $SceneList -EditorPath $Editor -Platform $Platform -Arch $Arch -ScriptingBackend $ScriptingBackend -UnityArgs $UnityArgs -Define $Define -BuildArtifactStagingDirectory $(Build.ArtifactStagingDirectory) -UnityCacheServerAddress $(Unity.CacheServer.Address) -Succeeded ([ref]$Result)
}

exit $Result