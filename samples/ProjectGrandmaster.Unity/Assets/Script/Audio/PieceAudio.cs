// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    [RequireComponent(typeof(AudioSource))]
    /// <summary>
    /// Plays audio whenever chess piece is collided
    /// </summary>
    public class PieceAudio : MonoBehaviour
    {
        [SerializeField]
        private AudioClip collisionSound;
        private AudioSource source;
        private bool isDropped;

        private void Start()
        {
            isDropped = false;
            source = this.GetComponent<AudioSource>();
            source.clip = collisionSound;
            source.volume = 1;
        }

        private void OnTriggerEnter(Collider collision)
        {
            // To prevent the sound from playing when the game starts
            // as the chess pieces drop onto the board
            if (Time.frameCount < 100)
            {
                return;
            }

            if (!source.isPlaying && isDropped)
            {
                source.Play();
                isDropped = false;
            }
        }

        public void BeingDropped()
        {
            isDropped = true;
        }
    }
}
