namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Enumerates tracking states for AR/VR Devices.
    /// </summary>
    public enum TrackingState
    {
        /// <summary>
        /// AR/VR Device has tracking.
        /// </summary>
        Tracking,

        /// <summary>
        /// AR/VR Device lost tracking.
        /// </summary>
        LostTracking,

        /// <summary>
        /// AR/VR Device may or may not have tracking.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Classes that implement this interface report whether the application's AR/VR Device is tracking its location in the physical world.
    /// </summary>
    public interface ITrackingObserver
    {
        /// <summary>
        /// Returns the tracking state associated with the application's AR/VR Device.
        /// </summary>
        TrackingState TrackingState { get; }
    }
}