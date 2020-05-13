# Spectator View

Spectator View is an augmented reality product that enables viewing HoloLens experiences from secondary devices. Spectator View has multiple configurations and supports a variety of scenarios from filming quick prototypes to producing keynote demos.

## Samples

The Spectator View repository contains multiple sample projects. To see how to set up samples go [here](samples/README.md). To add Spectator View to your own project, see below.

## Getting started with your own project

### Obtaining the code

To build the Microsoft.MixedReality.SpectatorView Unity package, you will need the following:

1. Windows PC
3. [Visual Studio 2019](https://visualstudio.microsoft.com/vs/) installed on the PC
    * Universal Windows Platform development tools (installed through visual studio installer)
    * Desktop development with C++ tools (installed through visual studio installer)
4. [Windows 10 SDK (10.0.18362.0)](https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk)
5. [Git](https://git-scm.com/downloads) installed on the PC and added to your `PATH` variable
6. [NuGet](https://www.nuget.org/downloads) installed on the PC and added to your `PATH` variable

#### Grab external dependencies (optional)
The below dependencies are optional but should be obtained if they are desired for video camera filming.
1. Download the [Azure Kinect Sensor SDK](https://docs.microsoft.com/en-us/azure/kinect-dk/sensor-sdk-download)
2. Download the [Azure Kinect Body Tracking SDK](https://docs.microsoft.com/en-us/azure/kinect-dk/body-sdk-download)
3. Download Blackmagic design's Desktop Video & Desktop Video SDK from [here](https://www.blackmagicdesign.com/support)
    * Search for Desktop Video & Desktop Video SDK in "Latest Downloads" (Note: **10.9.11** is the current version used in the SpectatorView.Compositor.dll. Newer versions may contain breaks.)
    * Extract downloaded content to a `external\Blackmagic DeckLink SDK 10.9.11` (make sure this path matches the path found in )

There are currently two ways to consume the com.microsoft.mixedreality.spectatorview.* Unity package. You can either build the package and generate a folder containing all project content to share with your team or you can add the MixedReality-SpectatorView codebase as a submodule to your project.

### Build a package to share with your team
1. Clone the MixedReality-SpectatorView repository.
2. Checkout your desired branch (`master`).
3. Run `tools\scripts\CreateUnityPackage.bat` in an administrator cmd window.
    > Note: It may take a while to build external dependencies for Spectator View's native components the first time this script is run.
4. Copy the generated packages\com.microsoft.mixedreality.spectatorview.* somewhere in/near your project (don't place the package inside your Unity project's Assets folder).
5. Add a reference to the com.microsoft.mixedreality.spectatorview.* folder to your Unity project's Package/manifest.json file. For more information on referencing a local Unity package, see [here](https://docs.unity3d.com/Manual/upm-ui-local.html).

### Referencing MixedReality-SpectatorView as a submodule
1. Add the MixedReality-SpectatorView repository as a submodule to your preexisting git repository.
2. Checkout your desired branch (`master`).
    > Note: It may take a while to build external dependencies for Spectator View's native components the first time this script is run.
3. Run `tools\scripts\CreateUnityPackage.bat` in an administrator cmd window.
4. Add a reference to the submodule folder src/SpectatorView.Unity/Assets in your Unity project's Package/manifest.json file. For more information on referencing a local Unity package, see [here](https://docs.unity3d.com/Manual/upm-ui-local.html).

> Note 1: Creating a unity package is only supported on PCs. You can prepare the repo on Mac for building the iOS example applications by installing [powershell](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-macos?view=powershell-7) and running `tools\scripts\SetupRepository.ps1`.

> Note 2: If your development setup does not allow for symbolic links, running `tools\scripts\CreateUnityPackage.bat -HardCopySymbolicLinks` will replace symbolic links in the project with copied file content. This should allow developers to generate unity packages, but it will make it more difficult to edit and contribute submodule changes. 

### Detailed Unity Setup
For more information on setting up a Spectator View project after obtaining the com.microsoft.mixedreality.spectatorview.* Unity package, see the following pages:

* [Spectating with an Android, an iOS or a HoloLens device](doc/SpectatorView.Setup.md)
* [Spectating with a video camera](doc/SpectatorView.Setup.VideoCamera.md)

## Architecture

For more information on Spectator View's architecture, see [here](doc/SpectatorView.Architecture.md).

## Debugging

For more information on debugging Spectator View, see [here](doc/SpectatorView.Debugging.md)

## Filing feedback

The easiest way to file feedback is by [opening an issue](https://github.com/microsoft/MixedReality-SpectatorView/issues). When filing feedback, please include the following information (when applicable):

1) Whether you're using a HoloLens or HoloLens 2 device
2) Development PC Windows Version
3) Unity Version
4) Whether you are building with .Net, Mono or il2cpp in Unity
5) Visual Studio Version
6) Windows SDK Version
7) iOS device type/iOS Version
8) Mac OS Version
9) Android device type/Android OS Version
10) Android Studio Version

In addition to opening issues, Spectator View contributors are active on [Stack Overflow](https://stackoverflow.com/). Use the [MRTK tag](https://stackoverflow.com/questions/tagged/mrtk) when asking Spectator View related questions.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit <https://cla.microsoft.com>.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
