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
    public class StateSynchronizationPerformanceMonitor : Singleton<StateSynchronizationPerformanceMonitor>
    {
        private struct StopWatchKey
        {
            public string ComponentName;
            public string EventName;

            public StopWatchKey(string componentName, string eventName)
            {
                this.ComponentName = componentName;
                this.EventName = eventName;
            }
        };

        public struct ParsedMessage
        {
            public bool PerformanceMonitoringEnabled;
            public List<Tuple<string, double>> EventDurations;
            public List<Tuple<string, int>> EventCounts;

            public ParsedMessage(bool performanceMonitoringEnabled, List<Tuple<string, double>> eventDurations, List<Tuple<string, int>> eventCounts)
            {
                this.PerformanceMonitoringEnabled = performanceMonitoringEnabled;
                this.EventDurations = eventDurations;
                this.EventCounts = eventCounts;
            }
        };

        private Dictionary<StopWatchKey, Stopwatch> eventStopWatches = new Dictionary<StopWatchKey, Stopwatch>();
        private Dictionary<StopWatchKey, int> eventCounts = new Dictionary<StopWatchKey, int>();

        protected override void Awake()
        {
            base.Awake();
        }

        public IDisposable MeasureEventDuration(string componentName, string eventName)
        {
            if (!StateSynchronizationPerformanceParameters.EnablePerformanceReporting)
            {
                return null;
            }

            var key = new StopWatchKey(componentName, eventName);
            if (!eventStopWatches.TryGetValue(key, out var stopwatch))
            {
                stopwatch = new Stopwatch();
                eventStopWatches.Add(key, stopwatch);
            }

            return new TimeScope(stopwatch);
        }

        public void IncrementEventCount(string componentName, string eventName)
        {
            if (!StateSynchronizationPerformanceParameters.EnablePerformanceReporting)
            {
                return;
            }

            var key = new StopWatchKey(componentName, eventName);
            if (!eventCounts.ContainsKey(key))
            {
                eventCounts.Add(key, 1);
            }
            else
            {
                eventCounts[key]++;
            }
        }

        public void WriteMessage(BinaryWriter message, double averageFrameDurationInSeconds)
        {
            if (StateSynchronizationPerformanceParameters.EnablePerformanceReporting)
            {
                message.Write(true);
            }
            else
            {
                message.Write(false);
                return;
            }

            double timeInMilliseconds = 1000 * averageFrameDurationInSeconds;
            List<Tuple<string, double>> durations = new List<Tuple<string, double>>();
            foreach(var pair in eventStopWatches)
            {
                durations.Add(new Tuple<string, double>($"{pair.Key.ComponentName}.{pair.Key.EventName}", pair.Value.Elapsed.TotalMilliseconds / timeInMilliseconds));
                pair.Value.Reset();
            }

            message.Write(durations.Count);
            foreach(var duration in durations)
            {
                message.Write(duration.Item1);
                message.Write(duration.Item2);
            }

            List<Tuple<string, int>> counts = new List<Tuple<string, int>>();
            foreach (var pair in eventCounts.ToList())
            {
                counts.Add(new Tuple<string, int>($"{pair.Key.ComponentName}.{pair.Key.EventName}", pair.Value));
                eventCounts[pair.Key] = 0;
            }

            message.Write(counts.Count);
            foreach(var count in counts)
            {
                message.Write(count.Item1);
                message.Write(count.Item2);
            }
        }

        public static void ReadMessage(BinaryReader reader, out ParsedMessage message)
        {
            bool performanceMonitoringEnabled = reader.ReadBoolean();
            List<Tuple<string, double>> durations = null;
            List<Tuple<string, int>> counts = null;

            if (!performanceMonitoringEnabled)
            {
                message = new ParsedMessage(performanceMonitoringEnabled, durations, counts);
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

            message = new ParsedMessage(performanceMonitoringEnabled, durations, counts);
        }

        public void SetDiagnosticMode(bool enabled)
        {
            StateSynchronizationPerformanceParameters.EnablePerformanceReporting = enabled;
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