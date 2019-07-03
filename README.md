# Spectator View

Spectator View is an augmented reality product that enables viewing HoloLens experiences from secondary devices. Spectator View has multiple configurations and supports a variety of scenarios from filming quick prototypes to producing keynote demos.

## Getting Started

These are the simple instructions to get started with Spectator View by adding it to your project.

1. Ensure you have all of the [Software & Hardware](doc/SpectatorView.Setup.md##Software%20%26%20Hardware%20Requirements).
    - Ensure you have added the appropriate platform dependencies to your project (ARCore/ARKit)
2. Download the Spectator View repository.
3. Copy the contents of `src\SpectatorView.Unity\Assets` to `MixedReality.SpectatorView` folder in the Assets folder of your project.
4. Based on which localizer you wish to have, add the appropriate [Spatial Localizer Dependencies](doc/SpectatorView.Setup.md##Spatial%20Localizer%20Dependencies).
5. Add the `MixedReality.SpectatorView/SpectatorView/Prefabs/SpectatorView.prefab` prefab to your primary scene.
6. Configure the localization methods you wish to use: [Spatial Localizer Dependencies](doc/SpectatorView.Setup.md##Spatial%20Localizer%20Dependencies).
7. Generate and check-in Asset Caches: [Before Building](doc/SpectatorView.Setup.md###Before%20Building)
8. Build & Deploy your primary scene onto the hosting device.
9. Configure the IP Address of the host device in the spectating scene for each platform `MixedReality.SpectatorView/SpectatorView/SpectatorView.<Platform>.unity` you will deploy to.
10. Build & Deploy your spectating scenes onto the spectating devices.

> Note: Some platforms require a special build step, for build information see: [Building & Deploying](doc/SpectatorView.Setup.md###Building%20%26%20Deploying)

### Samples

It's also easy to get started by building off one of the samples, or inspecting them to understand project setup. For more information, see [Samples](samples/README.md).

## Architecture

For more information on Spectator View's architecture, see [here](doc/SpectatorView.Architecture.md).

## Setup

* [**Spectating with an Android or iOS device**](doc/SpectatorView.Setup.md)
* [**Spectating with a DSLR camera**](doc/SpectatorView.Setup.DSLR.md)

## Debugging

For more information on debugging Spectator View, see [here](doc/SpectatorView.Debugging.md)

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
