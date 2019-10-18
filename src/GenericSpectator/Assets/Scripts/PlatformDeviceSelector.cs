using UnityEngine;

public class PlatformDeviceSelector : MonoBehaviour
{
    /// <summary>
    /// The device to enable for Windows platforms.
    /// </summary>
    [Tooltip("The device to enable for Windows platforms.")]
    [SerializeField]
    protected GameObject windowsDevice;

    /// <summary>
    /// The device to enable for the Android platform.
    /// </summary>
    [Tooltip("The device to enable for the Android platform.")]
    [SerializeField]
    protected GameObject androidDevice;

    /// <summary>
    /// The device to enable for the iOS platform.
    /// </summary>
    [Tooltip("The device to enable for the iOS platform.")]
    [SerializeField]
    protected GameObject iOSDevice;

    protected void Awake()
    {
        Debug.Assert(windowsDevice?.activeSelf != true, "All devices should be initially disabled.", windowsDevice);
        Debug.Assert(androidDevice?.activeSelf != true, "All devices should be initially disabled.", androidDevice);
        Debug.Assert(iOSDevice?.activeSelf != true, "All devices should be initially disabled.", iOSDevice);

        GameObject deviceToEnable;

#if UNITY_WSA || UNITY_STANDALONE_WIN
        deviceToEnable = windowsDevice;
#elif UNITY_ANDROID
        deviceToEnable = androidDevice;
#elif UNITY_IOS
        deviceToEnable = iOSDevice;
#else
        Debug.LogError($"There is no device setting for the current build platform: {Application.platform}.  Please select a different build platform or add a device setting for this one.", this);
        return;
#endif

        if (deviceToEnable == null)
        {
            Debug.LogError($"The device isn't set for the current build platform. Please select a different build platform or set the device.", this);
            return;
        }

        deviceToEnable.SetActive(true);
    }
}
