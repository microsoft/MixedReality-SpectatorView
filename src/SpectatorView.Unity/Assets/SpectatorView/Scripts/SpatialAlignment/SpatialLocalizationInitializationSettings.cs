using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class SpatialLocalizationInitializationSettings : Singleton<SpatialLocalizationInitializationSettings>
    {
        [SerializeField]
        private SpatialLocalizationInitializer[] prioritizedInitializers = null;

        public SpatialLocalizationInitializer[] PrioritizedInitializers => prioritizedInitializers;
    }
}