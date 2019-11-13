// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class AssetStateVisual : MonoBehaviour
    {
        [Tooltip("Text used to display asset state.")]
        [SerializeField]
        protected Text assetStateText = null;

        private StateSynchronizationObserver observer;

        protected void Awake()
        {
            if (assetStateText == null)
            {
                Debug.LogError($"{nameof(assetStateText)} is required.", this);
            }
        }

        protected void OnEnable()
        {
            if (observer == null)
            {
                var spectatorView = FindObjectOfType<SpectatorView>();

                observer = spectatorView?.StateSynchronizationObserver;

                if (observer == null)
                {
                    Debug.LogWarning($"{nameof(AssetStateVisual)} couldn't find {nameof(SpectatorView)}.{nameof(spectatorView.StateSynchronizationObserver)}.", this);
                }
            }

            if (observer != null)
            {
                observer.AssetStateChanged += UpdateVisual;
                UpdateVisual(observer.AssetState);
            }
        }

        protected void OnDisable()
        {
            if (observer != null)
            {
                observer.AssetStateChanged -= UpdateVisual;
            }
        }

        private void UpdateVisual(AssetState assetState)
        {
            string newStateText;
            Color newColor;

            switch (assetState.Status)
            {
                case AssetStateStatus.Unknown:
                    newStateText = "Unknown.";
                    newColor = Color.yellow;
                    break;

                case AssetStateStatus.None:
                    newStateText = "None.";
                    newColor = Color.red;
                    break;

                case AssetStateStatus.Preloaded:
                    newStateText = "Loaded built-in assets.";
                    newColor = Color.green;
                    break;

                case AssetStateStatus.RequestingAssetBundle:
                    newStateText = "Asking remote user for updated asset bundle.";

                    if (assetState.AssetBundleDisplayName != null)
                    {
                        newStateText += $" Current bundle: \"{assetState.AssetBundleDisplayName}\".";
                    }

                    newColor = Color.yellow;
                    break;

                case AssetStateStatus.DownloadingAssetBundle:
                    newStateText = $"Downloaded {StateSynchronizationObserver.FormatByteProgress(assetState.BytesSoFar, assetState.TotalBytes)} of bundle \"{assetState.AssetBundleDisplayName}\".";
                    newColor = Color.yellow;
                    break;

                case AssetStateStatus.AssetBundleLoaded:
                    newStateText = $"Loaded \"{assetState.AssetBundleDisplayName}\".";
                    newColor = Color.green;
                    break;

                case AssetStateStatus.NonePreloadedAndNoAssetBundleAvailable:
                    newStateText = "No assets. Consider building asset bundles into the remote user app.";
                    newColor = Color.red;
                    break;

                case AssetStateStatus.ErrorDownloadingAssetBundle:
                    newStateText = $"Error downloading asset bundle \"{assetState.AssetBundleDisplayName}\". Error details: {assetState.ErrorDetails}";
                    newColor = Color.red;
                    break;

                case AssetStateStatus.ErrorLoadingAssetBundle:
                    newStateText = $"Error loading asset bundle \"{assetState.AssetBundleDisplayName}\". Error details: {assetState.ErrorDetails}";
                    newColor = Color.red;
                    break;

                default:
                    Debug.LogError($"Unexpected {nameof(assetState)}.{nameof(assetState.Status)} \"{assetState.Status}\".", this);
                    newStateText = "Unexpected internal error.";
                    newColor = Color.red;
                    break;
            }

            assetStateText.text = $"Assets: {newStateText}";
            assetStateText.color = newColor;
        }
    }
}
