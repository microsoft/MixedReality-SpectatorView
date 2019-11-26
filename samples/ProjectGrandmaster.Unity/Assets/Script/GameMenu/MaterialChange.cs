// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class MaterialChange : MonoBehaviour
    {
        public Material classicWhite;
        public Material classicBlack;
        public Material modernBlack;
        public Material modernWhite;
        public Material blackTileMat;
        public Material whiteTileMat;
        public Material classicBlackTile;
        public Material classicWhiteTile;

        public List<GameObject> blackTiles;
        public List<GameObject> whiteTiles;

        private BoardInformation boardInfo;
        private PieceAction pieceAction;

        // Classic material by default
        private bool modern = false;

        void Awake()
        {
            GameObject manager = GameObject.Find("GameManager");
            boardInfo = manager.GetComponent<BoardInformation>();
            pieceAction = manager.GetComponent<PieceAction>();
        }

        void ClassicMat()
        {
            float blackMat = classicBlack.GetFloat("_Metallic");
            float blackGloss = classicBlack.GetFloat("_Glossiness");
            float whiteMat = classicWhite.GetFloat("_Metallic");
            float whiteGloss = classicBlack.GetFloat("_Glossiness");
            pieceAction.SetMaterialValues(blackMat, whiteMat, whiteGloss, blackGloss);
        }

        void ModernMat()
        {
            float blackMat = modernBlack.GetFloat("_Metallic");
            float blackGloss = modernBlack.GetFloat("_Glossiness");
            float whiteMat = modernWhite.GetFloat("_Metallic");
            float whiteGloss = modernBlack.GetFloat("_Glossiness");
            pieceAction.SetMaterialValues(blackMat, whiteMat, whiteGloss, blackGloss);
        }

        /// <summary>
        /// Called when the button is pressed in the hand menu
        /// </summary>
        public void Toggle()
        {
            // Change from modern to classic
            if (modern)
            {
                modern = false;
                ChangePieceMat(classicWhite, classicBlack);
                ChangeTileMat(classicWhiteTile, classicBlackTile);
                ClassicMat();
            }

            // Change from classic to modern
            else
            {
                modern = true;
                ChangePieceMat(modernWhite, modernBlack);
                ChangeTileMat(whiteTileMat, blackTileMat);
                ModernMat();
            }
        }

        /// <summary>
        /// Changes the tile mat.
        /// </summary>
        /// <param name="white">The new white material.</param>
        /// <param name="black">The new black material.</param>
        private void ChangeTileMat(Material white, Material black)
        {
            foreach (GameObject tile in blackTiles)
            {
                tile.GetComponent<MeshRenderer>().material = black;
                if (tile.GetComponent<ActivateHighlight>())
                {
                    tile.GetComponent<ActivateHighlight>().ChangeStartColour();
                }
            }
            foreach (GameObject tile in whiteTiles)
            {
                tile.GetComponent<MeshRenderer>().material = white;
                if (tile.GetComponent<ActivateHighlight>())
                {
                    tile.GetComponent<ActivateHighlight>().ChangeStartColour();
                }
            }
        }

        /// <summary>
        /// Changes the piece mat.
        /// </summary>
        /// <param name="white">The new white piece material.</param>
        /// <param name="black">The new black piece material.</param>
        private void ChangePieceMat(Material white, Material black)
        {
            foreach (GameObject piece in boardInfo.GetPieceAvailable())
            {
                PieceInformation pieceInfo = piece.GetComponent<PieceInformation>();
                if (pieceInfo.colour == 0)
                {
                    piece.GetComponent<MeshRenderer>().material = white;
                }
                else
                {
                    piece.GetComponent<MeshRenderer>().material = black;
                }
                piece.GetComponent<HighlightChessPiece>().ChangeStartColour();
            }
            foreach (GameObject piece in MoveHistory.Instance.EliminatedObjects)
            {
                PieceInformation pieceInfo = piece.GetComponent<PieceInformation>();
                if (pieceInfo.colour == 0)
                {
                    piece.GetComponent<MeshRenderer>().material = white;
                }
                else
                {
                    piece.GetComponent<MeshRenderer>().material = black;
                }
                piece.GetComponent<HighlightChessPiece>().ChangeStartColour();
            }
        }
    }
}