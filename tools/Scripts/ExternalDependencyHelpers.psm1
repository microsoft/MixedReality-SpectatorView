function DownloadQRCodePlugin
{
  $zipFile = "$PSScriptRoot\..\..\external\MixedReality-QRCodePlugin\qrcodeplugin.zip"
  if (!(Test-Path $zipFile))
  {
    Write-Host "Populating QR Code Dependencies"
    $url = "https://github.com/dorreneb/mixed-reality/releases/download/1.1/release.zip"

    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile($url, $zipFile)
    Expand-Archive -Path $zipFile -DestinationPath "$PSScriptRoot\..\..\external\MixedReality-QRCodePlugin" -Force
  }
  else
  {
    Write-Host "external/MixedReality-QRCodePlugin already populated in repo"
  }
}