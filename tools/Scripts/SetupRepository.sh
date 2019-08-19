#!/bin/sh

# Directory to the build script
SCRIPTPATH="$( cd "$(dirname "$0")" ; pwd -P )"

echo "Configuring repo to support symbolic links and crlf line endings."
git config core.symlinks true
git config core.autocrlf true

ZIPPATH="$SCRIPTPATH/../../external/ARKit-Unity-Plugin/ARKitUnityPlugin.zip"
if [ ! -f $ZIPPATH ]
then
    echo "Obtaining ARKit-Unity-Plugin dependencies."
    curl -o $ZIPPATH https://bitbucket.org/Unity-Technologies/unity-arkit-plugin/get/94e47eae5954.zip
    echo "Extracting ARKit-Unity-Plugin dependencies."
    TEMPPATH="$SCRIPTPATH/../../external/ARKit-Unity-Plugin/Temp"
    mkdir $TEMPPATH
    unzip $ZIPPATH -d $TEMPPATH
    mv $TEMPPATH/Unity-Technologies-unity-arkit-plugin-94e47eae5954/* $TEMPPATH/../
    rm -r $TEMPPATH
fi

echo "Updating submodules."
git submodule update --init

echo "Fixing symbolic links."
find $SCRIPTPATH/../../samples -type l | xargs rm
git checkout -f -- :/samples
