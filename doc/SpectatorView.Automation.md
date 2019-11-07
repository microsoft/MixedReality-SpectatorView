# Building with the command line

## Building Native Plugins via cmd
1) Install [NuGet](https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools)
2) Add `nuget.exe` to your `PATH` variable 
    1) Go to `Edit the system environment variables -> Advanced -> Environment Variables`
    2) Add the directory containing nuget.exe to your User variables `Path` or the System variables `Path`
3) Restart any command windows to pick up your new `Path` definition
4) Add your downloaded `Blackmagic DeckLink SDK 10.9.11` folder to a `external\dependencies\BlackmagicDesign` folder in your MixedReality-SpectatorView repo clone.
5) Run `tools\ci\scripts\buildNativeProjectLocal.bat`

## Building Unity Projects via cmd
1) Run `tools\ci\scripts\buildUnityProjectLocal.bat`
> Note: If you have updated your local SpectatorView.Example.Unity project to contain additional preprocessor directives (for example, STATESYNC_TEXTMESHPRO, etc) and the code no longer compiles upon opening the project, this buildUnityProjectLocal.bat will fail.

# Running tests

## Running PlayMode Tests
1) Open the SpectatorView.Example.Unity project in Unity
2) Open the Build Settings `(File -> Build Settings)` and include the `SpectatorViewCompositor` scene in your build
3) Open the Test Runner Window `(Window -> General -> Test Runner)`
4) Select `Run All` for the PlayMode tests
