param(
    [switch] $NoDownloads,
    [switch] $HardCopySymbolicLinks
)

Import-Module "$PSScriptRoot\SetupRepositoryFunc.psm1"

$repoSetupSucceeded = $false
if ($NoDownloads -And $HardCopySymbolicLinks)
{
    Write-Host "Running setup with no downloads and hard copying of symbolic links"
    SetupRepository -NoDownloads -HardCopySymbolicLinks -NoBuilds -Succeeded ([ref]$repoSetupSucceeded)
}
elseif ($NoDownloads)
{
    Write-Host "Running setup with no downloads"
    SetupRepository -NoDownloads -NoBuilds -Succeeded ([ref]$repoSetupSucceeded)
}
elseif ($HardCopySymbolicLinks)
{
    Write-Host "Running setup with hard copying of symbolic links"
    SetupRepository -HardCopySymbolicLinks -NoBuilds -Succeeded ([ref]$repoSetupSucceeded)
}
else
{
    Write-Host "Running default repo setup"
    SetupRepository -NoBuilds -Succeeded ([ref]$repoSetupSucceeded)
}
 
Write-Host "`n"
Write-Host -NoNewLine 'Setup Completed. Press any key to continue...';
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');
Write-Host "`n"
