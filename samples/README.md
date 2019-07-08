# Spectator View Samples

This folder contains sample and demo projects that are maintained by the SpectatorView team. Below you will find the instructions on how to get started, as well as the sample folder structure setup and dependencies.

## Getting Started

Ensure you have all the required software, for detailed instructions see [Required Software](../README.md).

- Git Tools
- Visual Studio
- Unity 2018.3.14f1
- __(Optional)__ Windows 10.0.18362.0 SDK
- __(Optional)__ Android Studio
- __(Optional)__ XCode

Once you have the required software, follow these steps:

1. Using your favorite Git management tool, clone this repository if you haven't yet.
2. Then with git, pull the latest version of the code.
3. Run `/tools/scripts/ResetSamples.bat` as an administrator to get to a clean state of the Samples folder with appropriate configuration.
    - On Mac or Linux, you can run `/tools/scripts/ResetSamples.sh`.

> In future updates you can pull to latest by invoking `git pull --recurse-submodules` command.

## Contents

This repository currently has the following samples:

- [//BUILD 2019 Demo](./Build2019Demo.Unity/README.md)

## Troubleshooting

If you encounter some issues, the first thing to do is to run `/tools/scripts/ResetSamples.bat` as an administrator. For additional troubleshooting options look below.

### __Issue:__ Unity Project Folder Structure Broken

If you happened to run step 3 above when Unity was open, you will notice that the Project window may contain the incorrect folder structure. This only happens when a symlink is inflated while Unity is open, to fix this:

1. Close Unity
2. Delete the Library folder that is adjacent to the Assets folder
3. Re-open Unity

### __Issue:__ DirectoryNotFoundException during Build

There is a known issue with `MixedRealityToolkit-Unity` codebase that produces build issues due to deeply nested AsmDef files. You will see build errors such as:

    DirectoryNotFoundException: Could not find a part of the path "X:\...<SOME_PATH>...\samples\Build2019Demo.Unity\Assets\MixedRealityToolkit-Unity\MixedRealityToolkit.Examples\Demos\Utilities\InspectorFields\Inspectors\MixedRealityToolkit.Examples.Demos.Utilities.InspectorFields.Inspectors.asmdef"
