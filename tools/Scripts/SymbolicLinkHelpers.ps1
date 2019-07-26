Function Restore-SymbolicLink {
    [CmdletBinding()]
    Param(
        [Parameter(ValueFromPipeline)]$file
    )

    Process {
        Remove-Item $file.FullName
        git checkout -f -- $file.FullName
    }
}

Function Restore-SymbolicLinkFromTypeChange {
    [CmdletBinding()]
    Param(
        [Parameter(ValueFromPipeline)]$line
    )

    Process {
        $code = $line.Substring(0, 2)

        # Codes are two-character status identifiers. When configuring
        # core.symlinks, files that should have been symbolic links but
        # which were checked out as normal files will have a status of T
        # for typechange. Performing a reset-checkout of the file will
        # now cause Git to create the symbolic link correctly.
        if ($code -eq " T") {
            $path = $line.Substring(3)
            git checkout -f -- ":/$path"
        }
    }
}