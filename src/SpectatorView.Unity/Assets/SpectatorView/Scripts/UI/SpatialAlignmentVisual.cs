// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.SpatialAlignment;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    public class SpatialAlignmentVisual : MonoBehaviour
    {
        [Tooltip("Text used to display experience spatial alignment state.")]
        [SerializeField]
        private Text spatialAlignmentStateText = null;

        private SpectatorView spectatorView;
        private readonly string spatialAlignmentStatePrompt = "Experience spatially aligned:";

        private void Start()
        {
            spectatorView = FindObjectOfType<SpectatorView>();
        }

        private void Update()
        {
            if (spatialAlignmentStateText != null)
            {
                if (SpatialCoordinateSystemManager.IsInitialized &&
                    SpatialCoordinateSystemManager.Instance.AllCoordinatesLocated)
                {
                    spatialAlignmentStateText.text = $"{spatialAlignmentStatePrompt} True";
                    spatialAlignmentStateText.color = Color.green;
                }
                else
                {
                    spatialAlignmentStateText.text = $"{spatialAlignmentStatePrompt} False";
                    spatialAlignmentStateText.color = Color.yellow;
                }
            }
        }

        public void OnResetLocalizationClick()
        {
            if (spectatorView != null)
            {
                spectatorView.TryResetLocalizationAsync().FireAndForget();
            }
            else
            {
                Debug.LogError("SpectatorView was not found in the scene, failed to reset localiation.");
            }
        }
    }
}
