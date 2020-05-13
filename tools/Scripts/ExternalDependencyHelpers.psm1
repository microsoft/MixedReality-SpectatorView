function DownloadNuGetPackage
{ 
  param(
    $PackageName,
    $Version,
    $IntermediateFolder,
    $OutputFolder
  )

  $nugetFile = "$IntermediateFolder\$PackageName.$Version.nupkg"
  $zipFile = "$IntermediateFolder\$PackageName.$Version.zip"
  $zipOutputFolder = "$IntermediateFolder\$PackageName.$Version"

  $url = "https://www.nuget.org/api/v2/package/$PackageName/$Version"
  Invoke-WebRequest -Uri $url -OutFile $nugetFile

  if (Test-Path -Path $nugetFile)
  {
    Copy-Item -Path $nugetFile -Destination $zipFile -Force
    Expand-Archive -Path $zipFile -DestinationPath $zipOutputFolder -Force
  
    if (Test-Path -Path "$zipOutputFolder\Unity")
    {
      New-Item -Path "$OutputFolder\$PackageName.$Version\Unity" -ItemType Directory
      Copy-Item -Path "$zipOutputFolder\Unity\*" -Destination "$OutputFolder\$PackageName.$Version\" -Recurse
    }
  
    if (Test-Path -Path "$zipOutputFolder\lib\unity")
    {
      New-Item -Path "$OutputFolder\$PackageName.$Version\lib\net46" -ItemType Directory
      Copy-Item -Path "$zipOutputFolder\lib\unity\*" -Destination "$OutputFolder\$PackageName.$Version\" -Recurse
    }
  }
  else
  {
    Write-Error "Failed to obtain $nugetFile from $url"
  }
}

function DownloadQRCodePlugin
{
  Write-Output "Downloading QRCode Dependencies for HoloLens"
  $mainFolder = "$PSScriptRoot\..\..\external\MixedReality-QRCodePlugin\"
  $contentFolder = "$mainFolder\UnityFiles\"

  Remove-Item -Path "$mainFolder\*Microsoft.*" -Recurse
  if (Test-Path $contentFolder)
  {
    Remove-Item -Path "$contentFolder" -Recurse
  }

  New-Item -ItemType Directory -Force -Path $contentFolder

  DownloadNuGetPackage -PackageName "Microsoft.MixedReality.QR" -Version "0.5.2100" -IntermediateFolder $mainFolder -OutputFolder "$contentFolder"
  DownloadNuGetPackage -PackageName "Microsoft.VCRTForwarders" -Version "140.1.0.5" -IntermediateFolder $mainFolder -OutputFolder "$contentFolder"

  # TODO - remove this deletion step once qr code nuget packages don't break mac builds
  Write-Host "Removing c# files that break in Unity packages for QRCode Dependencies in directory $contentFolder\*.cs*"
  Remove-Item -Recurse $contentFolder -Include *.cs*
}
