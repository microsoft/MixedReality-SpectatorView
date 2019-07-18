# SpectatorView.Native Dlls

The following Dlls are built out of the [SpectatorView.Native.sln](SpectatorView.Native.sln).

- **SpectatorView.Compositor.dll** is needed for DSLR camera calibration and DSLR camera spectating experiences.
- **SpectatorView.Compositor.UnityPlugin.dll** is needed for DSLR camera calibartion and DSLR camera spectating experiences.
- **SpectatorView.OpenCV.dll** is used in both ArUco marker detection and DSLR camera calibration.
- **SpectatorView.WinRTExtensions.dll** is used for ArUco marker detection and QR code detection.

## Building DLLs

The instructions below show how to build all of the native DLLs required by SpectatorView.

### 1. Obtain external dependencies

**DeckLink Capture Card**
If you are using a Blackmagic design capture card, you will need to install the SDK and create a Visual Studio user macro for its location.

- Download Blackmagic design's Desktop Video & Desktop Video SDK from here: <https://www.blackmagicdesign.com/support> - Search for Desktop Video & Desktop Video SDK in "Latest Downloads" (Note: **10.9.11** is the current version used in the SpectatorView.Compositor.dll. Newer versions may contain breaks.)

    >Note: Desktop Video SDK 10.9.11 does not have a MIT license. License information is provided when downloading the sdk.

- Extract the SDK anywhere on your computer.
- Update the DeckLink_inc user macro in [dependencies.props](SpectatorView.Compositor/dependencies.props) with the corresponding path on your computer.
- Restart Visual Studio

**Elgato Capture Card**
If you are using an Elgato capture card, you will need to clone Elgato's [gamecapture github repo](https://github.com/elgatosf/gamecapture).

- Open a Command Prompt in administrator mode
- Navigate to a folder in which you would like to store your repositories (ex: c:\git)
- git clone <https://github.com/elgatosf/gamecapture>
- Update the Elgato_Filter user macro in [dependencies.props](SpectatorView.Compositor/dependencies.props) with the corresponding path on your computer.
- Restart Visual Studio

**OpenCV**

>Note: SpectatorView.OpenCV.dll introduces dependencies on OpenCV. OpenCV does not have a MIT license. For more information on OpenCV's license, see [here](https://opencv.org/license/).

- **DSLR camera calibration** requires a **Release x64** version of this binary built from the [**SpectatorView.OpenCV.Desktop**](SpectatorView.OpenCV/Desktop/SpectatorView.OpenCV.Desktop.vcxproj) visual studio project.
- **ArUco Marker detection** on a HoloLens 1 device requires a **Release x86** version of this binary built from the [**SpectatorView.OpenCV.UWP**](SpectatorView.OpenCV/UWP/SpectatorView.OpenCV.UWP.vcxproj)

###### 1. Install [Vcpkg](https://github.com/microsoft/vcpkg)

- Open a Command Prompt in administrator mode
- Navigate to a folder in which you would like to store your repositories (ex: c:\git)
- git clone <https://github.com/Microsoft/vcpkg>
- cd vcpkg
- .\bootstrap-vcpkg.bat
- .\vcpkg integrate install

###### 2. Install OpenCV Contrib

For ArUco marker detection, you will need to install a x86 uwp friendly version of opencv. For DSLR camera calibration, you will need to install a x64 desktop friendly version of opencv.

- .\vcpkg install opencv[contrib]:x86-uwp --recurse
- .\vcpkg install opencv[contrib]:x64-windows --recurse

>NOTE: Copy the above lines exactly (the []s do not indicate an optional value).

### 2. Build the plugins

Building the SpectatorView.Native solution for each architecture will produce the correct required binaries for each platform. Note that not all binaries will build on every architecture.
- Open [SpectatorView.Native.sln](SpectatorView.Native.sln) in Visual Studio.
- Build a **Release x64** version of the solution.
- Build a **Release x86** version of the solution.
- Build a **Release ARM** version of the solution.


### 3. Troubleshooting build issues

#### Installing OpenCV Contrib for UWP failed

When encountering issues with vcpkg, the most up to date information will be found in the [vcpkg project](https://github.com/microsoft/vcpkg). Searching for specific errors in the [vcpkg issues list](https://github.com/microsoft/vcpkg/issues) will be the quickest way to find potential workarounds.
>NOTE: When trying other vcpkgs commits, you may end up with a different version of opencv getting installed to your development machine. This will likely require updating the opencv lib dependencies as described below.

#### OpenCV header/dll is not found

If installing opencv with vcpkg succeeded, a few things could still occur that prevent SpectatorViewPlugin's from referencing the opencv libs/dlls correctly. Try the following:

- Restart Visual Studio. If SpectatorViewPlugin.sln was opened in visual studio prior to installing the opencv for uwp components, visual studio may not have correctly resolved needed environment variables. Closing and reopening visual studio should result in these environment variable paths resolving correctly.
- Ensure that the opencv lib dependencies declared in the SpectatorViewPlugin project have the correct version number. Vcpkg will periodically move to installing newer versions of opencv. If you right click on your SpectatorViewPlugin project in visual studio's solution explorer, you can then open the project properties dialogue. Look at Linker->Input to see what specific opencv libs are referenced by the project. For OpenCV 3.4.3, you will need to make sure that libs end in *343.lib*. Older versions of OpenCV, such as 3.4.1, have dlls ending in *341.lib*

## 4. Adding compiled binaries to SpectatorView.Unity

After compiling the above binaries, run [CopyPluginsToUnity.bat](../../tools/Scripts/CopyPluginsToUnity.bat) to add said binaries to the SpectatorView.Unity project. This script will also add .meta files for the binaries to the Unity project.
>Note: The Unity editor does not currently dynamically unload binaries. Errors may occur when trying to copy binaries into your Unity project if the unity editor has loaded said binaries. If errors are encountered with this script, close your Unity editor and try again.
