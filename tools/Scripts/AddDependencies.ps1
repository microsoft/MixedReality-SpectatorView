$projPath = $Args[0]
$projPathValid = Test-Path $projPath
$repoPath = $Args[1]
$repoPathValid = Test-Path $repoPath

If ( -not $projPathValid)
{
	Write-Error "Invalid Unity project path specified: $projPath"
	Write-Host "`n"
	Write-Host "Example Usage: AddDependencies.bat C:\Your\Unity\Project\Assets C:\MixedReality-SpectatorView"
	Write-Host "`n"
}
ElseIf ( -not $repoPathValid)
{
	Write-Error "Invalid MixedReality-SpectatorView repository path specified: $repoPath"
	Write-Host "`n"
	Write-Host "Example Usage: AddDependencies.bat C:\Your\Unity\Project\Assets C:\MixedReality-SpectatorView"
	Write-Host "`n"
}
Else
{
	Write-Host "`n"
	Write-Host Creating symbolic links in the following directory: $projPath
	Write-Host Symbolic links based off of following path: $repoPath
	Write-Host "`n"

	cd $projPath
	cmd /c mklink /D "MixedReality-SpectatorView" "$repoPath\src\SpectatorView.Unity\Assets"
	cmd /c mklink /D "ARKit-Unity-Plugin" "$repoPath\external\ARKit-Unity-Plugin"
	cmd /c mklink /D "AzureSpatialAnchorsPlugin" "$repoPath\external\Azure-Spatial-Anchors-Samples\Unity\Assets\AzureSpatialAnchorsPlugin"
	cmd /c mklink /D "GoogleARCore" "$repoPath\external\GoogleARCore"
	cmd /c mklink /D "MixedReality-QRCodePlugin" "$repoPath\external\MixedReality-QRCodePlugin"
	cmd /c mkdir AzureSpatialAnchors.Resources
	cd AzureSpatialAnchors.Resources
	cmd /c mklink /D "android-logos" "$repoPath\external\Azure-Spatial-Anchors-Samples\Unity\Assets\android-logos"
	cmd /c mklink /D "logos" "$repoPath\external\Azure-Spatial-Anchors-Samples\Unity\Assets\logos"
	Write-Host "`n"
}

Write-Host -NoNewLine 'Press any key to continue...';
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');