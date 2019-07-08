Set-Location (Split-Path $MyInvocation.MyCommand.Path) 
git config core.symlinks true
git submodule update --init
dir "$PSScriptRoot\..\..\samples" -Recurse -File | ?{$_.LinkType -eq "SymbolicLink" } | Remove-Item
git checkout -f -- :/samples