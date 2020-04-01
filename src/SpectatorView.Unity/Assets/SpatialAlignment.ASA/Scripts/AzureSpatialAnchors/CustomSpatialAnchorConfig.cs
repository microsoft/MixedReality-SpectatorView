// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.SpatialAnchors.Unity
{
    public class CustomSpatialAnchorConfig : SpatialAnchorConfig
    {
        /// <summary>
        /// Call to apply a custom spatial anchors account id and account key. Note, this call will change the authentication mode to ApiKey
        /// </summary>
        /// <param name="spatialAnchorsAccountId">account id</param>
        /// <param name="spatialAnchorsAccountKey">account key</param>
        public static CustomSpatialAnchorConfig Create(string spatialAnchorsAccountId, string spatialAnchorsAccountKey)
        {
            CustomSpatialAnchorConfig config = CreateInstance<CustomSpatialAnchorConfig>();
            config.authenticationMode = AuthenticationMode.ApiKey;
            config.spatialAnchorsAccountId = spatialAnchorsAccountId;
            config.spatialAnchorsAccountKey = spatialAnchorsAccountKey;
            return config;
        }
    }
}
