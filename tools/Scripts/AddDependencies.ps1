$origLoc = Get-Location
$scriptLoc = Split-Path $PSCommandPath -Parent

If (-not $origLoc.equals($scriptLoc))
{
    Set-Location $scriptLoc
}

Set-Location "../../../"
$repoPath = git rev-parse --show-toplevel
Set-Location $origLoc

$projPath = $repoPath + "/" + $Args[0]
$projPathValid = Test-Path $projPath
$assetsFolder = Split-Path $projPath -Leaf
$assetsFolderValid = $assetsFolder.equals("Assets");

$submodulePath = $repoPath + "/" + $Args[1]
$submodulePathValid = Test-Path $submodulePath
$externalPath = $submodulePath + "/external"
$externalPathValid = Test-Path $externalPath

If ( -not $projPathValid -or -not $assetsFolderValid )
{
	Write-Error "Invalid Unity project path specified: $projPath"
    Write-Host "Note, this path should be equal to the submodule location relative to your project's repository root."
	Write-Host "`n"
	Write-Host "Example Usage: AddDependencies.bat Assets sv"
	Write-Host "`n"
}
ElseIf ( -not $submodulePathValid -or -not $externalPathValid )
{
	Write-Error "Invalid MixedReality-SpectatorView submodule path specified: $submodulePath"
    Write-Host "Note, this path should be equal to the submodule location relative to your project's repository root."
	Write-Host "`n"
	Write-Host "Example Usage: AddDependencies.bat Assets sv"
	Write-Host "`n"
}
Else
{
    $pathDepth = ($Args[0].ToCharArray() | Where-Object {$_ -eq '/'} | Measure-Object).Count + 1
    For ($i=0; $i -lt $pathDepth; $i++) 
    {
        $relativePath = $relativePath + "../"
    }
    $relativePath += $Args[1]

	Write-Host "`n"
	Write-Host Creating symbolic links in the following directory: $projPath
    $builtPath = $projPath + "/" + $relativePath
	Write-Host Symbolic links based off of following path: $builtPath
	Write-Host "`n"

    cd $projPath
    $relativePath = $relativePath -replace '/','\'
	cmd /c mklink /D "MixedReality-SpectatorView" "$relativePath\src\SpectatorView.Unity\Assets"
	cmd /c mklink /D "ARKit-Unity-Plugin" "$relativePath\external\ARKit-Unity-Plugin"
	cmd /c mklink /D "AzureSpatialAnchorsPlugin" "$relativePath\external\Azure-Spatial-Anchors-Samples\Unity\Assets\AzureSpatialAnchorsPlugin"
	cmd /c mklink /D "GoogleARCore" "$relativePath\external\ARCore-Unity-SDK\Assets\GoogleARCore"
	cmd /c mklink /D "MixedReality-QRCodePlugin" "$relativePath\external\MixedReality-QRCodePlugin"

	cmd /c mkdir "AzureSpatialAnchors.Resources"    
    cd "AzureSpatialAnchors.Resources"
    $relativePath = "..\" + $relativePath
    Write-Host $relativePath
	cmd /c mklink /D "android-logos" "$relativePath\external\Azure-Spatial-Anchors-Samples\Unity\Assets\android-logos"
	cmd /c mklink /D "logos" "$relativePath\external\Azure-Spatial-Anchors-Samples\Unity\Assets\logos"
	Write-Host "`n"
}

Write-Host -NoNewLine 'Press any key to continue...';
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');
Write-Host "`n"
cd $origLoc