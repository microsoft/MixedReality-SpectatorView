// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class StateSynchronizationPerformanceParameters : MonoBehaviour
    {
        public enum PollingFrequency
        {
            InheritFromParent = 0x0,
            UpdateOnceOnStart = 0x1,
            UpdateContinuously = 0x2
        }

        public enum FeatureInclusionType
        {
            InheritFromParent = 0x0,
            SynchronizeFeature = 0x1,
            DoNotSynchronizeFeature = 0x2,
        }

        [Serializable]
        internal class MaterialPropertyPollingFrequency
        {
            public string shaderName = null;
            public string propertyName = null;
            public PollingFrequency updateFrequency = PollingFrequency.UpdateContinuously;
        }

        [SerializeField]
        [Tooltip("Controls how frequently each GameObject checks for attached components that have a related ComponentBroadcaster.")]
        protected PollingFrequency checkForComponentBroadcasters = PollingFrequency.UpdateContinuously;

        [SerializeField]
        [Tooltip("Controls how frequently the shaderKeywords property on materials are checked for updates.")]
        protected PollingFrequency shaderKeywords = PollingFrequency.UpdateContinuously;

        [SerializeField]
        [Tooltip("Controls how frequently the renderQueue property on materials are checked for updates.")]
        protected PollingFrequency renderQueue = PollingFrequency.UpdateContinuously;

        [SerializeField]
        [Tooltip("Controls how frequently material properties are checked for updates by default. Specific material properties can be changed in the materialPropertyOverrides array.")]
        protected PollingFrequency materialProperties = PollingFrequency.UpdateContinuously;

        [SerializeField]
        [Tooltip("Overrides how frequently specific material properties are updated by shader and property name. Shader properties not listed here are updated at a frequency controlled by the materialProperties value.")]
        protected MaterialPropertyPollingFrequency[] materialPropertyOverrides = null;

        [SerializeField]
        [Tooltip("Controls whether or not MaterialPropertyBlocks on Renderers are synchronized.")]
        protected FeatureInclusionType materialPropertyBlocks = FeatureInclusionType.SynchronizeFeature;

        private static GameObject emptyParametersGameObject;
        private StateSynchronizationPerformanceParameters parentParameters;
        private Dictionary<MaterialPropertyKey, MaterialPropertyPollingFrequency> pollingFrequencyByMaterialProperty;
        private readonly string performanceComponentName = "StateSynchronizationPerformanceParameters";

        private PollingFrequency? cachedCheckForComponentBroadcasters = null;
        private PollingFrequency? cachedShaderKeywords = null;
        private PollingFrequency? cachedRenderQueue = null;
        private PollingFrequency? cachedMaterialProperties = null;
        private FeatureInclusionType? cachedMaterialPropertyBlocks = null;

        public static bool EnablePerformanceReporting
        {
            get
            {
                if (BroadcasterSettings.IsInitialized &&
                    BroadcasterSettings.Instance.ForcePerformanceReporting)
                {
                    return true;
                }

                return enablePerformanceReporting;
            }
            set
            {
                enablePerformanceReporting = value;
            }
        }
        private static bool enablePerformanceReporting = false;

        /// <summary>
        /// Returns true if custom material property polling frequencies are defined for this instance of StateSynchronizationPerformanceParameters
        /// </summary>
        public bool HasCustomMateriaPropertyPollingFrequencies => (PollingFrequencyByMaterialProperty.Count > 0);
        private IDictionary<MaterialPropertyKey, MaterialPropertyPollingFrequency> PollingFrequencyByMaterialProperty
        {
            get
            {
                return pollingFrequencyByMaterialProperty ?? (pollingFrequencyByMaterialProperty = (materialPropertyOverrides ?? Array.Empty<MaterialPropertyPollingFrequency>()).ToDictionary(p => new MaterialPropertyKey(p.shaderName, p.propertyName)));
            }
        }

        private T GetInheritedProperty<T>(Func<StateSynchronizationPerformanceParameters, T> getter, T defaultValue)
        {
            using (StateSynchronizationPerformanceMonitor.Instance.MeasureEventDuration(performanceComponentName, "GetInheritedProperty"))
            {
                StateSynchronizationPerformanceParameters parameters = this;
                while (parameters != null)
                {
                    T pollingFrequency = getter(parameters);
                    if (!Equals(pollingFrequency, PollingFrequency.InheritFromParent))
                    {
                        return pollingFrequency;
                    }

                    parameters = parameters.parentParameters;
                }

                return defaultValue;
            }
        }

        private PollingFrequency GetInheritedProperty(ref PollingFrequency? cachedValue, Func<StateSynchronizationPerformanceParameters, PollingFrequency> getter, PollingFrequency defaultValue)
        {
            if (cachedValue == null)
            {
                cachedValue = GetInheritedProperty(getter, defaultValue);
            }

            return cachedValue.Value;
        }

        private FeatureInclusionType GetInheritedProperty(ref FeatureInclusionType? cachedValue, Func<StateSynchronizationPerformanceParameters, FeatureInclusionType> getter, FeatureInclusionType defaultValue)
        {
            if (cachedValue == null)
            {
                cachedValue = GetInheritedProperty(getter, defaultValue);
            }

            return cachedValue.Value;
        }

        public PollingFrequency CheckForComponentBroadcasters
        {
            get { return GetInheritedProperty(ref cachedCheckForComponentBroadcasters, p => p.checkForComponentBroadcasters, PollingFrequency.UpdateContinuously); }
        }

        public PollingFrequency ShaderKeywords
        {
            get { return GetInheritedProperty(ref cachedShaderKeywords, p => p.shaderKeywords, PollingFrequency.UpdateContinuously); }
        }

        public PollingFrequency RenderQueue
        {
            get { return GetInheritedProperty(ref cachedRenderQueue, p => p.renderQueue, PollingFrequency.UpdateContinuously); }
        }

        public PollingFrequency MaterialProperties
        {
            get { return GetInheritedProperty(ref cachedMaterialProperties, p => p.materialProperties, PollingFrequency.UpdateContinuously); }
        }

        public FeatureInclusionType MaterialPropertyBlocks
        {
            get { return GetInheritedProperty(ref cachedMaterialPropertyBlocks, p => p.materialPropertyBlocks, FeatureInclusionType.SynchronizeFeature); }
        }

        public bool ShouldUpdateMaterialProperty(MaterialPropertyAsset materialProperty)
        {
            using (StateSynchronizationPerformanceMonitor.Instance.MeasureEventDuration(performanceComponentName, "ShouldUpdateMaterialProperty"))
            {
                if (materialProperty.propertyType == MaterialPropertyType.ShaderKeywords)
                {
                    return ShaderKeywords == PollingFrequency.UpdateContinuously;
                }
                else if (materialProperty.propertyType == MaterialPropertyType.RenderQueue)
                {
                    return RenderQueue == PollingFrequency.UpdateContinuously;
                }
                else
                {
                    MaterialPropertyPollingFrequency pollingFrequency;
                    if (PollingFrequencyByMaterialProperty.TryGetValue(new MaterialPropertyKey(materialProperty.ShaderName, materialProperty.propertyName), out pollingFrequency))
                    {
                        switch (pollingFrequency.updateFrequency)
                        {
                            case PollingFrequency.UpdateContinuously:
                                return true;
                            case PollingFrequency.UpdateOnceOnStart:
                                return false;
                        }
                    }

                    // If we have a parent, check the parent to see if the parent has an explicit override list
                    if (materialProperties != PollingFrequency.InheritFromParent || parentParameters == null)
                    {
                        return materialProperties == PollingFrequency.UpdateContinuously;
                    }
                }
            }

            // Stop the timer before calling parent function
            return parentParameters.ShouldUpdateMaterialProperty(materialProperty);
        }

        protected virtual void Awake()
        {
            UpdateParentParameters();
        }

        private void OnTransformParentChanged()
        {
            UpdateParentParameters();
        }

        private void UpdateParentParameters()
        {
            StateSynchronizationPerformanceMonitor.Instance.IncrementEventCount(performanceComponentName, "UpdateParentParameters");
            if (GetComponent<DefaultStateSynchronizationPerformanceParameters>() != null)
            {
                parentParameters = null;
            }
            else
            {
                if (transform.parent == null)
                {
                    parentParameters = DefaultStateSynchronizationPerformanceParameters.Instance;
                }
                else
                {
                    parentParameters = transform.parent.GetComponentInParent<StateSynchronizationPerformanceParameters>();
                    if (parentParameters == null)
                    {
                        parentParameters = DefaultStateSynchronizationPerformanceParameters.Instance;
                    }
                }
            }

            cachedCheckForComponentBroadcasters = null;
            cachedShaderKeywords = null;
            cachedRenderQueue = null;
            cachedMaterialProperties = null;
            cachedMaterialPropertyBlocks = null;
        }

    public static StateSynchronizationPerformanceParameters CreateEmpty()
        {
            if (emptyParametersGameObject == null)
            {
                emptyParametersGameObject = new GameObject("EmptySychronizationPerformanceParameters");
            }
            return ComponentExtensions.EnsureComponent<StateSynchronizationPerformanceParameters>(emptyParametersGameObject);
        }
    }
}