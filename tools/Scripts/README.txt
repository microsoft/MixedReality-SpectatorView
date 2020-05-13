The content included from Microsoft.MixedReality.QR.* and Microsoft.VCRTForwarders.* have in Editor initializer scripts removed.
This will cause these dlls to not work as expected in editor. You will need to deploy to a device to test out QR Code detection.
This was done because the in Editor initializer scripts break Azure Spatial Anchors iOS builds by preventing custom post build steps from running.
This was also done because the in Editor initializer scripts are unable to locate dlls located in Unity packages compared to in the Unity project's Assets folder.