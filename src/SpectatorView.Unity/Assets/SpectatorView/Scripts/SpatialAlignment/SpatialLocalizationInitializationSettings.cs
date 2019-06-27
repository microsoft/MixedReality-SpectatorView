using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class SpatialLocalizationInitializationSettings : Singleton<SpatialLocalizationInitializationSettings>
    {
        [SerializeField]
        private bool debugLogging = false;

        [SerializeField]
        private SpatialLocalizationInitializer[] prioritizedInitializers = null;

        private bool shouldAutomaticallyLocalize = false;

        public void ConfigureAutomaticLocalization()
        {
            shouldAutomaticallyLocalize = true;
            SpatialCoordinateSystemManager.Instance.ParticipantConnected += OnParticipantConnected;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (shouldAutomaticallyLocalize)
            {
                SpatialCoordinateSystemManager.Instance.ParticipantConnected -= OnParticipantConnected;
            }
        }

        private async void OnParticipantConnected(SpatialCoordinateSystemParticipant participant)
        {
            if (prioritizedInitializers == null || prioritizedInitializers.Length == 0)
            {
                return;
            }

            DebugLog($"Waiting for the set of supported localizers from connected participant {participant.SocketEndpoint.Address}");

            // When a remote participant connects, get the set of ISpatialLocalizers that peer
            // supports. This is asynchronous, as it comes across the network.
            ISet<Guid> peerSupportedLocalizers = await participant.GetPeerSupportedLocalizersAsync();

            // If there are any supported localizers, find the first configured localizer in the
            // list that supports that type. If and when one is found, use it to perform localization.
            if (peerSupportedLocalizers != null)
            {
                DebugLog($"Received a set of {peerSupportedLocalizers.Count} supported localizers");
                for (int i = 0; i < prioritizedInitializers.Length; i++)
                {
                    if (peerSupportedLocalizers.Contains(prioritizedInitializers[i].PeerSpatialLocalizerId))
                    {
                        DebugLog($"Localization initializer {prioritizedInitializers[i].GetType().Name} supported localization with ID {prioritizedInitializers[i].PeerSpatialLocalizerId}, starting localization");
                        prioritizedInitializers[i].RunLocalization(participant);
                        return;
                    }
                }

                DebugLog($"None of the configured LocalizationInitializers were supported by the connected participant, localization will not be started");
            }
            else
            {
                DebugLog($"No supported localizers were received from the participant, localization will not be started");
            }
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                UnityEngine.Debug.Log($"SpatialLocalizationInitializationSettings: {message}");
            }
        }
    }
}