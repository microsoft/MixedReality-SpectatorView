// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class MenuVoiceHandler : MonoBehaviour
    {
        public void ToggleMenuOn()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        public void ToggleMenuOff()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }
}