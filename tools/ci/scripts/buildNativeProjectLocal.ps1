param(
    $MSBuild,
    [switch]$ForceRebuild,
    [switch]$ExcludeBlackmagic,
    [Parameter(Mandatory=$false)][ref]$Succeeded
)

# Setup Build Tools, change this for your local environment
if ((!$MSBuild) -And (Test-Path -Path "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\amd64\MSBuild.exe"))
{
    $MSBuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\amd64\MSBuild.exe"
}
elseif (!$MSBuild)
{
    Write-Error Unable to locate MSBuild.exe
    exit 1;
}

if (!(Get-Command "nuget"))
{
    Write-Error "NuGet.exe does not seem to be installed as a command on this computer."
    exit 1;
}

Import-Module $PSScriptRoot\genericHelpers.psm1
Import-Module $PSScriptRoot\spectatorViewHelpers.psm1

$SetupResult = "False"
$ARMResult = "False"
$86Result = "False"
$64Result = "False"
$CopyResult = "False"

$Arg1 = "-None"
if ($ForceRebuild -And $ExcludeBlackmagic)
{
    . $PSScriptRoot\setupNativeProject.ps1 -ForceRebuild -ExcludeBlackmagic -Succeeded ([ref]$SetupResult)
}
elseif ($ForceRebuild)
{
    . $PSScriptRoot\setupNativeProject.ps1 -ForceRebuild -Succeeded ([ref]$SetupResult)
}
elseif ($ExcludeBlackmagic)
{
    . $PSScriptRoot\setupNativeProject.ps1 -ExcludeBlackmagic -Succeeded ([ref]$SetupResult)
}
else
{
    . $PSScriptRoot\setupNativeProject.ps1 -Succeeded ([ref]$SetupResult)
}

if ($SetupResult -eq $true)
{
    Write-Host "Attempting to restore NuGet packages for SpectatorView.Native.sln"
    & nuget restore $PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Native.sln
    
    BuildProject -MSBuild $MSBuild -VSSolution $PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Native.sln -Configuration "Release" -Platform "ARM" -Succeeded ([ref]$ARMResult)
    
    $86Includes = "`"$PSScriptRoot\..\..\..\external\vcpkg\installed\x86-uwp\include`""
    $86LibDirs = "`"$PSScriptRoot\..\..\..\external\vcpkg\installed\x86-uwp\lib`""
    $86Libs = "`"$PSScriptRoot\..\..\..\external\vcpkg\installed\x86-uwp\lib\*.lib`""
    BuildProject -MSBuild $MSBuild -VSSolution $PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Native.sln -Configuration "Release" -Platform "x86" -Includes $86Includes -LibDirs $86LibDirs -Libs $86Libs -Succeeded ([ref]$86Result)
    # Building in visual studio automatically copies these binaries
    Copy-Item $PSScriptRoot\..\..\..\external\vcpkg\installed\x86-uwp\bin\* $PSScriptRoot\..\..\..\src\SpectatorView.Native\Release\SpectatorView.OpenCV.UWP\ -Force

    Copy-Item $PSScriptRoot\dependencies.props $PSScriptRoot\..\..\..\src\SpectatorView.Native\SpectatorView.Compositor\dependencies.props

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

    $SetupResult = "True"
}

Write-Host "`n`nSpectatorView.Native Build Results:"
Write-Host "    Setup Succeeded: $SetupResult"
Write-Host "    x86 Build Succeeded: $86Result"
Write-Host "    x64 Build Succeeded: $64Result"
Write-Host "    ARM Build Succeeded: $ARMResult"
Write-Host "    Copy Native Plugins Succeeded: $CopyResult"

$success = ($SetupResult -eq $true) -And ($86Result -eq $true) -And ($64Result -eq $true) -And ($ARMResult -eq $true) -And ($CopyResult -eq $true)
if ($Succeeded)
{
   $Succeeded.Value = $success
}
exit $success