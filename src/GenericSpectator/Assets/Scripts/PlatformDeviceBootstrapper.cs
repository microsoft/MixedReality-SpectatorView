using UnityEngine;

public class PlatformDeviceBootstrapper : MonoBehaviour
{
    /// <summary>
    /// The device prefab for Windows platforms.
    /// </summary>
    [Tooltip("The device prefab for Windows platforms.")]
    [SerializeField]
    protected GameObject windowsDevicePrefab;

    /// <summary>
    /// The device prefab for the Android platform.
    /// </summary>
    [Tooltip("The device prefab for the Android platform.")]
    [SerializeField]
    protected GameObject androidDevicePrefab;

    /// <summary>
    /// The device prefab for the iOS platform.
    /// </summary>
    [Tooltip("The device prefab for the iOS platform.")]
    [SerializeField]
    protected GameObject iOSDevicePrefab;

    protected void Awake()
    {
        GameObject devicePrfab;

#if UNITY_WSA || UNITY_STANDALONE_WIN
        devicePrfab = windowsDevicePrefab;
#elif UNITY_ANDROID
        devicePrfab = androidDevicePrefab;
#elif UNITY_IOS
        devicePrfab = iOSDevicePrefab;
#else
        Debug.LogError($"There is no device prefab for the current build platform: {Application.platform}.  Please select a different build platform or add a device prefab for this one.", this);
        return;
#endif

        if (devicePrfab == null)
        {
            Debug.LogError($"The device prefab isn't set for the current build platform. Please select a different build platform or set the device prefab.", this);
            return;
        }

        Instantiate(devicePrfab, transform);
    }
}
