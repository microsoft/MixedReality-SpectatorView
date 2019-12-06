// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Microsoft.MixedReality.SpectatorView
{
    internal class AudioSourceService : ComponentBroadcasterService<AudioSourceService, AudioSourceObserver>, IAssetCache
    {
        public static readonly ShortID ID = new ShortID("AUD");

        public override ShortID GetID() { return ID; }

        private const int DSPBufferSize = 1024;
        private const AudioSpeakerMode SpeakerMode = AudioSpeakerMode.Stereo;

        private AudioClipAssetCache audioClipAssets;
        private AudioMixerGroupAssetCache audioMixerGroupAssets;

        private void Start()
        {
            audioClipAssets = AudioClipAssetCache.LoadAssetCache<AudioClipAssetCache>();
            audioMixerGroupAssets = AudioMixerGroupAssetCache.LoadAssetCache<AudioMixerGroupAssetCache>();
            StateSynchronizationSceneManager.Instance.RegisterService(this, new ComponentBroadcasterDefinition<AudioSourceBroadcaster>(typeof(AudioSource)));
        }

        public AssetId GetAudioClipId(AudioClip clip)
        {
            return audioClipAssets?.GetAssetId(clip) ?? AssetId.Empty;
        }

        public AudioClip GetAudioClip(AssetId assetId)
        {
            return audioClipAssets?.GetAsset(assetId);
        }

        public AssetId GetAudioMixerGroupId(AudioMixerGroup group)
        {
            return audioMixerGroupAssets?.GetAssetId(group) ?? AssetId.Empty;
        }

        public AudioMixerGroup GetAudioMixerGroup(AssetId assetId)
        {
            return audioMixerGroupAssets?.GetAsset(assetId);
        }

        public void UpdateAssetCache()
        {
            AudioClipAssetCache.GetOrCreateAssetCache<AudioClipAssetCache>().UpdateAssetCache();
            AudioMixerGroupAssetCache.GetOrCreateAssetCache<AudioMixerGroupAssetCache>().UpdateAssetCache();
        }

        public void ClearAssetCache()
        {
            AudioClipAssetCache.GetOrCreateAssetCache<AudioClipAssetCache>().ClearAssetCache();
            AudioMixerGroupAssetCache.GetOrCreateAssetCache<AudioMixerGroupAssetCache>().ClearAssetCache();
        }
    }
}