git config core.symlinks true
git submodule update --init
dir '..\..\samples' -Recurse -File | ?{$_.LinkType -eq "SymbolicLink" } | Remove-Item
git checkout -f -- :/samples