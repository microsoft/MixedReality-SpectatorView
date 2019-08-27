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
    git submodule update --init --recursive
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

function BuildUnityProject
{
    param
    (
      $ProjectPath,
      $SceneList,
      $EditorPath,
      $Platform,
      $Arch,
      $ScriptingBackend,
      $UnityArgs,
      $Define,
      $BuildArtifactStagingDirectory,
      $UnityCacheServerAddress,
      $OutDirExt,
      [Parameter(Mandatory=$true)][ref]$Succeeded
    )

# The build output goes to a unique combination of Platform + Arch + ScriptingBackend to ensure that
# each build will have a fresh destination folder.
$outDir = "$BuildArtifactStagingDirectory\build\${Platform}_${Arch}_${ScriptingBackend}_${OutDirExt}"
$logFile = New-Item -Path "$outDir\build\build.log" -ItemType File -Force
$logDirectory = "$outDir\logs"
   
Write-Host "`n`nBeginning Unity Player Build"
Write-Host "Output Directory: $outDir"
Write-Host "Project Path: $ProjectPath"
Write-Host "Editor Path: $EditorPath"
Write-Host "Platform: $Platform"
Write-Host "Architecture: $Arch"
Write-Host "Scripting Backend: $ScriptingBackend"
Write-Host "Scene List: $SceneList"
Write-Host "Define: $Define"
Write-Host "Unity Args: $UnityArgs"
Write-Host "Build Artifact Staging Directory: $BuildArtifactStagingDirectory"
Write-Host "Unity Cache Server Address: $UnityCacheServerAddress`n`n"

$extraArgs = ''
If ("$Platform" -eq "UWP")
{
   $extraArgs += '-buildTarget WSAPlayer -buildAppx'
}
ElseIf ("$Platform" -eq "Standalone")
{
   $extraArgs += "-buildTarget StandaloneWindows"
}
ElseIf ("$Platform" -eq "iOS")
{
   $extraArgs += "-buildTarget iOS"
}
ElseIf ("$Platform" -eq "Android")
{
   $extraArgs += "-buildTarget Android"
}

If ("$UnityArgs" -ne "none")
{
   $extraArgs += " $UnityArgs"
}

If ("$ScriptingBackend" -eq ".NET")
{
   $extraArgs += " -scriptingBackend 2"
}

If (![string]::IsNullOrEmpty($Define))
{
   $extraArgs += " -define $Define"
}

Write-Host "Extra Args: $extraArgs"
Write-Host "Starting build process"
$proc = Start-Process -FilePath "$EditorPath" -ArgumentList "-projectPath $ProjectPath -executeMethod Microsoft.MixedReality.BuildTools.Unity.UnityPlayerBuildTools.StartCommandLineBuild -sceneList $SceneList -logFile $($logFile.FullName) -batchMode -$Arch -buildOutput $outDir $extraArgs -CacheServerIPAddress $UnityCacheServerAddress -logDirectory $logDirectory" -PassThru
$ljob = Start-Job -ScriptBlock { param($log) Get-Content "$log" -Wait } -ArgumentList $logFile.FullName
   
while (-not $proc.HasExited -and $ljob.HasMoreData)
{
   Receive-Job $ljob
   Start-Sleep -Milliseconds 200
}
Receive-Job $ljob
   
Stop-Job $ljob
   
Remove-Job $ljob
Stop-Process $proc

Write-Output '====================================================='
Write-Output '           Unity Build Player Finished               '
Write-Output '====================================================='

   # Changing this variable will break reporting scripts
   $Succeeded.Value = ($proc.ExitCode -eq 0)
}

function ConfirmEditorCompiles
{
    param
    (
      $ProjectPath,
      $SceneList,
      $EditorPath,
      $Platform,
      $Arch,
      $ScriptingBackend,
      $UnityArgs,
      $Define,
      $BuildArtifactStagingDirectory,
      $UnityCacheServerAddress,
      $OutDirExt,
      [Parameter(Mandatory=$true)][ref]$Succeeded
    )

# The build output goes to a unique combination of Platform + Arch + ScriptingBackend to ensure that
# each build will have a fresh destination folder.
$outDir = "$BuildArtifactStagingDirectory\build\${Platform}_${Arch}_${ScriptingBackend}_${OutDirExt}"
$logFile = New-Item -Path "$outDir\build\build.log" -ItemType File -Force
$logDirectory = "$outDir\logs"
   
$extraArgs = ''
If ("$Platform" -eq "UWP")
{
   $extraArgs += '-buildTarget WSAPlayer -buildAppx'
}
ElseIf ("$Platform" -eq "Standalone")
{
   $extraArgs += "-buildTarget StandaloneWindows"
}
ElseIf ("$Platform" -eq "iOS")
{
   $extraArgs += "-buildTarget iOS"
}
ElseIf ("$Platform" -eq "Android")
{
   $extraArgs += "-buildTarget Android"
}

If ("$UnityArgs" -ne "none")
{
   $extraArgs += " $UnityArgs"
}

If ("$ScriptingBackend" -eq ".NET")
{
   $extraArgs += " -scriptingBackend 2"
}

If (![string]::IsNullOrEmpty($Define))
{
   $extraArgs += " -define $Define"
}

$proc = Start-Process -FilePath "$EditorPath" -ArgumentList "-projectPath $ProjectPath -executeMethod Microsoft.MixedReality.BuildTools.Unity.UnityPlayerBuildTools.ConfirmEditorCompiles -sceneList $SceneList -logFile $($logFile.FullName) -batchMode -$Arch -buildOutput $outDir $extraArgs -CacheServerIPAddress $UnityCacheServerAddress -logDirectory $logDirectory" -PassThru
$ljob = Start-Job -ScriptBlock { param($log) Get-Content "$log" -Wait } -ArgumentList $logFile.FullName
   
while (-not $proc.HasExited -and $ljob.HasMoreData)
{
   Receive-Job $ljob
   Start-Sleep -Milliseconds 200
}
Receive-Job $ljob
   
Stop-Job $ljob
   
Remove-Job $ljob
Stop-Process $proc

Write-Output '====================================================='
Write-Output '           Unity Build Player Finished               '
Write-Output '====================================================='

If (Test-Path $logFile.FullName)
{
   Write-Output '====================================================='
   Write-Output '           Begin Unity Player Log                    '
   Write-Output '====================================================='

   Get-Content $logFile.FullName

   Write-Output '====================================================='
   Write-Output '           End Unity Player Log                      '
   Write-Output '====================================================='
}
Else
{
   Write-Output 'Unity Player Log Missing!'
}

   # Changing this variable will break reporting scripts
   $Succeeded.Value = ($proc.ExitCode -eq 0)
   Write-Host "`n`nBuild Succeeded: $(Succeeded.Value)`n`n"
}