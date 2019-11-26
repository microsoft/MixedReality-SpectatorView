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
    public class PieceInformation : MonoBehaviour
    {
        // Original position on the board 2D array
        private int originalXPosition;
        private int originalZPosition;
        private Vector3 currPos;

        // Current position on the board 2D array
        public int CurrentXPosition { get; set; }
        public int CurrentZPosition { get; set; }

        // Piece dropped fix boolean
        private bool fixingYPosition;

        // Piece colour and type {Rook, Knight, Bishop, Queen, King, Pawn}
        public enum Colour { White, Black };
        public enum Type { Rook, Knight, Bishop, Queen, King, Pawn };
        public Colour colour;
        public Type type;
        public char piece;

        // Tracking number of times the piece has moved
        public int PieceMoves { get; set; }

        // Raycast detect only chessboard layer - for valid position checking
        private LayerMask chessboardLayer;

        // Stores Possible locations the piece can move
        private List<string> possibleMoves;

        private GameObject[,] board;

        // Referencing other scripts
        private GameObject manager;
        private BoardInformation boardInfo;
        private PieceAction pieceAction;
        private GameObject chessboard;
        private GhostPickup ghostPickup;

        // Pawn promotion related variables
        public bool BeenPromoted { get; set; }

        void Awake()
        {
            // Initialising the original and current positions of each piece
            originalXPosition = (int)transform.localPosition.x;
            originalZPosition = (int)transform.localPosition.z;

            CurrentXPosition = originalXPosition;
            CurrentZPosition = originalZPosition;
            currPos = transform.position;

            manager = GameObject.Find("GameManager");
            boardInfo = manager.GetComponent<BoardInformation>();
            chessboard = GameObject.Find("Chessboard");
            pieceAction = manager.GetComponent<PieceAction>();
            ghostPickup = GetComponent<GhostPickup>();
            chessboardLayer = boardInfo.GetChessboardLayer();
        }

        #region Possible positions the piece can move
        /// <summary>
        /// Retrieves all valid positions the piece can move
        /// </summary>
        public void GetMoves()
        {
            // Sandbox mode
            if (boardInfo.GameEnded)
            {
                return;
            }

            // Get Updated Board
            board = boardInfo.Board;

            possibleMoves = new List<string>();
            
            // Global position of the tile the piece is currently on
            GameObject tile = chessboard.transform.GetChild(CurrentZPosition).gameObject.transform.GetChild(CurrentXPosition).gameObject;
            Vector3 globalPosition = new Vector3(tile.transform.position.x, tile.transform.position.y + 0.025f, tile.transform.position.z);

            /// <summary>
            /// Get a list of valid positions the piece can move
            /// Calls the class based on what piece is being manipulated
            /// </summary>
            switch (type)
            {
                case Type.Pawn:
                    possibleMoves = Pawn.Instance.RuleMove(globalPosition, gameObject, board);
                    break;
                case Type.Rook:
                    possibleMoves = Rook.Instance.RuleMove(globalPosition, gameObject, board);
                    break;
                case Type.Bishop:
                    possibleMoves = Bishop.Instance.RuleMove(globalPosition, gameObject, board);
                    break;
                case Type.Knight:
                    possibleMoves = Knight.Instance.RuleMove(gameObject, board);
                    break;
                case Type.Queen:
                    possibleMoves = Queen.Instance.RuleMove(globalPosition, gameObject, board);
                    break;
                case Type.King:
                    possibleMoves = King.Instance.RuleMove(globalPosition, gameObject, board, boardInfo.Check);
                    break;
            }

            // If in check, remove positions that do not block the path or eliminate the piece checking the king.
            // Skip if king as it can move away from the check path
            if (boardInfo.Check && type != Type.King)
            {
                List<string> checkPath = MoveHistory.Instance.GetCheckPath();
                int count = possibleMoves.Count;
                for (int i = count - 1; i >= 0; i--)
                {
                    if (!checkPath.Contains(possibleMoves[i]))
                    {
                        possibleMoves.RemoveAt(i);
                    }
                }
            }
        }

        #endregion

        #region Retrieving/Setting Piece Information

        public int GetOriginalX()
        {
            return originalXPosition;
        }

        public int GetOriginalZ()
        {
            return originalZPosition;
        }

        public bool HasMoved()
        {
            return PieceMoves != 0;
        }

        public List<string> GetPossibleMoves()
        {
            return possibleMoves;
        }

        #endregion

        #region Piece Manipulation

        /// <summary>
        /// Called when the piece is picked up
        /// </summary>
        public void Manipulation()
        {
            // Check if previous move was successfully complete
            // Check if game ended
            if (!boardInfo.CanMove || boardInfo.GameEnded)
            {
                return;
            } 

            // Check If Player's Turn
            if (boardInfo.GetTurn() != (int)colour)
            {
                possibleMoves.Clear();
                return;
            }

            if (boardInfo.ghostActive)
            {
                currPos = transform.position;
                ghostPickup.DuplicatePiece();
            }

            GetMoves();
        }

        /// <summary>
        /// Called when the piece is dropped
        /// </summary>
        public void Moved()
        {
            // Check if previous move was successfully complete
            // Return if false
            if (!boardInfo.CanMove)
            {
                return;
            }
            else
            {
                boardInfo.CanMove = false;
            }
            // Wait for the piece to drop onto the board
            Invoke("CheckValid", 1);

            if (boardInfo.ghostActive)
            {
                GetComponent<MeshRenderer>().enabled = false;
                ghostPickup.EndManipulation();
            }
        }

        void CheckValid()
        {
            // Sandbox mode - Game has ended
            if (boardInfo.GameEnded)
            {
                return;
            }

            // Check if king placed on the forfeit tile
            if (WinRules.CheckForfeit((int)type, (int)colour, gameObject, boardInfo)) {
                KingForfeit();
                return;
            }

            // If piece dropped off the board
            if (transform.localPosition.y <= -0.1f)
            {
                FixPosition();
                return;
            }

            // Declare the new position of the piece
            int newXPosition = -1;
            int newZPosition = -1;

            RaycastHit hit;
            /// <summary>
            /// Raycast down to find the tile the player is currently on
            /// </summary>
            if (Physics.Raycast(transform.position, -transform.up, out hit, 1f, chessboardLayer))
            {
                GameObject pieceCollided = hit.collider.gameObject;
                newXPosition = (int) pieceCollided.name[0] - 65;
                newZPosition = (int) Char.GetNumericValue(pieceCollided.transform.parent.gameObject.name[0]) - 1;
            } 
            // Since the peice can go through the board
            // It can miss the raycast (backup)
            else
            {
                newXPosition = (int)Math.Round(transform.localPosition.x);
                newZPosition = (int)Math.Round(transform.localPosition.z);
            }

            string newPosition = newXPosition.ToString() + " " + newZPosition.ToString();

            // Check if piece can be moved to this position
            if (possibleMoves.Contains(newPosition))
            {
                // If player was under check, check = false
                boardInfo.Check = false;

                string originalPosition = CurrentXPosition.ToString() + " " + CurrentZPosition.ToString();

                // Check if new position has opponent's piece
                if (board[newZPosition, newXPosition] != null)
                {
                    // Destroy opponent's piece at new position
                    GameObject opponentPiece = board[newZPosition, newXPosition];
                    
                    pieceAction.Eliminate(opponentPiece);
                    MoveHistory.Instance.Move(true, opponentPiece, gameObject, originalPosition, newPosition);
                    boardInfo.RemoveFromBoard(opponentPiece);

                    // Reset fifty move to 0
                    WinRules.FiftyMoves = 0;
                }

                // Check if en passant
                else if (type == Type.Pawn && CurrentXPosition != newXPosition)
                {
                    // Destroy opponent's pawn
                    GameObject eliminatedPiece = board[CurrentZPosition, newXPosition];
                    
                    pieceAction.Eliminate(eliminatedPiece);
                    MoveHistory.Instance.Move(true, eliminatedPiece, gameObject, originalPosition, newPosition);
                    board[CurrentZPosition, newXPosition] = null;

                    // reset fifty move to 0
                    WinRules.FiftyMoves = 0;
                }
                else
                {
                    // Reset fifty move to 0 if pawn moved
                    // Else, +1
                    if (type == Type.Pawn)
                    {
                        WinRules.FiftyMoves = 0;
                    } else
                    {
                        WinRules.FiftyMoves += 1;
                    }

                    MoveHistory.Instance.Move(false, null, gameObject, originalPosition, newPosition);

                    // Check if king and castling
                    // xDisplacement (- if moving right, + if moving left)
                    int xDisplacement = CurrentXPosition - newXPosition;
                    if (type == Type.King && Math.Abs(xDisplacement) == 2)
                    {
                        Vector3 endPosition;
                        GameObject rook;

                        int initialRookX;
                        int newRookX;

                        // If moved towards right, move rook to the left
                        if (xDisplacement < 0)
                        {
                            initialRookX = 7;
                            newRookX = 5;
                            rook = board[newZPosition, initialRookX];
                            endPosition = new Vector3(newZPosition, 0, newRookX);
                        }

                        // If moved towards left, move rook to the right
                        else
                        {
                            initialRookX = 0;
                            newRookX = 3;
                            rook = board[newZPosition, initialRookX];
                            endPosition = new Vector3(newZPosition, 0, newRookX);
                        }

                        // Move the rook to new position
                        pieceAction.ChangePosition(rook, endPosition, (int)colour);
                        boardInfo.UpdateBoard(initialRookX, newZPosition, newRookX, newZPosition);
                    }
                }

                boardInfo.UpdateBoard(CurrentXPosition, CurrentZPosition, newXPosition, newZPosition);
                PieceMoves += 1;

                if (boardInfo.ghostActive)
                {
                    StartCoroutine(AnimateMovement(transform.localPosition));
                }
            }
            // Not a valid move, still the player's turn
            else
            {
                FixPosition();
                return;
            }

            if (type == Type.Pawn)
            {
                // Check if in final tile for pawn promotion
                if ((colour == Colour.White && newZPosition == 7) || (colour == Colour.Black && newZPosition == 0))
                {
                    StartCoroutine(boardInfo.PromotePawn(GetComponent<PieceInformation>()));
                    BeenPromoted = true;
                }
                else
                {
                    ContinueProcess();
                }
            } 
            else
            {
                ContinueProcess();
            }
        }

        public void ContinueProcess()
        {
            FixPosition();

            // Check for check conditions after pieces have finished fixing positions
            Invoke("CheckCondition", 1.5f);
        }

        void CheckCondition()
        {
            // Check the board state for win conditions
            GetMoves();

            GameObject king;
            // Obtain the opponent's king
            if (colour == Colour.White)
            {
                king = boardInfo.GetBlackKing();
            }
            else
            {
                king = boardInfo.GetWhiteKing();
            }

            boardInfo.NextTurn();

            PieceInformation kingInfo = king.GetComponent<PieceInformation>();
            String kingPosition = kingInfo.CurrentXPosition + " " + kingInfo.CurrentZPosition;

            if (WinRules.CheckForCheck(possibleMoves, GetComponent<PieceInformation>(), boardInfo, kingPosition))
            {
                return;
            }

            // Check Draw Conditions
            if (WinRules.CheckDraw((int)colour, boardInfo))
            {
                return;
            }
        }

        /// <summary>
        /// Gets all the active pieces of the same colour and starts knock-down animation
        /// </summary>
        void KingForfeit()
        {
            List<GameObject> pieces = boardInfo.GetPieceAvailable();

            // If piece is forfeited player's piece, make piece collapse
            foreach (GameObject piece in pieces)
            {
                PieceInformation thisPiece = piece.GetComponent<PieceInformation>();

                if (thisPiece.type != type && thisPiece.colour == colour)
                {
                    StartCoroutine(pieceAction.FallDown(piece));
                }
            }

            // Display result
            boardInfo.Forfeit((int)colour);
        }

        void FixPosition()
        {
            List<GameObject> availablePieces = boardInfo.GetPieceAvailable();
            foreach (GameObject piece in availablePieces)
            {
                PieceInformation pieceInfo = piece.GetComponent<PieceInformation>();
                Vector3 position = new Vector3(pieceInfo.CurrentXPosition, 0, pieceInfo.CurrentZPosition);
                if (!CheckSimilarity(piece.transform.localPosition, position))
                {
                    pieceAction.ChangePosition(piece, position, (int)pieceInfo.colour);
                }
            }

            Invoke("MoveCompleted", 2.25f);
        }

        void MoveCompleted()
        {
            boardInfo.CanMove = true;
        }

        /// <summary>
        /// Checks if the two vectors are roughly the same
        /// </summary>
        bool CheckSimilarity(Vector3 first, Vector3 second)
        {
            float xDiff = Math.Abs(first.x - second.x);
            float yDiff = Math.Abs(first.y - second.y);
            float zDiff = Math.Abs(first.z - second.z);

            // Not in the same position
            if (xDiff > 0.1 || yDiff > 0.1 || zDiff > 0.1)
            {
                return false;
            }

            // In the same position
            return true;
        }

        /// <summary>
        /// Moves the piece back to the original position, then animates it to the square the player dropped it
        /// </summary>
        private IEnumerator AnimateMovement(Vector3 finalPos)
        {
            var position = new Vector3(CurrentXPosition, 0, CurrentZPosition);
            transform.position = currPos;
            GetComponent<MeshRenderer>().enabled = true;
            ghostPickup.DestroyClone();

            if(type == Type.Knight)
            {
                Vector3 transPosition;
                if(Mathf.Abs(transform.localPosition.x - position.x) < Mathf.Abs(transform.localPosition.z - position.z))
                {
                    transPosition = new Vector3(transform.localPosition.x, 0, position.z);
                }
                else
                {
                    transPosition = new Vector3(position.x, 0, transform.localPosition.z);
                }

                pieceAction.ChangePosition(gameObject, transPosition, (int)colour, true);
                yield return new WaitForSeconds(1.125f);
            }

            pieceAction.ChangePosition(gameObject, position, (int)colour, true);
        }

        #endregion
    }
}