// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class RecordingTestCube : MonoBehaviour
    {
        [SerializeField]
        private CompositionManager compositionManager = null;

        [SerializeField]
        private Material recordingTestCubeMaterial = null;

        [SerializeField]
        private AudioSource recordingTestCubeSound = null;

        private Color originalColor;
        private enum PressState
        {
            Down,
            Up
        };

        private Queue<Tuple<float, PressState>> pressQueue = new Queue<Tuple<float, PressState>>();

        void Start()
        {
            originalColor = recordingTestCubeMaterial.color;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                float offset = compositionManager.VideoTimestampToHolographicTimestampOffset;
                pressQueue.Enqueue(new Tuple<float, PressState>(Time.time + offset, PressState.Down));
                Debug.Log($"Space bar press delayed:{offset}s");
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                float offset = compositionManager.VideoTimestampToHolographicTimestampOffset;
                pressQueue.Enqueue(new Tuple<float, PressState>(Time.time + offset, PressState.Up));
                Debug.Log($"Space bar release delayed:{offset}s");
            }

            while (pressQueue.Count > 0 &&
                pressQueue.Peek().Item1 < Time.time)
            {
                var pressInfo = pressQueue.Dequeue();
                switch(pressInfo.Item2)
                {
                    case PressState.Down:
                        recordingTestCubeMaterial.color = Color.green;
                        recordingTestCubeSound.Play();
                        break;
                    case PressState.Up:
                        recordingTestCubeMaterial.color = originalColor;
                        break;
                }
            }
        }
    }
}
