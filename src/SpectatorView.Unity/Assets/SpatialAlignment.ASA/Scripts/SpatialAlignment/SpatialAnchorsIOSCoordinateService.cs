// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_IOS
using Microsoft.Azure.SpatialAnchors;
using System.Threading.Tasks;
using Microsoft.Azure.SpatialAnchors.Unity.IOS.ARKit;
using Microsoft.MixedReality.SpectatorView;
using UnityEngine.XR.iOS;

namespace Microsoft.MixedReality.SpatialAlignment
{
    internal class SpatialAnchorsIOSCoordinateService : SpatialAnchorsCoordinateService
    {
        private UnityARSessionNativeInterface arkitSession;

        public SpatialAnchorsIOSCoordinateService(SpatialAnchorsConfiguration spatialAnchorsConfiguration)
            : base(spatialAnchorsConfiguration)
        {
        }

        protected override Task OnInitializeAsync()
        {
            arkitSession = UnityARSessionNativeInterface.GetARSessionNativeInterface();
            UnityARSessionNativeInterface.ARFrameUpdatedEvent += UnityARSessionNativeInterface_ARFrameUpdatedEvent;
            return Task.CompletedTask;
        }

        private void UnityARSessionNativeInterface_ARFrameUpdatedEvent(UnityARCamera camera)
        {
            if (session != null && IsTracking)
            {
                session.ProcessFrame(arkitSession.GetNativeFramePtr());
            }
        }

        protected override void OnFrameUpdate()
        {
            base.OnFrameUpdate();
        }

        protected override void OnConfigureSession(CloudSpatialAnchorSession session)
        {
           session.Session = arkitSession.GetNativeSessionPtr();
        }
    }
}
#endif
