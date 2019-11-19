﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
        int originalXPosition;
        int originalZPosition;
        Vector3 currPos;

        // Current position on the board 2D array
        int currentXPosition;
        int currentZPosition;

        // Piece dropped fix boolean
        bool fixingYPosition;

        // Piece colour and type {Rook, Knight, Bishop, Queen, King, Pawn}
        public enum Colour { White, Black };
        public enum Type { Rook, Knight, Bishop, Queen, King, Pawn };
        public Colour colour;
        public Type type;
        public char piece;

        // Raycast detect only chessboard layer - for valid position checking
        LayerMask chessboardLayer;

        // Stores Possible locations the piece can move
        List<string> possibleMoves;

        GameObject[,] board;

        // Referencing other scripts
        GameObject manager;
        BoardInformation boardInfo;
        PieceAction pieceAction;
        GameObject chessboard;
        GhostPickup ghostPickup;

        // Pawn promotion related variables
        public bool BeenPromoted { get; set; }

        void Awake()
        {
            // Initialising the original and current positions of each piece
            originalXPosition = (int)transform.localPosition.x;
            originalZPosition = (int)transform.localPosition.z;

            currentXPosition = originalXPosition;
            currentZPosition = originalZPosition;
            currPos = transform.position;

            manager = GameObject.Find("GameManager");
            boardInfo = manager.GetComponent<BoardInformation>();
            chessboard = GameObject.Find("Chessboard");
            pieceAction = manager.GetComponent<PieceAction>();
            ghostPickup = GetComponent<GhostPickup>();
            boardInfo.CanMove = true;
            chessboardLayer = boardInfo.GetChessboardLayer();
        }

        /////////////////////////////////////// Determines possible positions the piece can move, and stores it in an arraylist ///////////////////////////////////////

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
            board = boardInfo.GetBoard();

            possibleMoves = new List<string>();
            
            // Global position of the tile the piece is currently on
            GameObject tile = chessboard.transform.GetChild(currentZPosition).gameObject.transform.GetChild(currentXPosition).gameObject;
            Vector3 globalPosition = new Vector3(tile.transform.position.x, tile.transform.position.y + 0.025f, tile.transform.position.z);

            /// <summary>
            /// Get a list of valid positions the piece can move
            /// Calls the class based on what piece is being manipulated
            /// </summary>
            switch (type)
            {
                case Type.Pawn:
                    possibleMoves = Pawn.RuleMove(globalPosition, gameObject, board);
                    break;
                case Type.Rook:
                    possibleMoves = Rook.RuleMove(globalPosition, gameObject, board);
                    break;
                case Type.Bishop:
                    possibleMoves = Bishop.RuleMove(globalPosition, gameObject, board);
                    break;
                case Type.Knight:
                    possibleMoves = Knight.RuleMove(gameObject, board);
                    break;
                case Type.Queen:
                    possibleMoves = Queen.RuleMove(globalPosition, gameObject, board);
                    break;
                case Type.King:
                    possibleMoves = King.RuleMove(globalPosition, gameObject, board, boardInfo.Check);
                    break;
            }

            // If in check, remove positions that do not block the path or eliminate the piece checking the king.
            // Skip if king as it can move away from the check path
            if (boardInfo.Check && type != Type.King)
            {
                List<string> checkPath = MoveDataStructure.GetCheckPath();
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

        //////////////////////////////////////////////////////////////// Retrieving/Setting Piece Information ////////////////////////////////////////////////////////////////

        public int GetXPosition()
        {
            return currentXPosition;
        }

        public int GetZPosition()
        {
            return currentZPosition;
        }

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

        public void SetXPosition(int newPosition)
        {
            currentXPosition = newPosition;
        }

        public void SetZPosition(int newPosition)
        {
            currentZPosition = newPosition;
        }

        // Tracking number of times the piece has moved
        public int PieceMoves { get; set; }

        ////////////////////////////////////// Piece Manipulation //////////////////////////////////////

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
                KingForfeited();
                return;
            }

            // If piece dropped off the board
            if (transform.localPosition.y <= -0.1f)
            {
                FixPosition();
                boardInfo.CanMove = true;
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
            // It can miss the raycast
            else
            {
                newXPosition = (int)Math.Round(transform.localPosition.x);
                newZPosition = (int)Math.Round(transform.localPosition.z);
            }

            string newPosition = newXPosition.ToString() + " " + newZPosition.ToString();

            // Check if piece can be moved to this position
            if (possibleMoves.Contains(newPosition))
            {
                // If player was under check, check = false and clear checkpath
                if (boardInfo.Check)
                {
                    boardInfo.Check = false;
                }

                string originalPosition = currentXPosition.ToString() + " " + currentZPosition.ToString();

                // Check if new position has opponent's piece
                if (board[newZPosition, newXPosition] != null)
                {
                    // Destroy opponent's piece at new position
                    GameObject eliminatedPiece = board[newZPosition, newXPosition];
                    
                    pieceAction.Eliminate(eliminatedPiece);
                    MoveDataStructure.Move(true, eliminatedPiece, gameObject, originalPosition, newPosition);
                    boardInfo.RemovedFromBoard(eliminatedPiece);

                    // Reset fifty move to 0
                    WinRules.FiftyMoves = 0;
                }
                // Check if en passant
                else if (type == Type.Pawn && currentXPosition != newXPosition)
                {
                    // Destroy opponent's pawn
                    GameObject eliminatedPiece = board[currentZPosition, newXPosition];
                    
                    pieceAction.Eliminate(eliminatedPiece);
                    MoveDataStructure.Move(true, eliminatedPiece, gameObject, originalPosition, newPosition);
                    board[currentZPosition, newXPosition] = null;

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

                    MoveDataStructure.Move(false, null, gameObject, originalPosition, newPosition);

                    // Check if king and castling
                    // xDisplacement (- if moving right, + if moving left)
                    int xDisplacement = currentXPosition - newXPosition;
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

                boardInfo.UpdateBoard(currentXPosition, currentZPosition, newXPosition, newZPosition);
                PieceMoves += 1;

                if (boardInfo.ghostActive)
                {
                    StartCoroutine(AnimateMovement(transform.localPosition));
                }
            }
            // Not a valid move, still the player's turn
            else
            {
                boardInfo.CanMove = true;
                FixPosition();
                return;
            }

            if (type == Type.Pawn)
            {
                // Check if in final tile
                if ((colour == Colour.White && newZPosition == 7) || (colour == Colour.Black && newZPosition == 0))
                {
                    StartCoroutine(boardInfo.PromotePawn(GetComponent<PieceInformation>(), gameObject));
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
            if (!boardInfo.ghostActive)
            {
                FixPosition();
            }
            

            boardInfo.CanMove = true;
            boardInfo.NextTurn();

            // If already in check, skip
            if (boardInfo.Check) { return; }

            // Check the board state for win conditions
            GetMoves();

            // Check after pieces have finished fixing positions
            Invoke("CheckCondition", 1f);
        }

        void CheckCondition()
        {
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

            PieceInformation kingInfo = king.GetComponent<PieceInformation>();
            String kingPosition = kingInfo.GetXPosition() + " " + kingInfo.GetZPosition();

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
        /// Gets all the active pieces of the same colour and starts the animation for knocking them down
        /// </summary>
        void KingForfeited()
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
            boardInfo.Forfeited((int)colour);
        }

        void FixPosition()
        {
            List<GameObject> availablePieces = boardInfo.GetPieceAvailable();
            foreach (GameObject piece in availablePieces)
            {
                PieceInformation pieceInfo = piece.GetComponent<PieceInformation>();
                Vector3 position = new Vector3(pieceInfo.GetXPosition(), 0, pieceInfo.GetZPosition());
                if (!CheckSimilarity(piece.transform.localPosition, position))
                {
                    pieceAction.ChangePosition(piece, position, (int)pieceInfo.colour);
                }
            }
        }

        // Checks if the two vectors are roughly the same
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

        // Moves the piece back to the original position, then animates it to the square the player dropped it
        private IEnumerator AnimateMovement(Vector3 finalPos)
        {
            var position = new Vector3(GetXPosition(), 0, GetZPosition());
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
    }
}