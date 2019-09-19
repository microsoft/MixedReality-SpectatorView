# TODO - create config file that contains these properties
$MSBuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\amd64\MSBuild.exe"
$UnityPath = "C:\Program Files\Unity\Hub\Editor\2018.3.14f1\Editor\Unity.exe"

$NativeBuildResult = "False"
. $PSScriptRoot\buildNativeProjectLocal.ps1 -MSBuild $MSBuild -Succeeded ([ref]$NativeBuildResult)

if ($NativeBuildResult)
{
    $UnityBuildResult = "False"
    . $PSScriptRoot\buildUnityProjectLocal.ps1 -UnityPath $UnityPath -Succeeded ([ref]$UnityBuildResult)
    if (!$UnityBuildResult)
    {
        Write-Host "`nUnity project build failed."
        exit $UnityBuildResult
    }
}
else
{
    Write-Host "`nNative plugin build failed."
    exit $NativeBuildResult;    
}

exit 0;
