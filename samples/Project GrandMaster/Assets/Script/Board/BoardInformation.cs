// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Microsoft.MixedReality.USYD.ChessPiece;
using UnityEngine.UI;
using TMPro;

namespace Microsoft.MixedReality.USYD.Board
{
    public class BoardInformation : MonoBehaviour
    {
        enum Colours { White, Black };
        Colours turn;

        GameObject[,] board;

        PieceAction pieceAction;

        List<GameObject> piecesOnBoard;

        GameObject blackKing;
        GameObject whiteKing;

        public LayerMask chessboardLayer;

        // Piece being handled by the user
        public bool PieceHandling { get; set; }

        // GameObject related to displaying end result
        public GameObject EndGameResult;
        public GameObject whiteText;
        public GameObject blackText;
        public GameObject leftText;
        public GameObject rightText;

        // Piece cannot be moved until previous move is complete
        public bool CanMove { get; set; }

        public bool Check { get; set; }
        public bool GameEnded { get; set; }
        public bool AIEnabled { get; set; }
        public bool ghostActive { get; set; }

        // Side that won. -1 if black, 0 if draw, +1 if white.
        // Game Ended must also be true
        public int Winner { get; set; }

        // Arrow on the boardmenu that let's the player know if its their turn
        // Green if true, else red. 
        public GameObject whiteArrow;
        public GameObject blackArrow;
        public Material playerTurn;
        public Material opponentTurn;

        // Variables related to pawn promotion
        public Mesh mesh { get; set; }
        public bool promoted { get; set; }
        public bool meshChosen { get; set; }
        public GameObject pawnPromo;

        public GameObject boardMenuTileText;
        public GameObject boardMenuTileTextTwo;
        public Mesh pawnMesh;

        void Start()
        {
            /// <summary>
            /// 2D chess layout with individual piece GameObject
            /// </summary>
            board = new GameObject[8, 8];
            piecesOnBoard = new List<GameObject>();

            foreach (GameObject piece in GameObject.FindGameObjectsWithTag("pieces"))
            {
                PieceInformation info = piece.GetComponent<PieceInformation>();
                int x = info.GetXPosition();
                int z = info.GetZPosition();
                board[z, x] = piece;

                /// <summary>
                /// Initialise kings
                /// </summary>
                if (info.type == PieceInformation.Type.King)
                {
                    if (info.colour == PieceInformation.Colour.White) { whiteKing = piece; }
                    else { blackKing = piece; }
                }

                /// <summary>
                /// Add piece to piecesOnBoard list to keep track of all pieces on board
                /// </summary>
                piecesOnBoard.Add(piece);
            }

            /// <summary>
            /// Assign null for empty positions
            /// </summary>
            for (int i = 2; i <= 5; i++)
            {
                for (int j = 0; j <= 7; j++)
                {
                    board[i, j] = null;
                }
            }
            /// <summary>
            /// Initialise whose turn it is
            /// </summary>
            turn = Colours.White;

            /// <summary>
            /// Initialise Data Structure
            /// </summary>
            pieceAction = GetComponent<PieceAction>();

            //Turn off collisions for all pieces if ghost animation is active
            if (ghostActive)
            {
                Physics.IgnoreLayerCollision(14, 14);
                Physics.IgnoreLayerCollision(15, 15);
                Physics.IgnoreLayerCollision(14, 15);
            }

            // Disable the result display
            EndGameResult.SetActive(false);
            pawnPromo.SetActive(false);
        }

        public GameObject[,] GetBoard() { return board; }

        public int GetTurn() { return (int)turn; }

        public List<GameObject> GetPieceAvailable() { return piecesOnBoard; }

        public GameObject GetBlackKing() { return blackKing; }

        public GameObject GetWhiteKing() { return whiteKing; }

        public LayerMask GetChessboardLayer()
        {
            return chessboardLayer;
        }

        /// <summary>
        /// If Multiplayer toggled off in the menu, AI = true, and vice versa
        /// </summary>
        public void toggleAI()
        {
            if (AIEnabled) { AIEnabled = false; }
            else { AIEnabled = true; }
        }

