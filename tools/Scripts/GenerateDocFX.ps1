# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See LICENSE in the project root for license information.

# Script to generate the docs into MixedReality-SpectatorView/doc/build for local iteration.

param(
    # Serve the generated docs on a temporary web server @ localhost
    # The docs are not completely static, so will not work if not served.
    [switch]$serve = $false
)

# Clear output dir
Write-Host "Clear previous version from build"
Remove-Item -Force -Recurse -ErrorAction Ignore $PSScriptRoot\..\..\doc\build

# Install DocFX command-line tool
Write-Host "Installing DocFx in build/docs using NuGet..."
nuget install docfx.console -o $PSScriptRoot\..\..\doc\build\
$DocFxDir = Get-ChildItem -Path $PSScriptRoot\..\..\doc\build\ | Where-Object {$_.PSIsContainer -eq $true -and $_.Name -match "docfx"} | Select-Object -first 1
$DocFxExe = Join-Path $DocFxDir "tools\docfx.exe"
Write-Host  "DocFx install : $DocFxExe"

# Generate the documentation
$logFile = "docfx.log"
Invoke-Expression "$($DocFxDir.FullName)\tools\docfx.exe $PSScriptRoot\..\..\docfx.json --intermediateFolder $PSScriptRoot\..\..\doc\build\obj --force -o $PSScriptRoot\..\..\doc\build $(if ($serve) {' --serve'} else {''})" | Tee-Object -FilePath $logFile
Write-Host "Documentation generated at $PSScriptRoot\..\..\doc\build\generated\index.html"

# Clean-up obj/xdoc folders in source -- See https://github.com/dotnet/docfx/issues/1156
$XdocDirs = Get-ChildItem -Path $PSScriptRoot\..\..\ -Recurse | Where-Object {$_.PSIsContainer -eq $true -and $_.Name -eq "xdoc"}
foreach ($Xdoc in $XdocDirs)
{
    if ($Xdoc.Parent -match "obj")
    {
        Write-Host "Deleting $($Xdoc.FullName)"
        Remove-Item -Force -Recurse -ErrorAction Ignore $Xdoc.FullName
    }
}

Write-Host "View logs in $logFile."
Write-Host "To view documentation, run '$($DocFxDir.FullName)\tools\docfx.exe serve $PSScriptRoot\..\..\doc\build\generated -p 8081'"
Write-Host "Then open a web browser to http://localhost:8081"
