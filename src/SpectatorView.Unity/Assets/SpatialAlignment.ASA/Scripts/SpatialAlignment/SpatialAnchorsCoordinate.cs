// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Azure.SpatialAnchors;
using UnityEngine;

#if UNITY_ANDROID || UNITY_IOS
using UnityEngine.XR.ARFoundation;
#endif

namespace Microsoft.MixedReality.SpatialAlignment
{
    /// <summary>
    /// The Spatial Coordinate based on Azure Spatial Anchors service.
    /// </summary>
    internal class SpatialAnchorsCoordinate : SpatialCoordinateUnityBase<string>
    {
        private readonly GameObject anchorGO;

        /// <summary>
        /// The associated <see cref="CloudSpatialAnchor"/>.
        /// </summary>
        public CloudSpatialAnchor CloudSpatialAnchor { get; }

        // TODO anborod this should be updated from the cloud session, but in our case while it's created it's technically located
        /// <inheritdoc/>
        public override LocatedState State => LocatedState.Tracking;

        private Matrix4x4 CoordinateTransform
        {
            get
            {
#if UNITY_ANDROID || UNITY_IOS
                if (Camera.main == null)
                {
                    Debug.LogError("Camera.main was not set for this application. The used SpatialAnchorsCoordinate will have an invalid transform.");
                    return this.anchorGO.transform.localToWorldMatrix;
                }

                Matrix4x4 cameraParentTransform = Camera.main.transform.parent == null ? Matrix4x4.identity : Camera.main.transform.parent.localToWorldMatrix;
                Matrix4x4 anchorTransform = this.anchorGO.transform.localToWorldMatrix;
                return cameraParentTransform.inverse * anchorTransform;
#else
                return this.anchorGO.transform.localToWorldMatrix;
#endif
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="SpatialAnchorsCoordinate"/>.
        /// </summary>
        /// <param name="cloudSpatialAnchor">The associated <see cref="CloudSpatialAnchor"/> to use for creation.</param>
        /// <param name="anchorGO">The <see cref="GameObject"/> representing this anchor.</param>
        public SpatialAnchorsCoordinate(CloudSpatialAnchor cloudSpatialAnchor, GameObject anchorGO)
            : base(cloudSpatialAnchor.Identifier)
        {
            this.CloudSpatialAnchor = cloudSpatialAnchor;
            this.anchorGO = anchorGO;
        }

        /// <inheritdoc/>
        protected override Vector3 CoordinateToWorldSpace(Vector3 vector)
        {
            return CoordinateTransform.MultiplyPoint(vector);
        }

        /// <inheritdoc/>
        protected override Quaternion CoordinateToWorldSpace(Quaternion quaternion)
        {
            return CoordinateTransform.rotation * quaternion;
        }

        /// <inheritdoc/>
        protected override Vector3 WorldToCoordinateSpace(Vector3 vector)
        {
            return CoordinateTransform.inverse.MultiplyPoint(vector);
        }

        /// <inheritdoc/>
        protected override Quaternion WorldToCoordinateSpace(Quaternion quaternion)
        {
            return CoordinateTransform.inverse.rotation * quaternion;
        }

        /// <inheritdoc/>
        public void Destroy()
        {
            UnityEngine.Object.Destroy(anchorGO);
        }
    }
}