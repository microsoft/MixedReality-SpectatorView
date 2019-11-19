// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class PieceCorrection : MonoBehaviour
    {
        /// <summary>
        /// NOT USED ---------------------------- SCRIPT NOT USED ------------------------------- NOT USED ///
        /// </summary>
        PieceInformation pieceInformation;
        BoardInformation boardInformation;
        bool fixingPosition;
        bool collided;

        /// <summary>
        /// Displacement of the piece from it's position
        /// </summary>
        float xDisplacement;
        float zDisplacement;

        /// <summary>
        /// X and Z position of the piece
        /// </summary>
        float xPosition;
        float zPosition;

        float yDirectionTime = 0.5f;
        float fixTime = 1f;

        /// <summary>
        /// Retrieve the PieceInformation object for the piece
        /// </summary> 
        void Start()
        {
            pieceInformation = GetComponent<PieceInformation>();
            boardInformation = GameObject.Find("GameManager").GetComponent<BoardInformation>();
        }

        void Update()
        {
            if (collided && !boardInformation.PieceHandling)
            {
                Dropped();
                collided = false;
            }
        }
        
        void OnCollisionEnter(Collision collision)
        {
            collided = true;
        }

        /// <summary>
        /// Check if Y position is below the board
        /// Check if it's board position is incorrect
        /// Position is based on its local position
        /// Only if the piece is not being handled by the player
        /// </summary>
        void Dropped()
        {
            if (!boardInformation.PieceHandling && !fixingPosition)
            {
                Debug.Log(transform.localPosition.y);
                if (transform.localPosition.y < -0.1f)
                {
                    Debug.Log("Local Y is: " + transform.localPosition.y);
                    FixPosition();
                }

                xPosition = pieceInformation.GetXPosition();
                zPosition = pieceInformation.GetZPosition();

                xDisplacement = xPosition - transform.localPosition.x;
                zDisplacement = zPosition - transform.localPosition.z;

                // Buggy state avoidance - localPosition values are way off due to float imprecision (for now) 
                xDisplacement = (float) Math.Round(xDisplacement * 10f) / 10f;
                zDisplacement = (float) Math.Round(zDisplacement * 10f) / 10f;

                if (Math.Abs(xDisplacement) > 0.1 || Math.Abs(zDisplacement) > 0.1) ;
                {
                    Debug.Log("X displacement is: " + xDisplacement + " Z displacement is: " + zDisplacement);
                    FixPosition();
                }
            }
        }

        /// <summary>
        /// Fixes the position of the chess piece
        /// </summary>
        void FixPosition()
        {
            // Animation duration
            float duration = fixTime;
            bool moveUp = false;

            fixingPosition = true;
            
            // Move up if both xDisplacement and zDisplacement is > 0.5
            // Or either xDisplacement or zDisplacement is > 1
            if ((xDisplacement > 0.5 && zDisplacement > 0.5) || (xDisplacement > 1 || zDisplacement > 1))
            {
                moveUp = true;
                duration += (2 * yDirectionTime);
            }

            StartCoroutine(Animate(duration, moveUp));
        }

        /// <summary>
        /// Coroutine to animate the motions in y and (x and z) direction, one at a time
        /// </summary>
        IEnumerator Animate(float duration, bool moveUp)
        {
            GetComponent<Rigidbody>().isKinematic = true;
            float time = 0;

            Vector3 position = transform.localPosition;
            Vector3 above = new Vector3(position.x, 2f, position.z);

            while (moveUp && time < yDirectionTime)
            {
                time += Time.deltaTime;
                float blend = Mathf.Clamp01(time / yDirectionTime);

                transform.localPosition = Vector3.Lerp(position, above, blend);

                yield return null;
            }

            time = 0;
            position = transform.localPosition;
            Vector3 move = new Vector3(xPosition, position.y, zPosition);

            while (time < fixTime) {
                time += Time.deltaTime;
                float blend = Mathf.Clamp01(time / duration);

                transform.localPosition = Vector3.Lerp(position, move, blend);

                yield return null;
            }

            time = 0;
            position = transform.localPosition;
            Vector3 below = new Vector3(position.x, 0f, position.z);

            while (moveUp && time < yDirectionTime)
            {
                time += Time.deltaTime;
                float blend = Mathf.Clamp01(time / yDirectionTime);

                transform.localPosition = Vector3.Lerp(position, below, blend);

                yield return null;
            }

            fixingPosition = false;
            GetComponent<Rigidbody>().isKinematic = false;
        }

    }
}
