// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

namespace Microsoft.Azure.SpatialAnchors.Unity
{
    public static class CustomSpatialAnchorManagerExtensions
    {
        /// <summary>
        /// Extension method that allows adding a SpatialAnchorManager with a custon SpatialAnchorConfig to a GameObject
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="spatialAnchorConfig"></param>
        /// <returns></returns>
        public static SpatialAnchorManager AddCustomSpatialAnchorManager(this GameObject gameObject, SpatialAnchorConfig spatialAnchorConfig)
        {
            gameObject.SetActive(false);
            CustomSpatialAnchorManager spatialAnchorManager = gameObject.AddComponent<CustomSpatialAnchorManager>();
            spatialAnchorManager.ApplyCustomConfiguration(spatialAnchorConfig);
            gameObject.SetActive(true);
            return spatialAnchorManager;
        }
    }

    public class CustomSpatialAnchorManager : SpatialAnchorManager
    {
        /// <summary>
        /// Call to apply a custom SpatialAnchorConfig to the SpatialAnchorManager
        /// </summary>
        /// <param name="config"></param>
        public void ApplyCustomConfiguration(SpatialAnchorConfig config)
        {
            if (config != null)
            {
                authenticationMode = config.AuthenticationMode;
                spatialAnchorsAccountId = config.SpatialAnchorsAccountId;
                spatialAnchorsAccountKey = config.SpatialAnchorsAccountKey;
                clientId = config.ClientId;
                tenantId = config.TenantId;
            }
            else
            {
                Debug.LogError("CustomSpatialAnchorManager was provided a null SpatialAnchorConfig.");
            }
        }
    }
}
