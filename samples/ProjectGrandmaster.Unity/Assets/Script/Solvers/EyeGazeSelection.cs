// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    /// <summary>
    /// Used to allow users to move pieces using their gaze
    /// </summary>
    public class EyeGazeSelection : MonoBehaviour
    {
        public BoardInformation boardInformation;
        private GameObject pieceSelected;
        private GameObject chosenTile;
        private PieceInformation pieceInformation;
        private int tileX;
        private int tileZ;
        private float duration = 1f;

        private IMixedRealityInputSystem inputSystem = null;

        /// <summary>
        /// The active instance of the input system.
        /// </summary>
        private IMixedRealityInputSystem InputSystem
        {
            get
            {
                if (inputSystem == null)
                {
                    MixedRealityServiceRegistry.TryGetService<IMixedRealityInputSystem>(out inputSystem);
                }
                return inputSystem;
            }
        }

        /// <summary>
        /// Selects the piece the user is gazing on.
        /// Called when user says "select"
        /// </summary>
        public void SelectPiece()
        {
            if (InputSystem?.GazeProvider == null)
            {
                return;
            }

            GameObject piece = InputSystem.GazeProvider.GazeTarget;

            // If "select" said twice, place the first piece back down
            if (pieceSelected != null)
            {
                StartCoroutine(PieceDown(changedPiece: piece));
            }

            else if (piece == null || piece.tag != "pieces")
            {
                return;
            } 
            
            else
            {
                pieceSelected = piece;
                pieceInformation = piece.GetComponent<PieceInformation>();
                StartCoroutine(PieceFloat());
            }
        }

        /// <summary>
        /// Moves the piece to the chosen gazed location.
        /// Called when the user says "move"
        /// </summary>
        public void Move()
        {
            if (InputSystem?.GazeProvider == null)
            {
                return;
            }

            // Check if piece has been selected to be moved
            if (pieceSelected == null)
            {
                return;
            }

            GameObject tile = InputSystem.GazeProvider.GazeTarget;
            chosenTile = tile;

            // if the gameobject is not a tile, do nothing
            // if tile is occupied, return and let user choose another tile
            if (tile == null || tile.tag != "BoardPosition" || !TileValid())
            {
                return;
            }

            else
            {
                StartCoroutine(MoveToTile());
            }
        }

        /// <summary>
        /// Checks if the tile contains no chess pieces
        /// </summary>
        /// <returns> true if piece can be moved to this tile </returns>
        private bool TileValid()
        {
            // Get tile position 
            TilePosition();
            // Get the updated board
            GameObject[,] board = boardInformation.Board;
            Debug.Log(tileZ + " " + tileX);
            if (board[tileZ, tileX] == null)
            {
                return true;
            }

            return false;
        }

        private IEnumerator PieceFloat()
        {
            pieceSelected.GetComponent<Rigidbody>().isKinematic = true;
            pieceSelected.GetComponent<Rigidbody>().detectCollisions = false;
            Vector3 piecePosition = pieceSelected.transform.localPosition;
            Vector3 floatPosition = new Vector3(piecePosition.x, 2, piecePosition.z);

            float time = 0;

            while (time <= duration)
            {
                time += Time.deltaTime;
                float blend = Mathf.Clamp01(time / duration);
                pieceSelected.transform.localPosition = Vector3.Lerp(piecePosition, floatPosition, blend);

                yield return null;
            }
        }

        private IEnumerator MoveToTile()
        {
            Vector3 piecePosition = pieceSelected.transform.localPosition;
            Vector3 floatPosition = new Vector3(tileX, 2, tileZ);

            float time = 0;

            while (time <= duration)
            {
                time += Time.deltaTime;
                float blend = Mathf.Clamp01(time / duration);
                pieceSelected.transform.localPosition = Vector3.Lerp(piecePosition, floatPosition, blend);

                yield return null;
            }

            StartCoroutine(PieceDown(checkValidity: true));
        }

        private IEnumerator PieceDown(GameObject changedPiece = null, bool checkValidity = false)
        {
            Vector3 piecePosition = pieceSelected.transform.localPosition;
            Vector3 down = new Vector3(piecePosition.x, 0, piecePosition.z);

            float time = 0;

            while (time <= duration)
            {
                time += Time.deltaTime;
                float blend = Mathf.Clamp01(time / duration);
                pieceSelected.transform.localPosition = Vector3.Lerp(piecePosition, down, blend);

                yield return null;
            }

            // If the player changes the piece they want to move
            if (changedPiece != null)
            {
                ChangePiece(changedPiece);
            }

            // Check if chosen position is valid
            if (checkValidity)
            {
                pieceInformation.GetMoves();
                pieceInformation.Moved();
            }

            pieceSelected.GetComponent<Rigidbody>().detectCollisions = true;
            pieceSelected.GetComponent<Rigidbody>().isKinematic = false;
            pieceSelected = null;
            pieceInformation = null;
        }

        private void TilePosition()
        {
            tileX = ((int)chosenTile.name[0]) - 65;
            tileZ = (int)char.GetNumericValue(chosenTile.transform.parent.name[0]);
        }

        private void ChangePiece(GameObject piece)
        {
            pieceSelected = piece;
            pieceInformation = pieceSelected.GetComponent<PieceInformation>();
            StartCoroutine(PieceFloat());
        }
    }
}