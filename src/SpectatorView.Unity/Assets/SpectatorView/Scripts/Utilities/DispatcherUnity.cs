// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// A helper class to Dispatch actions to the GameThread
    /// </summary>
    public static class DispatcherUnity
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Dispatcher.Initialize(Dispatcher.CurrentThreadId, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}