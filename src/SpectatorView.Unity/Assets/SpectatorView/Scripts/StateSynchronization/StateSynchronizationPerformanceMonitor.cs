// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class StateSynchronizationPerformanceMonitor : Singleton<StateSynchronizationPerformanceMonitor>
    {
        private const int PeriodsToAverageOver = 5;
        private int currentPeriod = 0;
        private StateSynchronizationPerformanceParameters performanceParameters = null;
        private Dictionary<string, Dictionary<string, Stopwatch>> eventStopWatches = new Dictionary<string, Dictionary<string, Stopwatch>>();
        private Dictionary<string, Dictionary<string, int>> eventCounts = new Dictionary<string, Dictionary<string, int>>();

        public StateSynchronizationPerformanceParameters PerformanceParameters => performanceParameters;

        protected override void Awake()
        {
            base.Awake();
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

        public IDisposable MeasureEventDuration(string componentName, string eventName)
        {
            if (performanceParameters != null &&
                !performanceParameters.EnablePerformanceReporting)
            {
                return null;
            }

            if (!eventStopWatches.TryGetValue(componentName, out var dictionary))
            {
                dictionary = new Dictionary<string, Stopwatch>();
                eventStopWatches.Add(componentName, dictionary);
            }

            if (!dictionary.TryGetValue(eventName, out var stopwatch))
            {
                stopwatch = new Stopwatch();
                dictionary.Add(eventName, stopwatch);
            }

            return new TimeScope(stopwatch);
        }

        public void IncrementEventCount(string componentName, string eventName)
        {
            if (performanceParameters != null &&
                !performanceParameters.EnablePerformanceReporting)
            {
                return;
            }

            if (!eventCounts.TryGetValue(componentName, out var dictionary))
            {
                dictionary = new Dictionary<string, int>();
                eventCounts.Add(componentName, dictionary);
            }

            if (!dictionary.ContainsKey(eventName))
            {
                dictionary.Add(eventName, 1);
            }
            else
            {
                dictionary[eventName]++;
            }
        }

        public void WriteMessage(BinaryWriter message)
        {
            if (performanceParameters != null &&
                performanceParameters.EnablePerformanceReporting)
            {
                message.Write(true);
            }
            else
            {
                message.Write(false);
                return;
            }

            List<Tuple<string, double>> durations = new List<Tuple<string, double>>();
            foreach(var componentPair in eventStopWatches)
            {
                foreach(var eventPair in componentPair.Value)
                {
                    durations.Add(new Tuple<string, double>($"{componentPair.Key}.{eventPair.Key}", eventPair.Value.Elapsed.TotalMilliseconds));
                    eventPair.Value.Reset();
                }
            }

            message.Write(durations.Count);
            foreach(var duration in durations)
            {
                message.Write(duration.Item1);
                message.Write(duration.Item2);
            }

            List<Tuple<string, int>> counts = new List<Tuple<string, int>>();
            foreach(var componentPair in eventCounts.ToList())
            {
                foreach(var eventPair in componentPair.Value.ToList())
                {
                    counts.Add(new Tuple<string, int>($"{componentPair.Key}.{eventPair.Key}", eventPair.Value));
                    eventCounts[componentPair.Key][eventPair.Key] = 0;
                }
            }

            message.Write(counts.Count);
            foreach(var count in counts)
            {
                message.Write(count.Item1);
                message.Write(count.Item2);
            }
        }

        public static void ReadMessage(BinaryReader reader, out bool performanceMonitoringEnabled, out List<Tuple<string, double>> durations, out List<Tuple<string, int>> counts)
        {
            performanceMonitoringEnabled = reader.ReadBoolean();
            durations = null;
            counts = null;

            if (!performanceMonitoringEnabled)
            {
                return;
            }

            int durationsCount = reader.ReadInt32();
            if (durationsCount > 0)
            {
                durations = new List<Tuple<string, double>>();
                for (int i = 0; i < durationsCount; i++)
                {
                    string eventName = reader.ReadString();
                    double eventDuration = reader.ReadDouble();
                    durations.Add(new Tuple<string, double>(eventName, eventDuration));
                }
            }

            int countsCount = reader.ReadInt32();
            if (countsCount > 0)
            {
                counts = new List<Tuple<string, int>>();
                for (int i = 0; i < countsCount; i++)
                {
                    string eventName = reader.ReadString();
                    int eventCount = reader.ReadInt32();
                    counts.Add(new Tuple<string, int>(eventName, eventCount));
                }
            }
        }

        public void SetDiagnosticMode(bool enabled)
        {
            if (performanceParameters != null)
            {
                performanceParameters.EnablePerformanceReporting = enabled;
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