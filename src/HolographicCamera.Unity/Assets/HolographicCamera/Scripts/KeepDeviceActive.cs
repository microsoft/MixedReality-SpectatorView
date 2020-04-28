// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

#if UNITY_WSA && WINDOWS_UWP
    using System;
    using Windows.System.Display;
#endif

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// This class creates a UWP/WSA DisplayRequest in order to prevent Windows devices from entering inactive states.
    /// For Holographic Camera, this functionality allows the HoloLens device to remain on when it is mounted to the camera.
    /// Without this class, the HoloLens would enter an inactive state due to a lack of motion, which in turn can break filming.
    /// </summary>
    public class KeepDeviceActive : MonoBehaviour
    {
#if UNITY_WSA && WINDOWS_UWP
        private static DisplayRequest displayRequest;

        private void Start()
        {
            CreateDisplayRequest();
        }

        private void OnDestroy()
        {
            UnityEngine.WSA.Application.InvokeOnUIThread(() =>
            {
                if (displayRequest != null)
                {
                    try
                    {
                        displayRequest.RequestRelease();
                        Debug.Log("Display request released.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error releasing display request: {e.Message}");
                    }

                    displayRequest = null;
                }
            }, true);
        }

        private void CreateDisplayRequest()
        {
            UnityEngine.WSA.Application.InvokeOnUIThread(() =>
            {
                if (displayRequest == null)
                {
                    try
                    {
                        displayRequest = new DisplayRequest();
                        if (displayRequest != null)
                        {
                            // This call activates a display-required request. If successful,  
                            // the screen is guaranteed not to turn off automatically due to user inactivity. 
                            displayRequest.RequestActive();
                            Debug.Log("Display request activated. Device won't go inactive until closing this scene.");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error creating display request: {e.Message}");
                    }
                }
            }, true);
        }
#endif
    }
}
