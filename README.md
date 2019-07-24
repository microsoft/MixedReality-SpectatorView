# Spectator View

Spectator View is an augmented reality product that enables viewing HoloLens experiences from secondary devices. Spectator View has multiple configurations and supports a variety of scenarios from filming quick prototypes to producing keynote demos.

## Getting Started

### Obtaining the code

**Currently, the supported process for obtaining and consuming Spectator View code is through cloning and referencing the MixedReality-SpectatorView git repository.** Downloading source code from the releases tab is possible, but helper scripts and sample projects may break if you choose not to clone the repository. Steps for cloning and using the git repository are as follows:

1. Download [git](https://git-scm.com/downloads)
2. Clone the MixedReality-SpectatorView repository by running the following command:

`git clone https://github.com/microsoft/MixedReality-SpectatorView.git sv`

>Note 1: Spectator view uses symbolic linked directories in its sample projects, which results in large file paths. It's suggested to place your Unity project in a short named directory (such as C:\sv). Otherwise, file paths may become too long to resolve in Unity.

>Note 2: If you are anticipating contributing to the MixedReality-SpectatorView project, you should fork your own version of the repository and clone it instead of the Microsoft repository. Your forked repository url will look something like this: `https://github.com/YourGitHubAliasHere/MixedReality-SpectatorView.git`.

3. Check out a release branch by running the following commands in your git repository directory:

`git fetch origin release/1.0.0-beta`

`git checkout release/1.0.0-beta`

After running these git commands, you will have a local copy of the MixedReality-SpectatorView codebase. Next, you will need to follow the instructions in `Setup your local environment` to obtain external dependencies.

### Setting up your local environment

The MixedReality-SpectatorView repository uses Unity packages, git submodules and symbolic linked directories for obtaining and referencing external dependencies. Prior to opening any Unity projects, you will need to run a setup script.

* The setup script will configure your git repository to use clrf line endings and support symbolic linked directories.
* The setup script will obtain and update all git submodules declared in the MixedReality-SpectatorView repository.
* The setup script will fix any symbolic linked directories in the MixedReality-SpectatorView repository.

> Note: Not all submodules have the same [MIT license](LICENSE) as the MixedReality-SpectatorView repository. Submodules in this project currently include: [MixedRealityToolkit-Unity](https://github.com/microsoft/MixedRealityToolkit-Unity), [Azure-Spatial-Anchors-Samples](https://github.com/Azure/azure-spatial-anchors-samples) and [ARCore-Unity-SDK](https://github.com/google-ar/arcore-unity-sdk). You should view and accept the licenses in these projects before running the [SetupRepository.bat](tools/Scripts/SetupRepository.bat) script.

Depending on what release you are using the correct setup script may vary. Choose the appropriate script below based on the git branch that you have checked out in your clone of the MixedReality-SpectatorView repository:

#### Setting up the `release/1.0.0-beta` branch

If you are using the release/1.0.0-beta branch, You will need to run the following command:

1. Run `'tools/Scripts/ResetSamples.bat'` as an administrator on a PC  (On Mac or Linux, you can run `'sh /tools/scripts/ResetSamples.sh'`).

#### Setting up the `master` branch

If you are using the master branch, you will need to run the following command:

1. Run `'tools/Scripts/SetupRepository.bat'` as an administrator on your PC (On Mac or Linux, you can run `'sh /tools/scripts/SetupRepository.sh'`).



### Samples

After going through the setup steps in 'Obtaining the code' and 'Setting up your local environment', sample projects will be configured for use in your clone of the MixedReality-SpectatorView repository. It's easy to get started by building off one of the samples or by inspecting them to understand project setup. For more information, see [Samples](samples/README.md).

## Setting up your own project

### Adding references to your own project

After obtaining a local clone of the MixedReality-SpectatorView repository and resolving its external dependencies (see above), the suggested mechanism for referencing the code is by adding symbolic linked directories to your Unity project's Assets folder. You can do this with the following:

#### Using the `release/1.0.0-beta` branch

1) Close any instance of Unity.
2) Open an administrator command window.
3) Run the following commands, updating the paths to reflect your local environment:

* `cd c:\Your\Unity\Project\Assets`
* `set PathToRepo="c:\your\path\to\MixedReality-SpectatorView\"`
* `mklink /D "MixedReality-SpectatorView" "%PathToRepo%\MixedReality-SpectatorView\src\SpectatorView.Unity\Assets"`
* `mklink /D "ARKit-Unity-Plugin" "%PathToRepo%\MixedReality-SpectatorView\external\ARKit-Unity-Plugin"`
* `mklink /D "AzureSpatialAnchorsPlugin" "%PathToRepo%\MixedReality-SpectatorView\external\Azure-Spatial-Anchors-Samples\Unity\Assets\AzureSpatialAnchorsPlugin"`
* `mklink /D "GoogleARCore" "%PathToRepo%\MixedReality-SpectatorView\external\ARCore-Unity-SDK\Assets"`
* `mklink /D "MixedReality-QRCodePlugin" "%PathToRepo%\MixedReality-SpectatorView\external\MixedReality-QRCodePlugin"`
* `mkdir AzureSpatialAnchors.Resources`
* `cd AzureSpatialAnchors.Resources`
* `mklink /D "android-logos" "%PathToRepo%\MixedReality-SpectatorView\external\Azure-Spatial-Anchors-Samples\Unity\Assets\android-logos"`
* `mklink /D "logos" "%PathToRepo%\MixedReality-SpectatorView\external\Azure-Spatial-Anchors-Samples\Unity\Assets\logos"`

#### Using the `master` branch

1. Close any instances of Unity.
2. Open an administrator command window.
3. Run `tools\Scripts\AddDependencies.bat c:\Your\Unity\Project\Assets c:\Your\MixedReality-SpectatorView\` (On Mac or Linux, you can run `'sh tools/Scripts/AddDependencies.sh //Users/You/Your/Unity/Project/Assets //Users/You/Your/MixedReality-SpectatorView/'`).

Now, when you reopen your project in Unity, folders should appear in your project's Assets folder.

### Basic Setup

Below are simple instructions for adding Spectator View to your project:

1. Ensure you have all of the [Software & Hardware](doc/SpectatorView.Setup.md##Software%20%26%20Hardware%20Requirements).
    - Ensure you have added the appropriate platform dependencies to your project (ARCore/ARKit)
2. Go through the **Getting Started** steps above to obtain and reference the MixedReality-SpectatorView codebase in your project.
3. Add a reference to the Spectator View code by going through the **Adding references to your project** steps above.
3. Choose a [Spatial Alignment Strategy](src/SpectatorView.Unity/Assets/SpatialAlignment/README.md) and spatial localizer.
4. Add the appropriate [Spatial Localizer Dependencies](doc/SpectatorView.Setup.md##Spatial%20Localizer%20Dependencies) based on your chosen spatial localizer.
5. Add the `MixedReality.SpectatorView/SpectatorView/Prefabs/SpectatorView.prefab` prefab to your primary scene.
6. Configure the localization methods you wish to use: [Spatial Localizer Dependencies](doc/SpectatorView.Setup.md##Spatial%20Localizer%20Dependencies).
7. Generate and check-in Asset Caches to your project repo: [Before Building](doc/SpectatorView.Setup.md###Before%20Building)
8. Build & Deploy your primary scene onto the hosting device.
9. Configure the IP Address of the host device in the spectating scene for each platform `MixedReality.SpectatorView/SpectatorView/SpectatorView.<Platform>.unity` you will deploy to.
10. Build & Deploy your spectating scenes onto the spectating devices.

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
