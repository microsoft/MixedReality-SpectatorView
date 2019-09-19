param(
    [switch] $iOS,
    [switch] $NoDownloads
)

Import-Module "$PSScriptRoot\SetupRepositoryFunc.psm1"

if ($NoDownloads)
{
    Write-Host "Running setup with no downloads"
    SetupRepository -NoDownloads
}
elseif ($iOS)
{
    Write-Host "Running setup with iOS dependencies"
    SetupRepository -iOS
}
else
{
    Write-Host "Running default repo setup"
    SetupRepository
}
 
Write-Host "`n"
Write-Host -NoNewLine 'Setup Completed. Press any key to continue...';
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');
Write-Host "`n"
