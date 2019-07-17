#!/bin/sh

# Directory to the build script
SCRIPTPATH="$( cd "$(dirname "$0")" ; pwd -P )"

git config core.symlinks true
git config core.autocrlf true
git submodule update --init
find $SCRIPTPATH/../../samples -type l | xargs rm
git checkout -f -- :/samples