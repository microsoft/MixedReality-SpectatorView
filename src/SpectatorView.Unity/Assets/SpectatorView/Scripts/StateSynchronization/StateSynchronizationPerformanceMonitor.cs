// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace Microsoft.MixedReality.SpectatorView
{
    public class StateSynchronizationPerformanceMonitor : Singleton<StateSynchronizationPerformanceMonitor>
    {
        public class MemoryUsage
        {
            public long TotalAllocatedMemory;
            public long TotalReservedMemory;
            public long TotalUnusedReservedMemory;

            public MemoryUsage()
            {
                TotalAllocatedMemory = 0;
                TotalReservedMemory = 0;
                TotalUnusedReservedMemory = 0;
            }

            public override string ToString()
            {
                return $"TotalAllocatedMemory:{TotalAllocatedMemory}, TotalReservedMemory:{TotalReservedMemory}, TotalUnusedReservedMemory:{TotalUnusedReservedMemory}";
            }
        }

        private struct PerformanceEventKey
        {
            public string ComponentName;
            public string EventName;

            public PerformanceEventKey(string componentName, string eventName)
            {
                this.ComponentName = componentName;
                this.EventName = eventName;
            }

            public override string ToString()
            {
                return $"{this.ComponentName}.{this.EventName}";
            }
        };

        public struct ParsedMessage
        {
            public bool PerformanceMonitoringEnabled;
            public List<Tuple<string, double>> EventDurations;
            public List<Tuple<string, double>> SummedEventDurations;
            public List<Tuple<string, int>> EventCounts;
            public List<Tuple<string, MemoryUsage>> MemoryUsages;

            public ParsedMessage(bool performanceMonitoringEnabled, List<Tuple<string, double>> eventDurations, List<Tuple<string, double>> summedDurations, List<Tuple<string, int>> eventCounts, List<Tuple<string, MemoryUsage>> memoryUsages)
            {
                this.PerformanceMonitoringEnabled = performanceMonitoringEnabled;
                this.EventDurations = eventDurations;
                this.SummedEventDurations = summedDurations;
                this.EventCounts = eventCounts;
                this.MemoryUsages = memoryUsages; 
            }

            public static ParsedMessage Empty => new ParsedMessage(false, null, null, null, null);
        };

        private Dictionary<PerformanceEventKey, Stopwatch> eventStopWatches = new Dictionary<PerformanceEventKey, Stopwatch>();
        private Dictionary<PerformanceEventKey, Stopwatch> incrementEventStopWatches = new Dictionary<PerformanceEventKey, Stopwatch>();
        private Dictionary<PerformanceEventKey, int> eventCounts = new Dictionary<PerformanceEventKey, int>();
        private Dictionary<PerformanceEventKey, MemoryUsage> eventMemoryUsage = new Dictionary<PerformanceEventKey, MemoryUsage>();

        protected override void Awake()
        {
            base.Awake();
            Profiler.enabled = StateSynchronizationPerformanceParameters.EnablePerformanceReporting;
        }

        public IDisposable IncrementEventDuration(string componentName, string eventName)
        {
            if (!StateSynchronizationPerformanceParameters.EnablePerformanceReporting)
            {
                return null;
            }

            var key = new PerformanceEventKey(componentName, eventName);
            if (!incrementEventStopWatches.TryGetValue(key, out var stopwatch))
            {
                stopwatch = new Stopwatch();
                incrementEventStopWatches.Add(key, stopwatch);
            }

            return new TimeScope(stopwatch, key.ToString());
        }

        public IDisposable MeasureEventDuration(string componentName, string eventName)
        {
            if (!StateSynchronizationPerformanceParameters.EnablePerformanceReporting)
            {
                return null;
            }

            var key = new PerformanceEventKey(componentName, eventName);
            if (!eventStopWatches.TryGetValue(key, out var stopwatch))
            {
                stopwatch = new Stopwatch();
                eventStopWatches.Add(key, stopwatch);
            }

            return new TimeScope(stopwatch, key.ToString());
        }

        public void IncrementEventCount(string componentName, string eventName)
        {
            if (!StateSynchronizationPerformanceParameters.EnablePerformanceReporting)
            {
                return;
            }

            var key = new PerformanceEventKey(componentName, eventName);
            if (!eventCounts.ContainsKey(key))
            {
                eventCounts.Add(key, 1);
            }
            else
            {
                eventCounts[key]++;
            }
        }
        
        public IDisposable MeasureEventMemoryUsage(string componentName, string eventName)
        {
            if (!StateSynchronizationPerformanceParameters.EnablePerformanceReporting)
            {
                return null;
            }

            var key = new PerformanceEventKey(componentName, eventName);
            if (!eventMemoryUsage.TryGetValue(key, out var memoryUsage))
            {
                memoryUsage = new MemoryUsage();
                eventMemoryUsage.Add(key, memoryUsage);
            }

            return new MemoryScope(memoryUsage);
        }

        public void WriteMessage(BinaryWriter message, int numFrames)
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

            double numFramesScale = (numFrames == 0) ? 1.0 : 1.0 / numFrames;
            List<Tuple<string, double>> durations = new List<Tuple<string, double>>();
            foreach(var pair in eventStopWatches)
            {
                durations.Add(new Tuple<string, double>($"{pair.Key.ComponentName}.{pair.Key.EventName}", pair.Value.Elapsed.TotalMilliseconds * numFramesScale));
                pair.Value.Reset();
            }

            message.Write(durations.Count);
            foreach(var duration in durations)
            {
                message.Write(duration.Item1);
                message.Write(duration.Item2);
            }

            message.Write(incrementEventStopWatches.Count);
            foreach (var pair in incrementEventStopWatches)
            {
                message.Write($"{pair.Key.ComponentName}.{pair.Key.EventName}");
                message.Write(pair.Value.Elapsed.TotalMilliseconds);
            }

            List<Tuple<string, int>> counts = new List<Tuple<string, int>>();
            foreach (var pair in eventCounts.ToList())
            {
                counts.Add(new Tuple<string, int>($"{pair.Key.ComponentName}.{pair.Key.EventName}", (int)(pair.Value * numFramesScale)));
                eventCounts[pair.Key] = 0;
            }

            message.Write(counts.Count);
            foreach(var count in counts)
            {
                message.Write(count.Item1);
                message.Write(count.Item2);
            }

            message.Write(eventMemoryUsage.Count);
            foreach (var pair in eventMemoryUsage)
            {
                message.Write($"{pair.Key.ComponentName}.{pair.Key.EventName}");
                message.Write(pair.Value.TotalAllocatedMemory);
                message.Write(pair.Value.TotalReservedMemory);
                message.Write(pair.Value.TotalUnusedReservedMemory);
            }
        }

        public static void ReadMessage(BinaryReader reader, out ParsedMessage message)
        {
            bool performanceMonitoringEnabled = reader.ReadBoolean();
            List<Tuple<string, double>> durations = null;
            List<Tuple<string, double>> summedDurations = null;
            List<Tuple<string, int>> counts = null;
            List<Tuple<string, MemoryUsage>> memoryUsages = null;

            if (!performanceMonitoringEnabled)
            {
                message = ParsedMessage.Empty;
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

            int summedDurationsCount = reader.ReadInt32();
            if (summedDurationsCount > 0)
            {
                summedDurations = new List<Tuple<string, double>>();
                for (int i = 0; i < summedDurationsCount; i++)
                {
                    string eventName = reader.ReadString();
                    double eventDuration = reader.ReadDouble();
                    summedDurations.Add(new Tuple<string, double>(eventName, eventDuration));
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

            int memoryUsageCount = reader.ReadInt32();
            if (memoryUsageCount > 0)
            {
                memoryUsages = new List<Tuple<string, MemoryUsage>>();
                for (int i = 0; i < memoryUsageCount; i++)
                {
                    string eventName = reader.ReadString();
                    MemoryUsage usage = new MemoryUsage();
                    usage.TotalAllocatedMemory = reader.ReadInt64();
                    usage.TotalReservedMemory = reader.ReadInt64();
                    usage.TotalUnusedReservedMemory = reader.ReadInt64();
                    memoryUsages.Add(new Tuple<string, MemoryUsage>(eventName, usage));
                }
            }

            message = new ParsedMessage(performanceMonitoringEnabled, durations, summedDurations, counts, memoryUsages);
        }

        public void SetDiagnosticMode(bool enabled)
        {
            StateSynchronizationPerformanceParameters.EnablePerformanceReporting = enabled;
        }

        private struct TimeScope : IDisposable
        {
            private Stopwatch stopwatch;
            private bool stopSample;

            public TimeScope(Stopwatch stopwatch, string eventName)
            {
                this.stopwatch = stopwatch;
                stopwatch.Start();
                stopSample = false;

                if (Profiler.enabled)
                {
                    Profiler.BeginSample(eventName);
                    stopSample = true;
                }
            }

            public void Dispose()
            {
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    stopwatch = null;
                }

                if (stopSample &&
                    Profiler.enabled)
                {
                    Profiler.EndSample();
                    stopSample = false;
                }
            }
        }

        private struct MemoryScope : IDisposable
        {
            private long startingAllocatedMemory;
            private long startingReservedMemory;
            private long startingUnusedMemory;
            private MemoryUsage memoryUsage;
            private bool calculationCompleted;

            public MemoryScope(MemoryUsage memoryUsage)
            {
                calculationCompleted = false;

                if (!Profiler.enabled)
                {
                    UnityEngine.Debug.LogError($"Profiler not enabled, MemoryUsage not supported.");
                    this.memoryUsage = null;
                    startingAllocatedMemory = 0;
                    startingReservedMemory = 0;
                    startingUnusedMemory = 0;
                }
                else
                {
                    this.memoryUsage = memoryUsage;
                    startingAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong();
                    startingReservedMemory = Profiler.GetTotalReservedMemoryLong();
                    startingUnusedMemory = Profiler.GetTotalUnusedReservedMemoryLong();
                }
            }

            public void Dispose()
            {
                if (!Profiler.enabled ||
                    memoryUsage == null)
                {
                    UnityEngine.Debug.LogError($"Profiler not enabled or memoryUsage was null, MemoryUsage not in usable state.");
                    return;
                }

                if (!calculationCompleted)
                {
                    calculationCompleted = true;
                    memoryUsage.TotalAllocatedMemory += Profiler.GetTotalAllocatedMemoryLong() - startingAllocatedMemory;
                    memoryUsage.TotalReservedMemory += Profiler.GetTotalReservedMemoryLong() - startingReservedMemory;
                    memoryUsage.TotalUnusedReservedMemory += Profiler.GetTotalUnusedReservedMemoryLong() - startingUnusedMemory;
                }
            }
        }
    }
}