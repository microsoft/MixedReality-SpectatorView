projPath="$1"
repoPath="$2"

if [ ! -d $projPath ]
then
    echo Invalid Unity project path specified: $projPath
elif [ ! -d $repoPath ]
then
    echo Invalid MixedReality-SpectatorView path specified: $repoPath
else
    echo
    echo Creating symbolic links in the following directory: $projPath
    echo Symbolic linkes based off of the following path: $repoPath
    echo

    cd $projPath
    ln -s "$repoPath/src/SpectatorView.Unity/Assets" "MixedReality-SpectatorView"
    ln -s "$repoPath/external/ARKit-Unity-Plugin" "ARKit-Unity-Plugin"
    ln -s "$repoPath/external/Azure-Spatial-Anchors-Samples/Unity/Assets/AzureSpatialAnchorsPlugin" "AzureSpatialAnchorsPlugin"
    ln -s "$repoPath/external/GoogleARCore" "GoogleARCore"
    ln -s "$repoPath/external/MixedReality-QRCodePlugin" "MixedReality-QRCodePlugin"
    mkdir AzureSpatialAnchors.Resources
    cd AzureSpatialAnchors.Resources
    ln -s "$repoPath/external/Azure-Spatial-Anchors-Samples/Unity/Assets/android-logos" "android-logos"
    ln -s "$repoPath/external/Azure-Spatial-Anchors-Samples/Unity/Assets/logos" "logos"
fi
