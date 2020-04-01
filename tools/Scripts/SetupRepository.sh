#!/bin/sh

# Directory to the build script
SCRIPTPATH="$( cd "$(dirname "$0")" ; pwd -P )"

echo "Configuring repo to support symbolic links and crlf line endings."
git config core.symlinks true
git config core.autocrlf true

echo "Syncing submodules."
git submodule sync

echo "Updating submodules."
git submodule update --init

echo "Fixing symbolic links."
find $SCRIPTPATH/../../samples -type l | xargs rm
git checkout -f -- :/samples
