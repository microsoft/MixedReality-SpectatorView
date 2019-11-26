// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    /// <summary>
    /// This script is attached to the tile objects and is used to change their colour
    /// </summary>
    public class ActivateHighlight : MonoBehaviour
    {
        private MeshRenderer mr;
        private Color startColor;
        private Color highlightColor;

        // Start is called before the first frame update
        void Start()
        {
            mr = GetComponent<MeshRenderer>();
            if (!mr)
            {
                Debug.LogError("Mesh Renderer not found");
            }
            else
            {
                ChangeStartColour();
                highlightColor = new Color(50f / 255f, 180f / 255f, 80f / 255f, 130f / 255f);
            }
            
        }

        // Update is called once per frame
        public void HighlightOn()
        {
            mr.material.color = highlightColor;
        }

        public void HighlightOff()
        {
            mr.material.color = startColor;
        }

        public void ChangeStartColour()
        {
            startColor = mr.material.color;
        }
    }
}