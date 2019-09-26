function DownloadQRCodePlugin
{
  $zipFile = "$PSScriptRoot\..\..\external\MixedReality-QRCodePlugin\qrcodeplugin.zip"
  if (!(Test-Path $zipFile))
  {
    Write-Host "Populating QR Code Dependencies"
    $url = "https://github.com/dorreneb/mixed-reality/releases/download/1.1/release.zip"
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile($url, $zipFile)
    Expand-Archive -Path $zipFile -DestinationPath "$PSScriptRoot\..\..\external\MixedReality-QRCodePlugin" -Force
  }
  else
  {
    Write-Host "external/MixedReality-QRCodePlugin already populated in repo"
  }
}

function DownloadARKitPlugin
{
  $zipFile = "$PSScriptRoot\..\..\external\ARKit-Unity-Plugin\unity-arkit-plugin.zip"
  if (!(Test-Path $zipFile))
  {
    Write-Host "Populating ARKit Dependencies"
    $url = "https://bitbucket.org/Unity-Technologies/unity-arkit-plugin/get/94e47eae5954.zip"
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile($url, $zipFile)
    Expand-Archive -Path $zipFile -DestinationPath "$PSScriptRoot\..\..\external\ARKit-Unity-Plugin\Temp" -Force
    Move-Item -Path "$PSScriptRoot\..\..\external\ARKit-Unity-Plugin\Temp\Unity-Technologies-unity-arkit-plugin-94e47eae5954\*" -Destination "$PSScriptRoot\..\..\external\ARKit-Unity-Plugin"
    Remove-Item -Path "$PSScriptRoot\..\..\external\ARKit-Unity-Plugin\Temp" -Recurse
  }
  else
  {
    Write-Host "external/ARKit-Unity-Plugin already populated in repo"
  }
}