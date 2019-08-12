function DownloadQRCodePlugin
{
  if (!(Test-Path "$PSScriptRoot\..\..\external\MixedReality-QRCodePlugin\release"))
  {
    $zipFile = "$PSScriptRoot\..\..\external\qrcodeplugin.zip"
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
  if (!(Test-Path "$PSScriptRoot\..\..\external\ARKit-Unity-Plugin\Unity-Technologies-unity-arkit-plugin-94e47eae5954"))
  {
    $zipFile = "$PSScriptRoot\..\..\external\unity-arkit-plugin.zip"
    $url = "https://bitbucket.org/Unity-Technologies/unity-arkit-plugin/get/94e47eae5954.zip"
    $wc = New-Object System.Net.WebClient
    $wc.DownloadFile($url, $zipFile)
    Expand-Archive -Path $zipFile -DestinationPath "$PSScriptRoot\..\..\external\ARKit-Unity-Plugin" -Force
  }
  else
  {
    Write-Host "external/ARKit-Unity-Plugin already populated in repo."
  }
}
