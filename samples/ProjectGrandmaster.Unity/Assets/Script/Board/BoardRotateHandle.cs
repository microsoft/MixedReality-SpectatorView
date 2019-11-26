// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    /// <summary>
    /// Forfeiting mechanism - Forfeits when the player flips the board from the middle 
    /// Needs to be enabled in the 'Chessboard' game object
    /// </summary>
    public class BoardRotateHandle : MonoBehaviour
    {
        private GameObject gameManager;
        private BoardInformation boardInfo;
        private PieceAction pieceAction;

        private PieceInformation.Colour lossColour;

        private void TiltForfeit(PieceInformation.Colour colour)
        {
            List<GameObject> pieces = boardInfo.GetPieceAvailable();
            foreach (GameObject piece in pieces)
            {
                PieceInformation pieceInfo = piece.GetComponent<PieceInformation>();
                if (pieceInfo.colour == colour)
                {
                    StartCoroutine(pieceAction.FallDown(piece));
                }
            }
            lossColour = colour;
            boardInfo.GameEnded = true;
        }

        public void FixBoard()
        {
            gameObject.transform.Rotate(-gameObject.transform.eulerAngles.x, 0, 0);
            gameObject.transform.localPosition = new Vector3(0, -0.0251f, 0);
            FixPieces();
        }

        private void FixPieces()
        {
            List<GameObject> pieces = boardInfo.GetPieceAvailable();
            foreach (GameObject piece in pieces)
            {
                PieceInformation pieceInfo = piece.GetComponent<PieceInformation>();
                Vector3 position = new Vector3(pieceInfo.GetXPosition(), 0, pieceInfo.GetZPosition());
                if (!CheckSimilarity(piece.transform.localPosition, position))
                {
                    if (pieceInfo.colour != lossColour)
                    {
                        pieceAction.ChangePosition(piece, position, (int)pieceInfo.colour);
                    }
                }
            }
        }

        bool CheckSimilarity(Vector3 first, Vector3 second)
        {
            float xDiff = Math.Abs(first.x - second.x);
            float yDiff = Math.Abs(first.y - second.y);
            float zDiff = Math.Abs(first.z - second.z);
            if (xDiff > 0.1 || yDiff > 0.1 || zDiff > 0.1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            gameManager = GameObject.Find("GameManager");
            boardInfo = gameManager.GetComponent<BoardInformation>();
            pieceAction = gameManager.GetComponent<PieceAction>();
        }

        // Update is called once per frame
        void Update()
        {
            if (gameObject.transform.eulerAngles.x > 10)
            {
                TiltForfeit(PieceInformation.Colour.White);
            }
            else if (gameObject.transform.eulerAngles.x < -10)
            {
                TiltForfeit(PieceInformation.Colour.Black);
            }
        }
    }
}
