# SpectatorView.Native Dlls

The following Dlls are built out of the [SpectatorView.Native.sln](../src/SpectatorView.Native/SpectatorView.Native.sln).

1. **SpectatorView.Compositor.dll** is needed for DSLR camera calibration and DSLR camera spectating experiences.
2. **SpectatorView.Compositor.UnityPlugin.dll** is needed for DSLR camera calibartion and DSLR camera spectating experiences.
3. **SpectatorView.OpenCV.dll** is used in both ArUco marker detection and DSLR camera calibration.

## SpectatorView.Compositor.dll & SpectatorView.Compositor.UnityPlugin.dll

Coming soon...

## SpectatorView.OpenCV.dll
>Note: SpectatorView.OpenCV.dll introduces dependencies on OpenCV. OpenCV does not have a MIT license. For more information on OpenCV's license, see [here](https://opencv.org/license/). 
* **DSLR camera calibration** requires a **Release x64** version of this binary built from the [**SpectatorView.OpenCV.Desktop**](../src/SpectatorView.Native/SpectatorView.OpenCV/Desktop/SpectatorView.OpenCV.Desktop.vcxproj) visual studio project.
* **ArUco Marker detection** on a HoloLens 1 device requires a **Release x86** version of this binary built from the [**SpectatorView.OpenCV.UWP**](../src/SpectatorView.Native/SpectatorView.OpenCV/UWP/SpectatorView.OpenCV.UWP.vcxproj)

#### Install Vcpkg

- Open a Command Prompt in administrator mode
- Navigate to a folder in which you would like to store your repositories (ex: c:\git)
- git clone <https://github.com/Microsoft/vcpkg>
- cd vcpkg
- git checkout 05b31030cee412118a9710daf5b4652a684b7f50
>NOTE: The above commit was the last tested commit by a contributor to this repo. If the below setup steps fail with this commit, it is likely worth checking if the failure is a known issue for the vcpkg repo. It is also worth attempting checking out the master branch and reattempting the setup steps.
- .\bootstrap-vcpkg.bat
- .\vcpkg integrate install

#### Install OpenCV Contrib

For ArUco marker detection, you will need to install a x86 uwp friendly version of opencv. For DSLR camera calibration, you will need to install a x64 desktop friendly version of opencv.
- .\vcpkg install opencv[contrib]:x86-uwp --recurse
- .\vcpkg install opencv[contrib]:x64-windows --recurse

>NOTE: Copy the above lines exactly (the []s do not indicate an optional value).

#### Building the Plugin

1. Open [SpectatorView.Native.sln](../src/SpectatorView.Native/SpectatorView.Native.sln) in visual studio.
2. Build a **Release x86** version of SpectatorView.OpenCV.dll with [**SpectatorView.OpenCV.UWP**](../src/SpectatorView.Native/SpectatorView.OpenCV/UWP/SpectatorView.OpenCV.UWP.vcxproj)
2. Build a **Release x64** version of SpectatorView.OpenCV.dll with [**SpectatorView.OpenCV.Desktop**](../src/SpectatorView.Native/SpectatorView.OpenCV/Desktop/SpectatorView.OpenCV.Desktop.vcxproj)

## SpectatorView.OpenCV.dll Troubleshooting
#### Installing OpenCV Contrib for UWP failed

The suggested commit above for vcpkg reflects the last locally tested vcpkg commit by a contributor to this repo. When encountering issues with vcpkg, it is likely worth checking out the master branch for the vcpkg repo and repeating the vcpkg related steps. Issues may also already be filed for issues with vcpkg.
>NOTE: When trying other vcpkgs commits, you may end up with a different version of opencv getting installed to your development machine. This will likely require updating the opencv lib dependencies as described below.

#### OpenCV header/dll is not found

If installing opencv with vcpkg succeeded, a few things could still occur that prevent SpectatorViewPlugin's from referencing the opencv libs/dlls correctly. Try the following:

- Restart Visual Studio. If SpectatorViewPlugin.sln was opened in visual studio prior to installing the opencv for uwp components, visual studio may not have correctly resolved needed environment variables. Closing and reopening visual studio should result in these environment variable paths resolving correctly.
- Ensure that the opencv lib dependencies declared in the SpectatorViewPlugin project have the correct version number. Vcpkg will periodically move to installing newer versions of opencv. If you right click on your SpectatorViewPlugin project in visual studio's solution explorer, you can then open the project properties dialogue. Look at Linker->Input to see what specific opencv libs are referenced by the project. For OpenCV 3.4.3, you will need to make sure that libs end in *343.lib*. Older versions of OpenCV, such as 3.4.1, have dlls ending in *341.lib*

# Adding compiled binaries to SpectatorView.Unity
After compiling the above binaries, run [CopyNativeDlls.bat](../tools/Scripts/CopyNativeDlls.bat) to add said binaries to the SpectatorView.Unity project. This script will also add .meta files for the binaries to the Unity project.
>Note: The Unity editor does not currently dynamically unload binaries. Errors may occur when trying to copy binaries into your Unity project if the unity editor has loaded said binaries. If errors are encountered with this script, close your Unity editor and try again.