        /// <summary>
        /// If ghosting enabled in the menu, Ghosting = true, and vice versa
        /// </summary>
        public void toggleGhosting()
        {
            if (ghostActive) {
                ghostActive = false;

                // Pieces ignore collisions if ghosting is on
                Physics.IgnoreLayerCollision(14, 15, false);
                Physics.IgnoreLayerCollision(14, 14, false);
                Physics.IgnoreLayerCollision(15, 15, false);
            }
            else {
                ghostActive = true;

                // Turn collisions back on
                Physics.IgnoreLayerCollision(14, 15, true);
                Physics.IgnoreLayerCollision(14, 14, true);
                Physics.IgnoreLayerCollision(15, 15, true);
            }
        }

        /// <summary>
        /// If White played, change to Black's turn and vice versa
        /// </summary>
        public void NextTurn()
        {
            if (turn == 0)
            {
                turn = Colours.Black;
                whiteArrow.GetComponent<Renderer>().material = opponentTurn;
                blackArrow.GetComponent<Renderer>().material = playerTurn;
            }
            else
            {
                turn = Colours.White;
                whiteArrow.GetComponent<Renderer>().material = playerTurn;
                blackArrow.GetComponent<Renderer>().material = opponentTurn;
            }
        }

        public void ResetTurn() {
            turn = Colours.Black;
            
            // Change arrow colours and reset turn to white
            NextTurn();
        }

        public void PrintBoard()
        {
            for (int i = 7; i >= 0; i--)
            {
                string row = "";
                for (int j = 0; j < 8; j++)
                {
                    if (board[i, j] == null)
                    {
                        row += "1 ";
                        continue;
                    }
                    PieceInformation info = board[i, j].GetComponent<PieceInformation>();
                    row += info.type;
                    row += " " + info.colour + " ";
                }
                Debug.Log(row);
            }
        }

        /////////////////////////////////////////////////////////////// Move Related Functions ///////////////////////////////////////////////////////////////

        public void RemovedFromBoard(GameObject piece)
        {
            piecesOnBoard.Remove(piece);
        }

        public void UpdateBoard(int prevX, int prevZ, int currentX, int currentZ, GameObject pieceParam = null)
        {
            GameObject piece = pieceParam;
            if (pieceParam == null) { piece = board[prevZ, prevX]; }
            board[prevZ, prevX] = null;
            board[currentZ, currentX] = piece;

            PieceInformation pieceInfo = piece.GetComponent<PieceInformation>();
            pieceInfo.SetXPosition(currentX);
            pieceInfo.SetZPosition(currentZ);
        }

        public void ResetState()
        {
            /// <summary>
            /// Reset state
            /// </summary>
            Check = false;
            GameEnded = false;

            foreach (GameObject restorePiece in MoveDataStructure.GetAllEliminated())
            {
                if (restorePiece == null)
                {
                    continue;
                }
                piecesOnBoard.Add(restorePiece);
                Debug.Log(restorePiece.name);
                pieceAction.FadeIn(restorePiece);
            }

            foreach (GameObject piece in piecesOnBoard)
            {
                PieceInformation pieceInfo = piece.GetComponent<PieceInformation>();
                if (pieceInfo.BeenPromoted)
                {
                    pieceInfo.BeenPromoted = false;
                    UndoPromotion(piece);
                }

                /// <summary>
                /// Update board
                /// </summary>
                UpdateBoard(pieceInfo.GetXPosition(), pieceInfo.GetZPosition(), pieceInfo.GetOriginalX(), pieceInfo.GetOriginalZ(), piece);

                /// <summary>
                /// Reset to piece's default values
                /// </summary>
                pieceInfo.SetXPosition(pieceInfo.GetOriginalX());
                pieceInfo.SetZPosition(pieceInfo.GetOriginalZ());
                pieceInfo.PieceMoves = 0;

                /// <summary>
                /// Reset location
                /// </summary>
                Vector3 endPosition = new Vector3(pieceInfo.GetOriginalX(), 0, pieceInfo.GetOriginalZ());
                pieceAction.ChangePosition(piece, endPosition, (int)pieceInfo.colour);
            }

            MoveDataStructure.Clear();

            /// <summary>
            /// Reset turn - White Starts
            /// </summary>
            ResetTurn();
        }

