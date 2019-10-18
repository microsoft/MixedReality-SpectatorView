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
        private static bool initialized = false;

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
            if (initialized)
            {
                Debug.Log("SpatialAnchorsAndroidCoordinateService: session already initialized");
                return Task.CompletedTask;
            }

            TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();

            UnityAndroidHelper.Instance.DispatchUiThread(unityActivity =>
            {
                if (initialized)
                {
                    Debug.Log("SpatialAnchorsAndroidCoordinateService: session already initialized");
                    taskCompletionSource.SetResult(null);
                    return;
                }

                try
                {
                    // We should only run the java initialization once
                    using (AndroidJavaClass cloudServices = new AndroidJavaClass("com.microsoft.CloudServices"))
                    {
                        cloudServices.CallStatic("initialize", unityActivity);
                        Debug.Log("SpatialAnchorsAndroidCoordinateService: session successfully initialized");
                        initialized = true;
                        taskCompletionSource.SetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Exception throw initializing SpatialAnchorAndroidCoordinateService: {ex.ToString()}");
                    taskCompletionSource.SetException(ex);
                }
            });

            return taskCompletionSource.Task;
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