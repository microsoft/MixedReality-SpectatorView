param(
    $MSBuild,
    [switch]$HardCopySymbolicLinks
)

Write-Host "Setting up the repository..."
Import-Module "$PSScriptRoot\SetupRepositoryFunc.psm1"
$setupAndBuildSucceeded = $true
if ($MSBuild -And $HardCopySymbolicLinks)
{
    SetupRepository -MSBuild $MSBuild -HardCopySymbolicLinks -Succeeded ([ref]$setupAndBuildSucceeded)
}
elseif ($MSBuild)
{
    SetupRepository -MSBuild $MSBuild -Succeeded ([ref]$setupAndBuildSucceeded)
}
elseif ($HardCopySymbolicLinks)
{
    SetupRepository -HardCopySymbolicLinks -Succeeded ([ref]$setupAndBuildSucceeded)
}
else
{
    SetupRepository -Succeeded ([ref]$setupAndBuildSucceeded)
}

if ($setupAndBuildSucceeded -eq $false)
{
    Write-Error "Failed to create Unity Package, SetupRepository failed."
    Write-Output "A combination of the following command arguments may be able to unblock:`nCreateUnityPackage.ps1 -MSBuild C:\Path\To\Your\MSBuild.exe`nCreateUnityPackage.ps1 -HardCopySymbolicLinks`n"
    exit -1
}

$PackageProps = Get-Content "$PSScriptRoot\..\..\src\SpectatorView.Unity\Assets\package.json" | ConvertFrom-Json
$PackageName = $PackageProps.name
$PackageVersion = $PackageProps.version
$PackagePath = "$PSScriptRoot\..\..\packages\$PackageName.$PackageVersion"

Write-Host "`nCreating package $PackageName.$PackageVersion"
if (Test-Path -Path "$PSScriptRoot\..\..\packages\$PackageName.$PackageVersion")
{
    Remove-Item -Path "$PackagePath\*" -Recurse
}
else
{
    New-Item -Path "$PackagePath" -ItemType "directory"
}

Copy-Item -Path "$PSScriptRoot\..\..\src\SpectatorView.Unity\Assets\*" -Destination $PackagePath -Recurse

Write-Host "Created package: packages/$PackageName.$PackageVersion"