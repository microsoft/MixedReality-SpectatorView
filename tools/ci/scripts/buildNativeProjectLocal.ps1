param(
    $MSBuild,
    [switch]$ForceRebuild,
    [Parameter(Mandatory=$false)][ref]$Succeeded
)

# Setup Build Tools, change this for your local environment
if (!$MSBuild)
{
    $MSBuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\amd64\MSBuild.exe"
}

. $PSScriptRoot\buildNativeProjectShared.ps1
. $PSScriptRoot\genericHelpers.ps1
. $PSScriptRoot\spectatorViewHelpers.ps1

$SetupResult = "False"
$ARMResult = "False"
$86Result = "False"
$64Result = "False"
$CopyResult = "False"

if ($ForceRebuild)
{
    SetupVSProjects -ForceRebuild -Succeeded ([ref]$SetupResult)
}
else
{
    SetupVSProjects -Succeeded ([ref]$SetupResult)
}

if ($SetupResult)
{
    BuildProject -MSBuild $MSBuild -VSSolution $PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Native.sln -Configuration "Release" -Platform "ARM" -Succeeded ([ref]$ARMResult)
    BuildProject -MSBuild $MSBuild -VSSolution $PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Native.sln -Configuration "Release" -Platform "x86" -Succeeded ([ref]$86Result)
    BuildProject -MSBuild $MSBuild -VSSolution $PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Native.sln -Configuration "Release" -Platform "x64" -Succeeded ([ref]$64Result)
    
    if ($86Result -And $64Result -And $ARMResult)
    {
        Write-Host "`n`nCopying native plugins to SpectatorView.Unity"
        & $PSScriptRoot\..\..\Scripts\CopyPluginsToUnity.bat
        $CopyResult = $?
    }

    $SetupResult = "True"
}

Write-Host "`n`nSpectatorView.Native Build Results:"
Write-Host "    Setup Succeeded: $SetupResult"
Write-Host "    x86 Build Succeeded: $86Result"
Write-Host "    x64 Build Succeeded: $64Result"
Write-Host "    ARM Build Succeeded: $ARMResult"
Write-Host "    Copy Native Plugins Succeeded: $CopyResult"

$success = $SetupResult -And $86Result -And $64Result -And $ARMResult -And $CopyResult
if ($Succeeded)
{
   $Succeeded.Value = $success
}
exit $success