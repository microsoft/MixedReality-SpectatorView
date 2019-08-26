function SetupToolsPath
{
  param
  (
    $ProjectPath,
    $ToolsAssetsPath
  )

  $projAssets = $ProjectPath + "\Assets"
  $toolsPath = $projAssets + "\BuildTools.Unity"
  If (!(Test-Path $toolsPath))
  {
    $tempLoc = Get-Location
    Set-Location $projAssets
    cmd /c mklink /D "BuildTools.Unity" $ToolsAssetsPath
    Set-Location $tempLoc
  }
}

function AddSubmodule
{
  param
  (
    $Repo,
    $DirectoryName,
    $Branch
  )

  Write-Host "Adding submodule:$repo at external\$DirectoryName"
  $origLoc = Get-Location

  if (!(Test-Path "$PSScriptRoot\..\..\..\external\$DirectoryName"))
  {
    Set-Location $PSScriptRoot
    git submodule add $Repo "../../../external/$DirectoryName"
    Set-Location $origLoc
  }

  Set-Location "$PSScriptRoot\..\..\..\external\$DirectoryName"
  git pull origin $Branch
  Set-Location $origLoc
}

function HideUnityAssetsDirectory
{
    param
    (
      $Path
    )

    if (Test-Path $Path)
    {
       $Leaf = Split-Path -Path $Path -Leaf
       $NewLeaf = ".$Leaf"
       Rename-Item -Path $Path -NewName $NewLeaf
    }
}

function IncludeUnityAssetsDirectory
{
    param
    (
      $Path
    )

    if (Test-Path $Path)
    {
       $Leaf = Split-Path -Path $Path -Leaf
       $NewLeaf = $Leaf.TrimStart(".")
       Rename-Item -Path $Path -NewName $NewLeaf
    }
}

function BuildProject
{
  param
  (
    $MSBuild,
    $VSSolution,
    $Configuration,
    $Platform,
    [Parameter(Mandatory=$true)][ref]$Succeeded
  )

  Write-Host "Cleaning and Building Project:$VSSolution, Platform:$Platform, Configuration:$Configuration"
  & $MSBuild /t:Clean /p:Configuration="$Configuration" /p:Platform="$Platform" $VSSolution
  $cleanSuccess = $?

  $buildSuccess = "False"
  if ($cleanSuccess)
  {
    & $MSBuild /t:Build /p:Configuration="$Configuration" /p:Platform="$Platform" $VSSolution
    $buildSuccess = $?
  }

  $Succeeded.Value = $cleanSuccess -And $buildSuccess
}