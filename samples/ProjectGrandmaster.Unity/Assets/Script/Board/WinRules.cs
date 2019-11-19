// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;
using System;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class WinRules : MonoBehaviour
    {
        public static int FiftyMoves { get; set; }

        // not yet implemented
        static bool ThreefoldRepetition()
        {
            return false;
        }

        /// <summary>
        /// Rule: if 50 moves played without moving a pawn or capturing piece
        /// Game ended with draw 
        /// </summary>
        static bool FiftyMoveRule()
        {
            if (FiftyMoves >= 50)
            {
                return true;
            }
            return false;
        }

        static bool Impossibility(BoardInformation boardInfo)
        {
            // Two kings are left
            if (boardInfo.GetPieceAvailable().Count == 2)
            {
                return true;
            }

            // Two kings and one bishop/knight
            if (boardInfo.GetPieceAvailable().Count == 3)
            {
                foreach (GameObject piece in boardInfo.GetPieceAvailable())
                {
                    PieceInformation pieceInfo = piece.GetComponent<PieceInformation>();
                    if ((int)pieceInfo.type == 1 || (int)pieceInfo.type == 2)
                    {
                        return true;
                    }
                }
            }

            // King and Bishop vs King and Bishop
            if (boardInfo.GetPieceAvailable().Count == 4)
            {
                int bishopCount = 0;
                PieceInformation[] bishops = new PieceInformation[2];
                foreach (GameObject piece in boardInfo.GetPieceAvailable())
                {
                    PieceInformation pieceInfo = piece.GetComponent<PieceInformation>();
                    if ((int)pieceInfo.type == 2)
                    {
                        bishops[bishopCount] = pieceInfo;
                        bishopCount++;
                    }
                }

                if (bishopCount == 2)
                {
                    // Cannot be the same colour
                    if (bishops[0].colour == bishops[1].colour)
                    {
                        return false;
                    }

                    // Must be on the same colour tile for it to be a draw
                    if (bishops[0].GetOriginalX() != bishops[1].GetOriginalX())
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Checks if the last move has checked the opponent's king
        /// </summary>
        /// <param name="pieceInfo"> Piece information for the piece checking the king </param>
        /// <param name="validMoves"> Positions piece can move from it's new location </param>
        /// <param name="colour"> Colour of the side that played the last move </param>
        public static bool CheckForCheck(List<string> validMoves, PieceInformation pieceInfo, BoardInformation boardInfo, string kingPos)
        {
            int type = (int)pieceInfo.type;
            int colour = (int)pieceInfo.colour;
            if ((colour == 0 && validMoves.Contains(kingPos)) || (colour == 1 && validMoves.Contains(kingPos)))
            {
                MoveDataStructure.Check();
                boardInfo.Check = true;
                StoreCheckPath(pieceInfo, kingPos);
                if (CheckmateStalemate(colour, boardInfo))
                {
                    boardInfo.Checkmate(colour);
                }
                else
                {
                    boardInfo.CheckDisplay();
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if at least one piece can block the path or eliminate the piece checking the king - if in check
        /// Or
        /// Checks to see if the opponent can play a move - for stalemate condition
        /// </summary>
        /// <returns> True if checkmate/stalemate </returns>
        static bool CheckmateStalemate(int colour, BoardInformation boardInfo)
        {
            foreach (GameObject pieceOnBoard in boardInfo.GetPieceAvailable())
            {
                PieceInformation pieceOnBoardInfo = pieceOnBoard.GetComponent<PieceInformation>();
                
                // skip if not opponent's piece
                if ((int)pieceOnBoardInfo.colour == colour) { continue; }

                pieceOnBoardInfo.GetMoves();
                List<string> allowedPositions = pieceOnBoardInfo.GetPossibleMoves();

                // Break out early if at least one piece can be moved
                if (allowedPositions.Count != 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the current board state satisfies the draw conditions
        /// </summary>
        /// <returns></returns>
        public static bool CheckDraw(int colour, BoardInformation boardInfo)
        {
            if (FiftyMoveRule() || Impossibility(boardInfo) || ThreefoldRepetition() || CheckmateStalemate(colour, boardInfo))
            {
                // Display draw
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the player placed the piece on the forfeit tile
        /// </summary>
        /// 
        /// <returns></returns>
        public static bool CheckForfeit(int type, int colour, GameObject piece, BoardInformation boardInfo)
        {
            // Piece not king
            if (type != 4)
            {
                return false;
            }

            RaycastHit hit;
            if (Physics.Raycast(piece.transform.position, new Vector3(0, -1, 0), out hit, 1f))
            {
                GameObject pieceCollided = hit.collider.gameObject;
                if (string.Compare(pieceCollided.name, "forfeit tile") == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// If in check, store the path - from the piece checking the king to the king
        /// </summary>
        static void StoreCheckPath(PieceInformation pieceInfo, string kingPosition)
        {
            int type = (int)pieceInfo.type;
            int colour = (int)pieceInfo.colour;
            int xPosition = pieceInfo.GetXPosition();
            int zPosition = pieceInfo.GetZPosition();

            int kingX = (int)char.GetNumericValue(kingPosition[0]);
            int kingZ = (int)char.GetNumericValue(kingPosition[2]);

            List<string> checkPath = new List<string>();

            // If check is by knight, getting out of check can only be done by eliminating the knight or moving the king
            // Otherwise, add straight path to the king from current piece
            if (type == 1)
            {
                checkPath.Add(xPosition + " " + zPosition);
            }
            
            // If checked from left or right only 
            // That is, both the piece checking the king and the king are on the same Z position
            else if (kingZ == zPosition)
            {
                for (int i = 0; i < Math.Abs(kingX - xPosition); i++)
                {
                    // King on the right side
                    if (kingX > xPosition)
                    {
                        checkPath.Add((xPosition + i) + " " + zPosition);
                    }
                    else
                    {
                        checkPath.Add((xPosition - i) + " " + zPosition);
                    }
                } 
            }

            // If checked from up or down only
            // That is, both the piece checking the king and the king are on the same X position
            else if (kingX == xPosition)
            {
                for (int i = 0; i < Math.Abs(kingZ - zPosition); i++)
                {
                    // King on the top
                    if (kingZ > zPosition)
                    {
                        checkPath.Add(xPosition + " " + (zPosition + i));
                    }
                    else
                    {
                        checkPath.Add(xPosition + " " + (zPosition - i));
                    }
                }
            }

            // If checked diagonally
            // That is, both the z and x position of the piece checking the king is different to the king
            else
            {
                int distance = Math.Abs(kingX - xPosition);

                for (int i = 0; i < distance; i++)
                {
                    string checkPosition = "";

                    // King is on the right side
                    if (kingX > xPosition)
                    {
                        checkPosition += (xPosition + i) + " ";
                    }
                    else
                    {
                        checkPosition += (xPosition - i) + " ";
                    }

                    // King is on top
                    if (kingZ > zPosition)
                    {
                        checkPosition += (zPosition + i);
                    }
                    else
                    {
                        checkPosition += (zPosition - i);
                    }

                    // Add to the check path array
                    checkPath.Add(checkPosition);
                }
            }

            // Add the path to the MoveDataStructure
            MoveDataStructure.AddCheckPath(checkPath);
        }
    }
}