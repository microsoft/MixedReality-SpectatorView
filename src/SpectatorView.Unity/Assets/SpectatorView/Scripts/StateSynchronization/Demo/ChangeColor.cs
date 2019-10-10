// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class ChangeColor : MonoBehaviour
    {
        [SerializeField]
        private Material material = null;

        void Update()
        {
            float r = (1.0f + Mathf.Sin(Time.time)) / 2.0f;
            float g = (1.0f + Mathf.Sin(2 * Time.time)) / 2.0f;
            float b = (1.0f + Mathf.Sin(3 * Time.time)) / 2.0f;
            float alpha = (1.0f + Mathf.Sin(4 * Time.time)) / 2.0f;
            material.color = new Color(r, g, b, alpha);
        }
    }
}
