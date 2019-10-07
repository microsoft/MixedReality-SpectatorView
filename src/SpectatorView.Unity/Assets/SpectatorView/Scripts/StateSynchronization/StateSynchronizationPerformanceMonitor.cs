// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal enum StateSynchronizationPerformanceFeature : short
    {
        GameObjectComponentCheck = 0,
        MaterialPropertyUpdate = 1,
        MaterialPropertyBlockUpdate = 2,
        TransformBroadcasterUpdate = 3,
        DynamicGameObjectHierarchyBroadcasterUpdate = 4,
        TextBroadcasterUpdate = 5,
        RectMask2DBroadcasterUpdate = 6,
        MaskBroadcasterUpdate = 7,
        LightBroadcasterUpdate = 8,
        ImageBroadcasterUpdate = 9,
        CanvasRendererBroadcasterUpdate = 10,
        CanvasGroupBroadcasterUpdate = 11,
        CanvasBroadcasterUpdate = 12,
        CameraBroadcasterUpdate = 13,
        AudioSourceBroadcasterUpdate = 14,
        RendererBroadcasterUpdate = 15,
        TextMeshProBaseUpdate = 16,
        Unknown = 17,
        Count = 18
    }

    internal class StateSynchronizationPerformanceMonitor : Singleton<StateSynchronizationPerformanceMonitor>
    {
        private const int PeriodsToAverageOver = 5;
        private const int FeatureCount = (int)StateSynchronizationPerformanceFeature.Unknown;
        private Stopwatch[] stopwatches;
        private float[][] previousSpentTimes;
        private float[][] previousActualTimes;
        private int currentPeriod = 0;
        private Dictionary<string, int> propertyUpdateCount = new Dictionary<string, int>();
        private StateSynchronizationPerformanceParameters performanceParameters = null;
        private int materialsUpdatedCount = 0;

        protected override void Awake()
        {
            base.Awake();

            stopwatches = new Stopwatch[FeatureCount];
            previousSpentTimes = new float[PeriodsToAverageOver][];
            previousActualTimes = new float[PeriodsToAverageOver][];
            for (int i = 0; i < FeatureCount; i++)
            {
                stopwatches[i] = new Stopwatch();
            }

            for (int i = 0; i < PeriodsToAverageOver; i++)
            {
                previousSpentTimes[i] = new float[FeatureCount];
                previousActualTimes[i] = new float[FeatureCount];
            }
        }

        public void RegisterPerformanceParameters(StateSynchronizationPerformanceParameters parameters)
        {
            if (performanceParameters != null)
            {
                UnityEngine.Debug.LogWarning("Multiple StateSynchronizationPerformanceParameters attempted to register with the StateSynchronizationPerformanceMonitor.");
            }

            performanceParameters = parameters;
        }

        public void UnregisterPerformanceParameters(StateSynchronizationPerformanceParameters parameters)
        {
            if (parameters != null)
            {
                UnityEngine.Debug.LogWarning("Attempted to unregister performance parameters that weren't being used.");
                return;
            }

            performanceParameters = null;
        }

        public IDisposable MeasureScope(StateSynchronizationPerformanceFeature feature)
        {
            return new TimeScope(stopwatches[(byte)feature]);
        }

        public void FlagMaterialPropertyUpdated(string materialName, string shaderName, string propertyName)
        {
            if (!performanceParameters.EnableDiagnosticPerformanceReporting)
            {
                return;
            }

            string key = $"{materialName}.{shaderName}.{propertyName}";
            if (!propertyUpdateCount.ContainsKey(key))
            {
                propertyUpdateCount.Add(key, 0);
            }

            propertyUpdateCount[key]++;
        }

        public void FlagMaterialsUpdated()
        {
            if (!performanceParameters.EnableDiagnosticPerformanceReporting)
            {
                return;
            }

            materialsUpdatedCount++;
        }

        public void WriteMessage(BinaryWriter message)
        {
            if (performanceParameters != null)
            {
                message.Write(performanceParameters.EnableDiagnosticPerformanceReporting);
            }
            else
            {
                message.Write(false);
            }

            for (int i = 0; i < currentPeriod && i < PeriodsToAverageOver - 1; i++)
            {
                for (int j = 0; j < FeatureCount; j++)
                {
                    previousSpentTimes[i + 1][j] = previousSpentTimes[i][j];
                    previousActualTimes[i + 1][j] = previousActualTimes[i][j];
                }
            }
            currentPeriod++;
            for (int i = 0; i < FeatureCount; i++)
            {
                previousSpentTimes[0][i] = (float)stopwatches[i].Elapsed.TotalMilliseconds;
                previousActualTimes[0][i] = Time.time;
            }

            if (currentPeriod > 1)
            {
                int targetPeriodSlot = Math.Min(currentPeriod, PeriodsToAverageOver - 1);
                message.Write(FeatureCount);
                for (int i = 0; i < FeatureCount; i++)
                {
                    float spentTimeDelta = previousSpentTimes[0][i] - previousSpentTimes[targetPeriodSlot][i];
                    float actualTimeDelta = previousActualTimes[0][i] - previousActualTimes[targetPeriodSlot][i];
                    message.Write(spentTimeDelta / actualTimeDelta);
                }
            }
            else
            {
                message.Write(FeatureCount);
                for (int i = 0; i < FeatureCount; i++)
                {
                    message.Write(0.0f);
                }
            }

            message.Write(propertyUpdateCount.Count);
            foreach(var propertyUpdateCountPair in propertyUpdateCount)
            {
                message.Write($"{propertyUpdateCountPair.Key}:{propertyUpdateCountPair.Value}");
            }
            propertyUpdateCount.Clear();

            message.Write(materialsUpdatedCount);
            materialsUpdatedCount = 0;
        }

        public static bool TryReadMessage(BinaryReader reader, out bool diagnosticModeEnabled, out int featureCount, ref double[] averageTimePerFeature, out IReadOnlyList<string> updatedPropertyDetails, out int materialUpdates)
        {
            diagnosticModeEnabled = reader.ReadBoolean();
            featureCount = reader.ReadInt32();

            if (averageTimePerFeature == null ||
                averageTimePerFeature.Length != featureCount)
            {
                averageTimePerFeature = new double[featureCount];
            }

            for (int i = 0; i < featureCount; i++)
            {
                averageTimePerFeature[i] = reader.ReadSingle();
            }

            updatedPropertyDetails = null;
            int updatedPropertyDetailCount = reader.ReadInt32();
            if (updatedPropertyDetailCount != 0)
            {
                List<string> detailsList = new List<string>();
                for (int i = 0; i < updatedPropertyDetailCount; i++)
                {
                    detailsList.Add(reader.ReadString());
                }

                updatedPropertyDetails = detailsList;
            }

            materialUpdates = reader.ReadInt32();

            return true;
        }

        public void SetDiagnosticMode(bool enabled)
        {
            if (performanceParameters != null)
            {
                performanceParameters.EnableDiagnosticPerformanceReporting = enabled;
            }
        }

        private struct TimeScope : IDisposable
        {
            private Stopwatch stopwatch;

            public TimeScope(Stopwatch stopwatch)
            {
                this.stopwatch = stopwatch;
                stopwatch.Start();
            }

            public void Dispose()
            {
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    stopwatch = null;
                }
            }
        }
    }
}