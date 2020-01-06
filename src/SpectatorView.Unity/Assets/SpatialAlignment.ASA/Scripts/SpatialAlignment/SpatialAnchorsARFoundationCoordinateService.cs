#if SPATIALALIGNMENT_ASA && (UNITY_ANDROID || UNITY_IOOS)

using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using Microsoft.Azure.SpatialAnchors.Unity.Android;
using Microsoft.MixedReality.SpectatorView;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Microsoft.MixedReality.SpatialAlignment
{
    /// <summary>
    /// AR Foundation implementation of the Azure Spatial Anchors coordinate service.
    /// </summary>
    public class SpatialAnchorsARFoundationCoordinateService : SpatialAnchorsCoordinateService
    {
#if UNITY_ANDROID
        private static bool javaInitialized = false;
#endif // UNITY_ANDROID

        private long lastFrameProcessedTimeStamp;
        private static Dictionary<string, ARReferencePoint> pointerToReferencePoints = new Dictionary<string, ARReferencePoint>();
        private List<AnchorLocatedEventArgs> pendingEventArgs = new List<AnchorLocatedEventArgs>();
        internal static ARReferencePointManager arReferencePointManager = null;
        private ARCameraManager arCameraManager = null;
        private ARSession arSession = null;
        private Camera mainCamera;
        bool isSessionStarted = false;

        public SpatialAnchorsARFoundationCoordinateService(SpatialAnchorsConfiguration spatialAnchorsConfiguration)
            : base(spatialAnchorsConfiguration)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = GameObject.FindObjectOfType<Camera>();
            }

            arCameraManager = GameObject.FindObjectOfType<ARCameraManager>();
            arSession = GameObject.FindObjectOfType<ARSession>();
            arReferencePointManager = GameObject.FindObjectOfType<ARReferencePointManager>();
            if (arReferencePointManager != null)
            {
                arReferencePointManager.referencePointsChanged += ARReferencePointManager_referencePointsChanged;
            }
            else
            {
                Debug.LogWarning("ARReferencePointManager was not found in the Unity scene.");
            }
        }

        private void ARReferencePointManager_referencePointsChanged(ARReferencePointsChangedEventArgs obj)
        {
            lock (pointerToReferencePoints)
            {
                foreach (ARReferencePoint aRReferencePoint in obj.added)
                {
                    string lookupkey = aRReferencePoint.nativePtr.GetPlatformPointer().GetPlatformKey();
                    if (!pointerToReferencePoints.ContainsKey(lookupkey))
                    {
                        pointerToReferencePoints.Add(lookupkey, aRReferencePoint);
                    }
                }

                foreach (ARReferencePoint aRReferencePoint in obj.removed)
                {
                    string toremove = null;
                    foreach (var kvp in pointerToReferencePoints)
                    {
                        if (kvp.Value == aRReferencePoint)
                        {
                            toremove = kvp.Key;
                            break;
                        }
                    }

                    if (toremove != null)
                    {
                        pointerToReferencePoints.Remove(toremove);
                    }
                }

                foreach (ARReferencePoint aRReferencePoint in obj.updated)
                {
                    string lookupKey = aRReferencePoint.nativePtr.GetPlatformPointer().GetPlatformKey();
                    if (!pointerToReferencePoints.ContainsKey(lookupKey))
                    {
                        pointerToReferencePoints.Add(lookupKey, aRReferencePoint);
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override Task OnInitializeAsync()
        {
            return Task.CompletedTask;
            //throw new System.NotImplementedException();
        }

        protected override void OnManagedDispose()
        {
            base.OnManagedDispose();
         
            // Forget about cached ARFoundation reference points
            pointerToReferencePoints.Clear();

            // Stop getting frames
            arCameraManager.frameReceived -= ArCameraManager_frameReceived;
        }

            /// <inheritdoc/>
        protected override Task OnConfigureSession(CloudSpatialAnchorSession session)
        {
#if UNITY_ANDROID // Android Only
            // We should only run the Java initialization once
            if (!javaInitialized)
            {
                // Create a TaskCompletionSource that we can use to know when
                // the Java plugin has completed initialization on the Android
                // thread.
                TaskCompletionSource<bool> pluginInit = new TaskCompletionSource<bool>();

                // Make sure ARCore is running. This code must be executed
                // on a Java thread provided by Android.
                AndroidHelper.Instance.DispatchUiThread(unityActivity =>
                {
                    // Create the plugin
                    using (AndroidJavaClass cloudServices = new AndroidJavaClass("com.microsoft.CloudServices"))
                    {
                        // Initialize the plugin
                        cloudServices.CallStatic("initialize", unityActivity);

                        // Update static variable to say that the plugin has been initialized
                        javaInitialized = true;

                        isSessionStarted = true;

                        // Set the task completion source so the CreateSession method can
                        // continue back on the Unity thread.
                        pluginInit.SetResult(true);
                    }
                });

                // Wait for the plugin to complete initialization on the
                // Java thread.
                return pluginInit.Task;
            }
#endif

            // Ask for ar frames to process
            isSessionStarted = true;
            arCameraManager.frameReceived += ArCameraManager_frameReceived;
            session.Session = arSession.subsystem.nativePtr.GetPlatformPointer();
            return Task.CompletedTask;
        }

        private void ArCameraManager_frameReceived(ARCameraFrameEventArgs obj)
        {
            ProcessLatestFrame();
            ProcessPendingEventArgs();
        }

        /// <summary>
        /// Sends the latest ARFoundation frame to Azure Spatial Anchors
        /// </summary>
        private void ProcessLatestFrame()
        {
            if (!isSessionStarted)
            {
                return;
            }

            var cameraParams = new XRCameraParams
            {
                zNear = mainCamera.nearClipPlane,
                zFar = mainCamera.farClipPlane,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                screenOrientation = Screen.orientation
            };

            XRCameraFrame xRCameraFrame;
            if (arCameraManager.subsystem.TryGetLatestFrame(cameraParams, out xRCameraFrame))
            {
                long latestFrameTimeStamp = xRCameraFrame.timestampNs;

                bool newFrameToProcess = latestFrameTimeStamp > lastFrameProcessedTimeStamp;

                if (newFrameToProcess)
                {
                    session.ProcessFrame(xRCameraFrame.nativePtr.GetPlatformPointer());
                    lastFrameProcessedTimeStamp = latestFrameTimeStamp;
                }
            }
        }

        /// <summary>
        /// Tries to get an ARReference point from an ARkit or Arcore anchor pointer
        /// </summary>
        /// <param name="intPtr">An ARKit or ARcore anchor pointer</param>
        /// <returns>A reference point if found or null</returns>
        internal static ARReferencePoint ReferencePointFromPointer(IntPtr intPtr)
        {
            string key = intPtr.GetPlatformKey();
            if (pointerToReferencePoints.ContainsKey(key))
            {
                return pointerToReferencePoints[key];
            }

            return null;
        }

        /// <summary>
        /// ARFoundation can discover platform anchors *after* ASA has provided the CloudSpatialAnchor to us
        /// When ARFoundation finds the platform anchor (usually within a frame or two) we will call the
        /// anchor located event.
        /// </summary>
        private void ProcessPendingEventArgs()
        {
            if (pendingEventArgs.Count > 0)
            {
                List<AnchorLocatedEventArgs> readyList = new List<AnchorLocatedEventArgs>();
                lock (pendingEventArgs)
                {
                    foreach (AnchorLocatedEventArgs args in pendingEventArgs)
                    {
                        string lookupValue = args.Anchor.LocalAnchor.GetPlatformKey();

                        if (pointerToReferencePoints.ContainsKey(lookupValue))
                        {
                            readyList.Add(args);
                        }
                    }

                    foreach (var ready in readyList)
                    {
                        pendingEventArgs.Remove(ready);
                    }
                }

                if (readyList.Count > 0)
                {
                    foreach (var args in readyList)
                    {
                        //AnchorLocated?.Invoke(this, args);
                    }
                }
            }
        }

        protected override void OnAnchorLocated(object sender, AnchorLocatedEventArgs args)
        {
            //if (AnchorLocated == null)
            //{
            //    return;
            //}
#if UNITY_ANDROID || UNITY_IOS
            // if the anchor was located, wait for ARFoundation to notice the anchor we added
            // before firing the event
            if (args.Status == LocateAnchorStatus.Located)
            {
                lock (pendingEventArgs)
                {
                    pendingEventArgs.Add(args);
                }
            }
            else // otherwise there is no anchor for ARFoundation to find, so just fire the event
#endif
            {
                //AnchorLocated?.Invoke(this, args);
            }
        }
    }
}
#endif