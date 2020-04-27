// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

#if UNITY_WSA && WINDOWS_UWP
    using System;
    using Windows.System.Display;
#endif

public class KeepDeviceActive : MonoBehaviour
{
#if UNITY_WSA && WINDOWS_UWP
    private static DisplayRequest displayRequest;
#endif

    private void Start()
    {
        CreateDisplayRequest();
    }

    private void OnDestroy()
    {
#if UNITY_WSA && WINDOWS_UWP
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
        }
#endif
    }

    private void CreateDisplayRequest()
    {
#if UNITY_WSA && WINDOWS_UWP
        UnityEngine.WSA.Application.InvokeOnUIThread(() =>
        {
            if (displayRequest == null)
            {
                try
                {
                    displayRequest = new DisplayRequest();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error creating display request: {e.Message}");
                }
            }

            if (displayRequest != null)
            {
                try
                {
                        // This call activates a display-required request. If successful,  
                        // the screen is guaranteed not to turn off automatically due to user inactivity. 
                        displayRequest.RequestActive();
                    Debug.Log("Display request activated. Device won't go inactive until closing this scene.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Display request failed: {e.Message}");
                }
            }
        }, true);
#endif
    }
}
