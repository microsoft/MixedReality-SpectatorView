param(
    $MSBuild
)

Write-Host "Setting up the repository..."
Import-Module "$PSScriptRoot\SetupRepositoryFunc.psm1"
$setupAndBuildSucceeded = $false
if ($MSBuild)
{
    SetupRepository -MSBuild $MSBuild -Succeeded ([ref]$setupAndBuildSucceeded)
}
else
{
    SetupRepository -Succeeded ([ref]$setupAndBuildSucceeded)
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
Compress-Archive -path $PackagePath -destinationpath "$PackagePath.zip" -Force

Write-Host "Created package: packages/$PackageName.$PackageVersion.zip"
Write-Host "Unzip and reference this package for your project as needed."