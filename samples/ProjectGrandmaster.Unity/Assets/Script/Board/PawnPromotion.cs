// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    /// <summary>
    /// Script for the Pieces for pawn promotion
    /// </summary>
    public class PawnPromotion : MonoBehaviour
    {
        BoardInformation boardInfo;
        public LayerMask layer;
        public bool manipulating { get; set; }

        Vector3 position;
        Quaternion rotation;

        void Start()
        {
            boardInfo = GameObject.Find("GameManager").GetComponent<BoardInformation>();
            position = transform.localPosition;
            rotation = transform.rotation;
        }

        void Update()
        {
            if (transform.localPosition.y < -1.5f && !manipulating)
            {
                transform.localPosition = position;
                transform.rotation = rotation;
            }
        }

        /// <summary>
        /// Checks if the piece is placed on top of the 'pawn promotion' tile
        /// Change the piece mesh to the chosen piece for pawn promotion
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        public void PromoteCheck(Mesh mesh)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, -transform.forward, out hit, 1f, layer))
            {
                GameObject pieceCollided = hit.collider.gameObject;
                if (string.Compare(pieceCollided.name, "forfeit tile") == 0)
                {
                    boardInfo.Mesh = mesh;
                    boardInfo.MeshChosen = true;
                    transform.localPosition = position;
                    transform.rotation = rotation;
                }
            }
        }
    }
}