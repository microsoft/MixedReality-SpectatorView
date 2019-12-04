# Spectator View Samples

This folder contains sample and demo projects that are maintained by Spectator View contributors. Below you will find the instructions on how to get started, as well as the sample folder structure setup and dependencies.

## Getting Started

Ensure you have all the required software, for detailed instructions see [Required Software](../doc/SpectatorView.Setup.md#software--hardware-requirements).

- Git Tools
- Visual Studio
- Unity 2018.3.14f1
- Windows 10.0.18362.0 SDK
- __(Optional)__ Android Studio
- __(Optional)__ XCode

Once you have the required software, follow these steps:

1. Using your favorite Git management tool, clone this repository.
2. Then with git, checkout the latest release of the code. (This will be the `release/*.*.` branch with the highest version number)
3. Run `/tools/scripts/SetupRepository.bat` as an administrator to get ensure the appropriate submodules for the sample projects are cloned and the correct directories are linked into the sample project.
    - On Mac or Linux, you can run `/tools/scripts/SetupRepository.sh`.

> In future updates you can pull to latest by invoking `git pull --recurse-submodules` command.

## Contents

This repository currently has the following samples:

1. [//BUILD 2019 Demo](./Build2019Demo.Unity/README.md)
    - This sample shows how to use [Azure Spatial Anchors](https://azure.microsoft.com/en-us/services/spatial-anchors/) to spatially align devices in the physical world.
    - This sample also shows to use Spectator View with the [Mixed Reality Toolkit](http://aka.ms/mrtk).
2. [SpectatorView.Example.Unity](./SpectatorView.Example.Unity/README.md)
    - This sample shows how to use [QR Code detection](https://docs.microsoft.com/en-us/windows/mixed-reality/qr-code-tracking) on HoloLens 2 to spatially align devices in the physical world.
    - This sample can also use [ArUco marker detection](https://docs.opencv.org/master/d5/dae/tutorial_aruco_detection.html) on HoloLens 1 to spatially align devices in the physical world.

## Troubleshooting

If you encounter some issues, the first thing to do is to run `/tools/scripts/SetupRepository.bat` as an administrator. For additional troubleshooting options look below.

### __Issue:__ Unity Project Folder Structure Broken

If you happened to run step 3 above when Unity was open, you will notice that the Project window may contain the incorrect folder structure. This only happens when a symlink is inflated while Unity is open, to fix this:

1. Close Unity
2. Delete the Library folder that is adjacent to the Assets folder
3. Re-open Unity

### __Issue:__ DirectoryNotFoundException during Build

There is a known issue with `MixedRealityToolkit-Unity` codebase that produces build issues due to deeply nested AsmDef files. You will see build errors such as:

    DirectoryNotFoundException: Could not find a part of the path "X:\...<SOME_PATH>...\samples\Build2019Demo.Unity\Assets\MixedRealityToolkit-Unity\MixedRealityToolkit.Examples\Demos\Utilities\InspectorFields\Inspectors\MixedRealityToolkit.Examples.Demos.Utilities.InspectorFields.Inspectors.asmdef"

To work around this issue for the time being, place your project in a directory with a shorter name, such as c:\proj.