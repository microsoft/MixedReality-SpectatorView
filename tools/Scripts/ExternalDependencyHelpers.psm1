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
  [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
  $wc = New-Object System.Net.WebClient
  $wc.DownloadFile($url, $nugetFile)
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
}
