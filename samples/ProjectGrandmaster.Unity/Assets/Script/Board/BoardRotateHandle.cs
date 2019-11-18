using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.USYD.Board;
using Microsoft.MixedReality.USYD.ChessPiece;

namespace Microsoft.MixedReality.USYD.Board
{
    public class BoardRotateHandle : MonoBehaviour
    {
        public GameObject chessBoard;

        GameObject gameManager;
        BoardInformation boardInfo;
        PieceAction pieceAction;

        PieceInformation.Colour lossColour;

        void tiltForfeit(PieceInformation.Colour colour)
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
        public void fixBoard()
        {
            chessBoard.transform.Rotate(-chessBoard.transform.eulerAngles.x, 0, 0);
            chessBoard.transform.localPosition = new Vector3(0, -0.0251f, 0);
            fixPieces();
        }
        void fixPieces()
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
            if (chessBoard.transform.eulerAngles.x > 10)
            {
                tiltForfeit(PieceInformation.Colour.White);
            }
            else if (chessBoard.transform.eulerAngles.x < -10)
            {
                tiltForfeit(PieceInformation.Colour.Black);
            }
        }
    }
}
