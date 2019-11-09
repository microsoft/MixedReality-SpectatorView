using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.USYD.Board;
using Microsoft.MixedReality.USYD.ChessPiece;
using Microsoft.MixedReality.USYD.HighlightObjects;

namespace Microsoft.MixedReality.USYD.GameMenu
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

        BoardInformation boardInfo;

        // Classic material by default
        private bool modern = false;

        void Awake()
        {
            GameObject manager = GameObject.Find("GameManager");
            boardInfo = manager.GetComponent<BoardInformation>();
        }

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
            foreach (GameObject piece in MoveDataStructure.GetAllEliminated())
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