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

Function ConfigureRepo {
    Process {
	# Enable symlink support for the repository. This is necessary
	# for inter-directory linking to build the HolographicCamera.Unity project
	# as well as for the samples to include submodule repository content
	# via directory symlinks.
	Write-Output "Enabling symbolic links for the repository."
	git config core.symlinks true

	# Ensure consistent line endings.
	Write-Output "Configuring the repository to use crlf line endings."
	git config core.autocrlf true
    }
}

Function FixSymbolicLinks {
    Process {
	# If any links were created to the submodules but were broken,
	# restore those symlinks now that the submodules are cloned.
	Write-Output "Fixing all symbolic links."
	"$(Get-Location)\..\..\" | dir -Recurse -File | ?{$_.LinkType -eq "SymbolicLink" } | Restore-SymbolicLink

	# If any links were created before the repository was configured to use
	# symlinks, those links need to be restored.
	git status --porcelain | Restore-SymbolicLinkFromTypeChange
    }
}

Function FixSymbolicLinksForDirectory {
    Param(
      $Directory
    )

    Process {
	# If any links were created to the submodules but were broken,
	# restore those symlinks now that the submodules are cloned.
	Write-Output "Fixing symbolic links for the following directory: $Directory"
	$Directory | dir -Recurse -File | ?{$_.LinkType -eq "SymbolicLink" } | Restore-SymbolicLink

	# If any links were created before the repository was configured to use
	# symlinks, those links need to be restored.
	git status --porcelain | Restore-SymbolicLinkFromTypeChange
    }
}

Function HardCopySymbolicLink {
    [CmdletBinding()]
    Param(
        [Parameter(ValueFromPipeline)]$file
    )

    Process {
        $fileContent = $file.Target
        Write-Output "Copying content from $fileContent to "$file.FullName

        $currDir = Get-Location
        $fileDir = Split-Path $file.FullName
        $fileName = Split-Path $file.FullName -Leaf

        Set-Location $fileDir
        (Get-Item $file).Delete()
        New-Item -ItemType Directory -Force -Path $fileName
        Copy-Item -Path "$fileContent\*" -Destination $fileName -Recurse

        Set-Location $currDir
    }
}

Function HardCopySymbolicLinksForDirectory {
    Param(
      $Directory
    )

    Process {
	# If any links were created to the submodules but were broken,
	# restore those symlinks now that the submodules are cloned.
    Write-Output "Hard copying symbolic links for the following directory: $Directory"
    $Directory | dir -Recurse | ?{$_.LinkType -eq "SymbolicLink" } | HardCopySymbolicLink

	# If any links were created before the repository was configured to use
	# symlinks, those links need to be restored.
	git status --porcelain | Restore-SymbolicLinkFromTypeChange
    }
}