git config core.symlinks true
git submodule update --init
dir '$PSScriptRoot\..\..\src' -Recurse -File | ?{$_.LinkType -eq "SymbolicLink" } | Remove-Item
dir '$PSScriptRoot\..\..\samples' -Recurse -File | ?{$_.LinkType -eq "SymbolicLink" } | Remove-Item
git checkout -f -- :/samples