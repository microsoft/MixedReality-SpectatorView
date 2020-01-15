param(
	[switch] $Sample,
	$AssetPath,
	$SVPath
)

$origLoc = Get-Location
$scriptLoc = Split-Path $PSCommandPath -Parent

If (-not $origLoc.equals($scriptLoc))
{
    Set-Location $scriptLoc
}

if ($Sample)
{
	Write-Host "Adding dependencies for a spectator view sample project"
	Set-Location "../../"
	$repoPath = Get-Location
}
else
{
	Write-Host "Adding dependencies for the spectator view submodule"
	Set-Location "../../../"
	$repoPath = git rev-parse --show-toplevel
}

Set-Location $origLoc
Write-Host "Repo Path: $repoPath"
Write-Host "Asset Path: $repoPath/$AssetPath"
Write-Host "SV Path Path: $repoPath/$SVPath"

$projPath = "$repoPath/$AssetPath"
$projPathValid = Test-Path $projPath
$assetsFolder = Split-Path $projPath -Leaf
$assetsFolderValid = $assetsFolder.equals("Assets");

$submodulePath = "$repoPath/$SVPath"
$submodulePathValid = Test-Path $submodulePath
$externalPath = $submodulePath + "/external"
$externalPathValid = Test-Path $externalPath

If ( -not $projPathValid -or -not $assetsFolderValid )
{
	Write-Error "Invalid Unity project path specified: $projPath"
    Write-Host "Note, this path should be equal to the unity assets location relative to your project's repository root."
	Write-Host "`n"
	Write-Host "Example Usage: AddDependencies.bat -AssetPath Project\Assets -SVPath external\MixedReality-SpectatorView"
	Write-Host "`n"
}
ElseIf ( -not $submodulePathValid -or -not $externalPathValid )
{
	Write-Error "Invalid MixedReality-SpectatorView submodule path specified: $submodulePath"
    Write-Host "Note, this path should be equal to the submodule location relative to your project's repository root."
	Write-Host "`n"
	Write-Host "Example Usage: AddDependencies.bat -AssetPath Project\Assets -SVPath external\MixedReality-SpectatorView"
	Write-Host "`n"
}
Else
{
	$pathDepth = ($AssetPath.ToCharArray() | Where-Object {$_ -eq '/' -or $_ -eq '\'} | Measure-Object).Count + 1
	Write-Host "Path Depth: $pathDepth"
    For ($i=0; $i -lt $pathDepth; $i++) 
    {
        $relativePath = $relativePath + "../"
    }
    $relativePath += $SVPath

	Write-Host "`n"
	Write-Host Creating symbolic links in the following directory: $projPath
    $builtPath = $projPath + "/" + $relativePath
	Write-Host Symbolic links based off of following path: $builtPath
	Write-Host "`n"

    Set-Location $projPath
    $relativePath = $relativePath -replace '/','\'
	cmd /c mklink /D "MixedReality-SpectatorView" "$relativePath\src\SpectatorView.Unity\Assets"
	cmd /c mklink /D "ARKit-Unity-Plugin" "$relativePath\external\ARKit-Unity-Plugin"
	cmd /c mklink /D "AzureSpatialAnchorsPlugin" "$relativePath\external\Azure-Spatial-Anchors-Samples\Unity\Assets\AzureSpatialAnchorsPlugin"
	cmd /c mklink /D "GoogleARCore" "$relativePath\external\ARCore-Unity-SDK\Assets\GoogleARCore"

	cmd /c mkdir "AzureSpatialAnchors.Resources"
    Set-Location "AzureSpatialAnchors.Resources"
    $resourcesPath = "..\" + $relativePath
    Write-Host $resourcesPath
	cmd /c mklink /D "android-logos" "$resourcesPath\external\Azure-Spatial-Anchors-Samples\Unity\Assets\android-logos"
	cmd /c mklink /D "logos" "$resourcesPath\external\Azure-Spatial-Anchors-Samples\Unity\Assets\logos"
	Write-Host "`n"

	Set-Location "../"
	if (!(Test-Path -Path "Plugins"))
	{
		cmd /c mkdir "Plugins"
	}

	$androidPluginPath = "..\" + $relativePath
    Write-Host $androidPluginPath
	Set-Location "Plugins"
	cmd /c mklink /D "Android" "$androidPluginPath\external\Azure-Spatial-Anchors-Samples\Unity\Assets\Plugins\Android"
	Write-Host "`n"
}

Write-Host -NoNewLine 'Press any key to continue...';
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');
Write-Host "`n"
cd $origLoc