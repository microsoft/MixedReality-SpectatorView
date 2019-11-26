// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;
using UnityEngine.UI;
using TMPro;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    /// <summary>
    /// This class contains information about the board state/layout. 
    /// Functions related to undo/redo/pawn promotion/check/checkmate/draw can also be found here.
    /// </summary>
    public class BoardInformation : MonoBehaviour
    {
        private enum Colours { White, Black };
        private Colours turn;

        // 2D chess layout with individual piece GameObject
        public GameObject[,] Board { get; } = new GameObject[8, 8];

        private PieceAction pieceAction;

        private List<GameObject> piecesOnBoard;

        private GameObject blackKing;
        private GameObject whiteKing;

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

        private Renderer blackArrowRenderer;
        private Renderer whiteArrowRenderer;

        // Piece Layer
        private LayerMask blackPieces;
        private LayerMask whitePieces;

        // Variables related to pawn promotion
        public Mesh Mesh { get; set; }
        public bool Promoted { get; set; }
        public bool MeshChosen { get; set; }
        public GameObject pawnPromo;

        public GameObject boardMenuTileText;
        public GameObject boardMenuTileTextTwo;
        public Mesh pawnMesh;

        // Display texts
        private Text leftDisplayText;
        private Text rightDisplayText;
        private Text whiteDisplayText;
        private Text blackDisplayText;

        private TextMeshPro whiteForfeitText;
        private TextMeshPro blackForfeitText;

        void Start()
        {
            CanMove = true;
            piecesOnBoard = new List<GameObject>();

            foreach (GameObject piece in GameObject.FindGameObjectsWithTag("pieces"))
            {
                PieceInformation info = piece.GetComponent<PieceInformation>();
                int x = info.CurrentXPosition;
                int z = info.CurrentZPosition;
                Board[z, x] = piece;

                // Initialise kings
                if (info.type == PieceInformation.Type.King)
                {
                    if (info.colour == PieceInformation.Colour.White)
                    {
                        whiteKing = piece;
                    }
                    else
                    {
                        blackKing = piece;
                    }
                }

                // Add piece to piecesOnBoard list to keep track of all pieces on board
                piecesOnBoard.Add(piece);
            }

            // Assign null for empty positions
            for (int i = 2; i <= 5; i++)
            {
                for (int j = 0; j <= 7; j++)
                {
                    Board[i, j] = null;
                }
            }

            // Initialise whose turn it is
            turn = Colours.White;

            // Initialise data structure
            pieceAction = GetComponent<PieceAction>();

            // Turn off collisions for all pieces if ghost animation is active
            if (ghostActive)
            {
                Physics.IgnoreLayerCollision(14, 14);
                Physics.IgnoreLayerCollision(15, 15);
                Physics.IgnoreLayerCollision(14, 15);
            }

            // Disable the result display
            EndGameResult.SetActive(false);
            pawnPromo.SetActive(false);

            blackArrowRenderer = blackArrow.GetComponent<Renderer>();
            whiteArrowRenderer = whiteArrow.GetComponent<Renderer>();

            blackPieces = LayerMask.NameToLayer("BlackPieces");
            whitePieces = LayerMask.NameToLayer("WhitePieces");

            whiteDisplayText = whiteText.GetComponent<Text>();
            blackDisplayText = blackText.GetComponent<Text>();
            leftDisplayText = leftText.GetComponent<Text>();
            rightDisplayText = rightText.GetComponent<Text>();

            whiteForfeitText = boardMenuTileText.GetComponent<TextMeshPro>();
            blackForfeitText = boardMenuTileTextTwo.GetComponent<TextMeshPro>();
        }

        public int GetTurn() { return (int)turn; }

        public List<GameObject> GetPieceAvailable() { return piecesOnBoard; }

        public GameObject GetBlackKing() { return blackKing; }

        public GameObject GetWhiteKing() { return whiteKing; }

        public LayerMask GetChessboardLayer()
        {
            return chessboardLayer;
        }

        /// <summary>
        /// If ghosting enabled in the menu, Ghosting = true, and vice versa
        /// </summary>
        public void toggleGhosting()
        {
            // Pieces ignore collisions if ghosting is on
            if (ghostActive)
            {
                ghostActive = false;

                Physics.IgnoreLayerCollision(blackPieces, whitePieces, false);
                Physics.IgnoreLayerCollision(blackPieces, blackPieces, false);
                Physics.IgnoreLayerCollision(whitePieces, whitePieces, false);
            }
            // Turn collisions back on
            else
            {
                ghostActive = true;

                Physics.IgnoreLayerCollision(blackPieces, whitePieces, true);
                Physics.IgnoreLayerCollision(blackPieces, blackPieces, true);
                Physics.IgnoreLayerCollision(whitePieces, whitePieces, true);
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
                whiteArrowRenderer.material = opponentTurn;
                blackArrowRenderer.material = playerTurn;
            }
            else
            {
                turn = Colours.White;
                whiteArrowRenderer.material = playerTurn;
                blackArrowRenderer.material = opponentTurn;
            }
        }

        public void ResetTurn()
        {
            turn = Colours.Black;
            
            // Change arrow colours and reset turn to white
            NextTurn();
        }

        #region Move Related Functions

        public void RemoveFromBoard(GameObject piece)
        {
            piecesOnBoard.Remove(piece);
        }

        public void UpdateBoard(int prevX, int prevZ, int currentX, int currentZ, GameObject pieceParam = null)
        {
            GameObject piece = pieceParam;

            if (piece == null)
            {
                piece = Board[prevZ, prevX];
            }

            Board[prevZ, prevX] = null;
            Board[currentZ, currentX] = piece;

            PieceInformation pieceInfo = piece.GetComponent<PieceInformation>();
            pieceInfo.CurrentXPosition = currentX;
            pieceInfo.CurrentZPosition = currentZ;
        }

        /// <summary>
        /// Reset/Restart from the Start Position
        /// </summary>
        public void ResetState()
        {
            Check = false;
            GameEnded = false;

            foreach (GameObject restorePiece in MoveHistory.Instance.EliminatedObjects)
            {
                if (restorePiece == null)
                {
                    continue;
                }
                piecesOnBoard.Add(restorePiece);
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

                // Update board
                UpdateBoard(pieceInfo.CurrentXPosition, pieceInfo.CurrentZPosition, pieceInfo.GetOriginalX(), pieceInfo.GetOriginalZ(), piece);

                // Reset to piece's default values
                pieceInfo.CurrentXPosition = pieceInfo.GetOriginalX();
                pieceInfo.CurrentZPosition = pieceInfo.GetOriginalZ();
                pieceInfo.PieceMoves = 0;

                // Reset location
                Vector3 endPosition = new Vector3(pieceInfo.GetOriginalX(), 0, pieceInfo.GetOriginalZ());
                pieceAction.ChangePosition(piece, endPosition, (int)pieceInfo.colour);
            }

            MoveHistory.Instance.Clear();

            // Reset turn - White Start
            ResetTurn();
        }

        /// <summary>
        /// Undoes the last move made
        /// </summary>
        public void UndoState()
        {
            GameEnded = false;
            /// Check if previous move exists
            if (MoveHistory.Instance.Index < 0)
            {
                return;
            }

            //  Retrieve previous move
            ArrayList moveData = MoveHistory.Instance.Undo();

            // If undo'd a check, remove the check path
            if ((bool)moveData[7])
            {
                MoveHistory.Instance.RemoveLastPath();
            }

            // Check = check state prior to the last move
            int checkIndex = MoveHistory.Instance.Index;

            Check = false;
            if (checkIndex >= 0)
            {
                Check = MoveHistory.Instance.Check[checkIndex];
            }

            // Change piece's position
            int lastZPosition = (int) moveData[4];
            int lastXPosition = (int) moveData[3];

            GameObject piece = (GameObject) moveData[2];
            PieceInformation pieceInformation = piece.GetComponent<PieceInformation>();

            if (pieceInformation.BeenPromoted && (bool)moveData[8])
            {
                pieceInformation.BeenPromoted = false;
                UndoPromotion(piece);
            }

            int currentXPosition = pieceInformation.CurrentXPosition;
            int currentZPosition = pieceInformation.CurrentZPosition;
            pieceInformation.CurrentXPosition = lastXPosition;
            pieceInformation.CurrentZPosition = lastZPosition;

            // Subtract one from total moves on current piece
            pieceInformation.PieceMoves--;

            // Move piece
            Vector3 revertPosition = new Vector3(lastXPosition, 0, lastZPosition);
            bool rookPiece = (int)pieceInformation.type == 0 ? true : false; // sliding motion
            pieceAction.ChangePosition(piece, revertPosition, (int)pieceInformation.colour, rookPiece);

            // Update Board
            UpdateBoard(currentXPosition, currentZPosition, lastXPosition, lastZPosition);

            // Check if the last move was a castle 
            if ((int)pieceInformation.type == 4 && Math.Abs(currentXPosition - lastXPosition) == 2)
            {
                int rookX;
                int originalRookX;

                // King castled right 
                if (currentXPosition > lastXPosition)
                {
                    rookX = currentXPosition - 1;
                    originalRookX = 7;
                }

                // King castled left
                else
                {
                    rookX = currentXPosition + 1;
                    originalRookX = 0;
                }

                GameObject rook = Board[currentZPosition, rookX];
                Vector3 rookPos = new Vector3(originalRookX, 0, currentZPosition);

                // Move the rook to its original position
                pieceAction.ChangePosition(rook, rookPos, (int)pieceInformation.colour, true);

                // Update board
                UpdateBoard(rookX, currentZPosition, originalRookX, currentZPosition);
            }
            
            // Reinstate any piece eliminated in the last move
            else if ((bool)moveData[0])
            {
                GameObject reinstate = (GameObject)moveData[1];
                piecesOnBoard.Add(reinstate);
                pieceAction.FadeIn(reinstate);

                // Fix positioning of the reinstated piece
                PieceInformation pieceInfo = reinstate.GetComponent<PieceInformation>();
                Vector3 position = new Vector3(pieceInfo.CurrentXPosition, 0, pieceInfo.CurrentZPosition);
                pieceAction.ChangePosition(reinstate, position, (int)pieceInfo.colour);

                // Update Board
                Board[pieceInfo.CurrentZPosition, pieceInfo.CurrentXPosition] = reinstate;
            }

            // Change player's turn 
            NextTurn();
        }

        #endregion

        #region Check/Checkmate/Draw/Pawn Promotion Functions

        /// <summary>
        /// Displays check for 5 seconds, turning on/off every second
        /// </summary>
        public void CheckDisplay()
        {
            string check = "CHECK!!";
            SetText(check);
            StartCoroutine(FlashText(3, true));
        }

        public void SetText(string text)
        {
            whiteDisplayText.text = text;
            blackDisplayText.text = text;
            leftDisplayText.text = text;
            rightDisplayText.text = text;
        }

        IEnumerator FlashText(int duration, bool flashAll)
        {
            EndGameResult.SetActive(true);
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
        /// <param name="colour"> Colour that won. 0 = white, 1 = black </param>
        public void Checkmate(int colour)
        {
            string checkmate = "CHECKMATE";
            SetText(checkmate);
            EndGame(colour);
        }

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
        /// Forfeit display
        /// </summary>
        /// <param name="colour">The colour of the side that forfeited.</param>
        public void Forfeit(int colour)
        {
            GameEnded = true;
            string text;
            
            // White won
            if (colour == 1)
            {
                text = "Black forfeited!";
                Winner = 1;
            }

            // Black won
            else
            {
                text = "White forfeited!";
                Winner = -1;
            }

            // Display result to players
            SetText(text);
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
                whiteDisplayText.text = draw;
                blackDisplayText.text = draw;
            }
            // White won
            else if (Winner == 1)
            {
                whiteDisplayText.text = won;
                blackDisplayText.text = lose;
            }
            // Black won
            else
            {
                whiteDisplayText.text = lose;
                blackDisplayText.text = won;
            }
        }

        /// <summary>
        /// Called when the pawn reaches the opposite side of the board.
        /// Pawn disappears and player has a chance to choose the piece they want to replace it with.
        /// </summary>
        public IEnumerator PromotePawn(PieceInformation pieceInfo)
        {
            GameObject pawn = pieceInfo.gameObject;
            pawnPromo.SetActive(true);
            string pawnPromotion = "PAWN PROMOTION!";
            SetText(pawnPromotion);
            MoveHistory.Instance.Promoted();

            whiteForfeitText.text = "Promotion Tile";
            blackForfeitText.text = "Promotion Tile";

            EndGameResult.SetActive(true);

            while (!Promoted)
            {
                if (MeshChosen)
                {
                    int x = pieceInfo.CurrentXPosition;
                    int z = pieceInfo.CurrentZPosition;
                    pawn.GetComponent<MeshFilter>().mesh = Mesh;
                    if (string.Compare(Mesh.name, "Rook") == 0)
                    {
                        pieceInfo.type = PieceInformation.Type.Rook;
                    }
                    else if (string.Compare(Mesh.name, "Queen") == 0)
                    {
                        pieceInfo.type = PieceInformation.Type.Queen;
                    }
                    else if (string.Compare(Mesh.name, "Bishop") == 0)
                    {
                        pieceInfo.type = PieceInformation.Type.Bishop;
                    }
                    else if (string.Compare(Mesh.name, "Knight") == 0)
                    {
                        pieceInfo.type = PieceInformation.Type.Knight;
                    }
                    Promoted = true;
                }

                yield return null;
            }
            Promoted = false;
            MeshChosen = false;

            whiteForfeitText.text = "Forfeit Tile";
            blackForfeitText.text = "Forfeit Tile";

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

        #endregion
    }
}