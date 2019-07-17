# Spectator View

Spectator View is an augmented reality product that enables viewing HoloLens experiences from secondary devices. Spectator View has multiple configurations and supports a variety of scenarios from filming quick prototypes to producing keynote demos.

## Getting Started

### Obtaining the code

Active development occurs in MixedReality-SpectatorView's master branch. It's suggested to consume Spectator View code from one of the release branches compared to the master branch.

>Note: Spectator view uses symbolic linked directories in its sample projects, which results in large file paths. It's suggested to place your Unity project in a short named directory (such as C:\sv). Otherwise, file paths may become too long to resolve in Unity.

### Setting up your local environment

The MixedReality-SpectatorView repository uses unity packages, git submodules and symbolic linked directories for obtaining and referencing external dependencies. Prior to opening any Unity projects, its suggested to run `tools/Scripts/SetupRepository.bat` on your PC (On Mac or Linux, you can run `/tools/scripts/SetupRepository.sh`). This script has a few purposes:

1. It configures your git repository to use clrf line endings and support symbolic linked directories.
2. It obtains and updates all git submodules declared in the MixedReality-SpectatorView repository.
3. It fixes any symbolic linked directories in the MixedReality-SpectatorView respository.

> Note: Not all submodules have the same [MIT license](LICENSE) as the MixedReality-SpectatorView repository. Submodules in this project currently include: [MixedRealityToolkit-Unity](https://github.com/microsoft/MixedRealityToolkit-Unity), [Azure-Spatial-Anchors-Samples](https://github.com/Azure/azure-spatial-anchors-samples) and [ARCore-Unity-SDK](https://github.com/google-ar/arcore-unity-sdk). You should view and accept the licenses in these projects before running the [SetupRepository.bat](tools/Scripts/SetupRepository.bat) script.

### Samples

It's easy to get started by building off one of the samples, or inspecting them to understand project setup. For more information, see [Samples](samples/README.md).

## Setting up your own project

### Basic Setup

Below are simple instructions for adding Spectator View to your project:

1. Ensure you have all of the [Software & Hardware](doc/SpectatorView.Setup.md##Software%20%26%20Hardware%20Requirements).
    - Ensure you have added the appropriate platform dependencies to your project (ARCore/ARKit)
2. Download the Spectator View repository.
3. Copy the contents of `src\SpectatorView.Unity\Assets` to `MixedReality.SpectatorView` folder in the Assets folder of your project.
4. Choose a [Spatial Alignment Strategy](src/SpectatorView.Unity/Assets/SpatialAlignment/README.md) and spatial localizer.
5. Add the appropriate [Spatial Localizer Dependencies](doc/SpectatorView.Setup.md##Spatial%20Localizer%20Dependencies) based on your chosen spatial localizer.
6. Add the `MixedReality.SpectatorView/SpectatorView/Prefabs/SpectatorView.prefab` prefab to your primary scene.
7. Configure the localization methods you wish to use: [Spatial Localizer Dependencies](doc/SpectatorView.Setup.md##Spatial%20Localizer%20Dependencies).
8. Generate and check-in Asset Caches to your project repo: [Before Building](doc/SpectatorView.Setup.md###Before%20Building)
9. Build & Deploy your primary scene onto the hosting device.
10. Configure the IP Address of the host device in the spectating scene for each platform `MixedReality.SpectatorView/SpectatorView/SpectatorView.<Platform>.unity` you will deploy to.
11. Build & Deploy your spectating scenes onto the spectating devices.

> Note: Some platforms require a special build step, for build information see: [Building & Deploying](doc/SpectatorView.Setup.md###Building%20%26%20Deploying)

### Detailed Setup
For more information on setting up a Spectator View project, see the following pages:

* [Spectating with an Android or iOS device](doc/SpectatorView.Setup.md)
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
