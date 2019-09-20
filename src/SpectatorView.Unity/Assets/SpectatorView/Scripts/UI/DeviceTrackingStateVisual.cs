// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    public class DeviceTrackingStateVisual : MobileOverlayVisualChild
    {
        [Tooltip("Text that displays the AR/VR Device tracking state.")]
        [SerializeField]
        private Text deviceTrackingStateText = null;

        private TrackingState cachedTrackingState = TrackingState.Unknown;
        private const string deviceTrackingPrompt = "Device tracking state:";
        private readonly Dictionary<TrackingState, string> promptDictionary = new Dictionary<TrackingState, string>
        {
            {TrackingState.Tracking, "Tracking"},
            {TrackingState.LostTracking, "Lost Tracking"},
            {TrackingState.Unknown, "Unknown"}
        };
        private readonly Dictionary<TrackingState, Color> colorDictionary = new Dictionary<TrackingState, Color>
        {
            {TrackingState.Tracking, Color.green},
            {TrackingState.LostTracking, Color.red},
            {TrackingState.Unknown, Color.yellow}
        };

        private void Update()
        {
            if (deviceTrackingStateText != null &&
                SpatialCoordinateSystemManager.IsInitialized &&
                SpatialCoordinateSystemManager.Instance.TrackingState != cachedTrackingState)
            {
                cachedTrackingState = SpatialCoordinateSystemManager.Instance.TrackingState;
                if (!promptDictionary.TryGetValue(cachedTrackingState, out var stateText))
                {
                    Debug.LogError($"Tracking state not supported by DeviceTrackingStateVisual promptDictionary: {cachedTrackingState}");
                    stateText = promptDictionary[TrackingState.Unknown];
                }

                deviceTrackingStateText.text = $"{deviceTrackingPrompt} {stateText}";

                if (!colorDictionary.TryGetValue(cachedTrackingState, out var stateColor))
                {
                    Debug.LogError($"Tracking state not supported by DeviceTrackingStateVisual colorDictionary: {cachedTrackingState}");
                    stateColor = colorDictionary[TrackingState.Unknown];
                }

                deviceTrackingStateText.color = stateColor;
            }
        }
    }
}