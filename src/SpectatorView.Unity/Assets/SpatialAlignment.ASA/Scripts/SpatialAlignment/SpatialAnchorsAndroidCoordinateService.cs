// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_ANDROID && SPATIALALIGNMENT_ASA
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity.Android;
using Microsoft.MixedReality.SpectatorView;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.SpatialAlignment
{
    /// <summary>
    /// Android implementation of the Azure Spatial Anchors coordinate service.
    /// </summary>
    internal class SpatialAnchorsAndroidCoordinateService : SpatialAnchorsCoordinateService
    {
        private long lastFrameProcessedTimeStamp;
        private static object initializeLock = new object();
        private static TaskCompletionSource<object> initializeCompletionSource = null;

        /// <summary>
        /// Instantiates a new <see cref="SpatialAnchorsAndroidCoordinateService"/>.
        /// </summary>
        /// <param name="spatialAnchorsConfiguration">Azure Spatial Anchors configuration.</param>
        public SpatialAnchorsAndroidCoordinateService(SpatialAnchorsConfiguration spatialAnchorsConfiguration)
            : base(spatialAnchorsConfiguration)
        {
        }

        /// <inheritdoc/>
        protected override Task OnInitializeAsync()
        {
            lock (initializeLock)
            {
                if (initializeCompletionSource != null)
                {
                    Debug.Log("SpatialAnchorsAndroidCoordinateService: initializeCompletionSource already initialized");
                    return initializeCompletionSource.Task;
                }

                initializeCompletionSource = new TaskCompletionSource<object>();
            }

            UnityAndroidHelper.Instance.DispatchUiThread(unityActivity =>
            {
                try
                {
                    // We should only run the java initialization once
                    using (AndroidJavaClass cloudServices = new AndroidJavaClass("com.microsoft.CloudServices"))
                    {
                        cloudServices.CallStatic("initialize", unityActivity);
                        Debug.Log("SpatialAnchorsAndroidCoordinateService: session successfully initialized");
                        initializeCompletionSource.SetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Exception throw initializing SpatialAnchorAndroidCoordinateService: {ex.ToString()}");
                    initializeCompletionSource.SetException(ex);
                }
            });

            return initializeCompletionSource.Task;
        }

        /// <inheritdoc/>
        protected override void OnConfigureSession(CloudSpatialAnchorSession session)
        {
            session.Session = GoogleARCoreInternal.ARCoreAndroidLifecycleManager.Instance.NativeSession.SessionHandle;
        }

        /// <inheritdoc/>
        protected override void OnFrameUpdate()
        {
            if (!IsTracking || session == null)
            {
                return;
            }

            GoogleARCoreInternal.NativeSession nativeSession = GoogleARCoreInternal.ARCoreAndroidLifecycleManager.Instance.NativeSession;

            if (nativeSession.FrameHandle == IntPtr.Zero)
            {
                return;
            }

            long latestFrameTimeStamp = nativeSession.FrameApi.GetTimestamp();

            bool newFrameToProcess = latestFrameTimeStamp > lastFrameProcessedTimeStamp;

            if (newFrameToProcess)
            {
                session.ProcessFrame(nativeSession.FrameHandle);
                lastFrameProcessedTimeStamp = latestFrameTimeStamp;
            }
        }
    }
}
#endif