        public void UndoState()
        {
            /// <summary>
            /// Check if previous move exists
            /// </summary>
            if (MoveDataStructure.GetCount() == 0)
            {
                return;
            }

            //  Retrieve previous move
            ArrayList moveData = MoveDataStructure.Undo();

            // If undo'd a check, remove the check path
            if ((bool)moveData[7])
            {
                MoveDataStructure.RemoveLastPath();
            }

            /// <summary>
            /// Check = check state prior to the last move
            /// </summary>
            Check = MoveDataStructure.GetLastCheck();
            Debug.Log(Check.ToString());

            /// <summary>
            /// Change piece's position
            /// </summary>
            int lastZPosition = (int) moveData[4];
            int lastXPosition = (int) moveData[3];

            GameObject piece = (GameObject) moveData[2];
            PieceInformation pieceInformation = piece.GetComponent<PieceInformation>();

            if (pieceInformation.BeenPromoted && (bool)moveData[8])
            {
                pieceInformation.BeenPromoted = false;
                UndoPromotion(piece);
            }

            int currentXPosition = pieceInformation.GetXPosition();
            int currentZPosition = pieceInformation.GetZPosition();
            pieceInformation.SetXPosition(lastXPosition);
            pieceInformation.SetZPosition(lastZPosition);

            /// <summary>
            /// Subtract one from total moves on current piece
            /// </summary>
            pieceInformation.PieceMoves--;

            /// <summary>
            /// Move piece
            /// </summary>
            Vector3 revertPosition = new Vector3(lastXPosition, 0, lastZPosition);
            bool rookPiece = (int)pieceInformation.type == 0 ? true : false; // sliding motion
            pieceAction.ChangePosition(piece, revertPosition, (int)pieceInformation.colour, rookPiece);

            /// <summary>
            /// Update Board
            /// </summary>
            UpdateBoard(currentXPosition, currentZPosition, lastXPosition, lastZPosition);

            /// <summary>
            /// Check if the last move was a castle 
            /// </summary>
            if ((int)pieceInformation.type == 4 && Math.Abs(currentXPosition - lastXPosition) == 2)
            {
                int rookX;
                int originalRookX;

                /// <summary>
                /// King castled right 
                /// </summary>
                if (currentXPosition > lastXPosition)
                {
                    rookX = currentXPosition - 1;
                    originalRookX = 7;
                }
                /// <summary>
                /// King castled left
                /// </summary>
                else
                {
                    rookX = currentXPosition + 1;
                    originalRookX = 0;
                }

                GameObject rook = board[currentZPosition, rookX];
                Vector3 rookPos = new Vector3(originalRookX, 0, currentZPosition);

                /// <summary>
                /// Move the rook to its original position
                /// </summary>
                pieceAction.ChangePosition(rook, rookPos, (int)pieceInformation.colour, true);
                /// <summary>
                /// Update board
                /// </summary>
                UpdateBoard(rookX, currentZPosition, originalRookX, currentZPosition);
            }

            /// <summary>
            /// Reinstate any piece eliminated in the last move
            /// </summary>
            else if ((bool)moveData[0])
            {
                GameObject reinstate = (GameObject)moveData[1];
                piecesOnBoard.Add(reinstate);
                pieceAction.FadeIn(reinstate);

                /// <summary>
                /// Fix positioning of the reinstated piece
                /// </summary>
                PieceInformation pieceInfo = reinstate.GetComponent<PieceInformation>();
                Vector3 position = new Vector3(pieceInfo.GetXPosition(), 0, pieceInfo.GetZPosition());
                pieceAction.ChangePosition(reinstate, position, (int)pieceInfo.colour);

                /// <summary>
                /// Update Board
                /// </summary>
                board[pieceInfo.GetZPosition(), pieceInfo.GetXPosition()] = reinstate;
            }
            /// <summary>
            /// Change player's turn 
            /// </summary>
            NextTurn();
        }

        /// <summary>
        /// Displays check for 5 seconds, turning on/off every second
        /// </summary>
        public void CheckDisplay()
        {
            EndGameResult.SetActive(true);
            string check = "CHECK!!";
            SetText(check);
            StartCoroutine(FlashText(3, true));
        }

        public void SetText(string text)
        {
            whiteText.GetComponent<Text>().text = text;
            blackText.GetComponent<Text>().text = text;
            leftText.GetComponent<Text>().text = text;
            rightText.GetComponent<Text>().text = text;
        }

