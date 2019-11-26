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

        // Classic material by default
        private bool modern = false;

        void Awake()
        {
            GameObject manager = GameObject.Find("GameManager");
            boardInfo = manager.GetComponent<BoardInformation>();
        }

        /// <summary>
        /// Called when the button is pressed in the hand menu
        /// </summary>
        public void toggle()
        {
            // Change from modern to classic
            if (modern)
            {
                modern = false;
                changePieceMat(classicWhite, classicBlack);
                changeTileMat(classicWhiteTile, classicBlackTile);
            }

            // Change from classic to modern
            else
            {
                modern = true;
                changePieceMat(modernWhite, modernBlack);
                changeTileMat(whiteTileMat, blackTileMat);
            }
        }

        /// <summary>
        /// Changes the tile mat.
        /// </summary>
        /// <param name="white">The new white material.</param>
        /// <param name="black">The new black material.</param>
        private void changeTileMat(Material white, Material black)
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
        private void changePieceMat(Material white, Material black)
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