        IEnumerator FlashText(int duration, bool flashAll)
        {
            int time = 0;
            bool active = true;

            while (time < duration)
            {
                if (flashAll)
                {
                    whiteText.SetActive(active);
                    blackText.SetActive(active);
                    leftText.SetActive(active);
                    rightText.SetActive(active);
                }
                // Flash left and right only
                else
                {
                    leftText.SetActive(active);
                    rightText.SetActive(active);
                    whiteText.SetActive(true);
                    blackText.SetActive(true);
                }
                time += 1;
                active = !active;
                yield return new WaitForSeconds(1);
            }
            // Turn off display after 'duration' seconds
            EndGameResult.SetActive(false);
        }


        /// <summary>
        /// Checkmate. Shows Checkmate to notify the players
        /// </summary>
        public void Checkmate(int colour)
        {
            EndGameResult.SetActive(true);
            string checkmate = "CHECKMATE";
            SetText(checkmate);
            EndGame(colour);
        }

        /// <summary>
        /// Changes the game state
        /// Declares the winner
        /// </summary>
        /// <param name="colour"> Colour that won. 0 if White. 1 if Black. -1 if Draw. </param>
        public void EndGame(int colour)
        {
            GameEnded = true;
            // White won
            if (colour == 0) { Winner = 1; }

            // Black won
            else if (colour == 1) { Winner = -1; }

            // Draw
            else { Winner = 0; }

            // Display result to players
            ShowResult();
            StartCoroutine(FlashText(5, false));
        }

        /// <summary>
        /// Displays the game result on top of the board
        /// </summary>
        /// <returns></returns>
        private void ShowResult()
        {
            string won = "YOU WON!";
            string lose = "YOU LOST :(";
            string draw = "YOU DREW";

            // Draw
            if (Winner == 0)
            {
                whiteText.GetComponent<Text>().text = draw;
                blackText.GetComponent<Text>().text = draw;
            }
            // White won
            else if (Winner == 1)
            {
                whiteText.GetComponent<Text>().text = won;
                blackText.GetComponent<Text>().text = lose;
            }
            // Black won
            else
            {
                whiteText.GetComponent<Text>().text = lose;
                blackText.GetComponent<Text>().text = won;
            }
        }

        /// <summary>
        /// Called when the pawn reaches the opposite side of the board.
        /// Pawn disappears and player has a chance to choose the piece they want to replace it with.
        /// </summary>
        public IEnumerator PromotePawn(PieceInformation pieceInfo, GameObject pawn)
        { 
            pawnPromo.SetActive(true);
            string pawnPromotion = "PAWN PROMOTION!";
            SetText(pawnPromotion);
            MoveDataStructure.Promoted();

            boardMenuTileText.GetComponent<TextMeshPro>().text = "Promotion Tile";
            boardMenuTileTextTwo.GetComponent<TextMeshPro>().text = "Promotion Tile";

            EndGameResult.SetActive(true);

            while (!promoted)
            {
                if (meshChosen)
                {
                    int x = pieceInfo.GetXPosition();
                    int z = pieceInfo.GetZPosition();

                    pawn.GetComponent<MeshFilter>().mesh = mesh;
                    if (mesh.ToString().Contains("Rook"))
                    {
                        pieceInfo.type = PieceInformation.Type.Rook;
                    }
                    else if (mesh.ToString().Contains("Queen"))
                    {
                        pieceInfo.type = PieceInformation.Type.Queen;
                    }
                    else if (mesh.ToString().Contains("Bishop"))
                    {
                        pieceInfo.type = PieceInformation.Type.Bishop;
                    }
                    else if (mesh.ToString().Contains("Knight"))
                    {
                        pieceInfo.type = PieceInformation.Type.Knight;
                    }
                    promoted = true;
                }

                yield return null;
            }

            promoted = false;
            meshChosen = false;
            boardMenuTileText.GetComponent<TextMeshPro>().text = "Forfeit Tile";
            boardMenuTileTextTwo.GetComponent<TextMeshPro>().text = "Forfeit Tile";
            pawnPromo.SetActive(false);
            EndGameResult.SetActive(false);
            pieceInfo.ContinueProcess();
        }

        /// <summary>
        /// Undo/Reset pawn promotion
        /// </summary>
        public void UndoPromotion(GameObject pawn)
        {
            PieceInformation pawnInfo = pawn.GetComponent<PieceInformation>();
            pawnInfo.type = PieceInformation.Type.Pawn;

            pawn.GetComponent<MeshFilter>().mesh = pawnMesh;
        }
    }
}