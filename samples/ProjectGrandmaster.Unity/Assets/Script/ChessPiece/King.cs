// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{

    public class King : MonoBehaviour
    {
        /// <summary>
        /// Colour of the king. 
        /// 0 if White, 1 if Black
        /// </summary>
        static int colour;

        /// <summary>
        /// Reference to the board 2D array
        /// </summary>
        static GameObject[,] board;

        /// <summary>
        /// List storing valid move positions for the king
        /// </summary>
        static List<string> validPositions;

        /// <summary>
        /// Current position of the piece on the board
        /// </summary>
        static int currentZPosition;
        static int currentXPosition;

        /// <summary>
        /// Returns a list of positions the pawn can move.
        /// list contain strings => "xPosition yPosition"
        /// </summary>
        /// <param name="check"> States if the board state is in check. </param>
        public static List<string> RuleMove(Vector3 position, GameObject pieceObject, GameObject[,] boardState, bool check)
        {
            PieceInformation piece = pieceObject.GetComponent<PieceInformation>();
            colour = (int)piece.colour;
            currentZPosition = piece.GetZPosition();
            currentXPosition = piece.GetXPosition();

            board = boardState;

            /// <summary>
            /// Initialise new list
            /// </summary>
            validPositions = new List<string>();

            /// <summary>
            /// Check if positions the king can move to will compromise the king to a check
            /// </summary>
            for (int xDisplacement = -1; xDisplacement <= 1; xDisplacement++)
            {
                for (int zDisplacement = -1; zDisplacement <= 1; zDisplacement++)
                {
                    /// <summary>
                    /// Skip if checking current position
                    /// </summary> 
                    if (xDisplacement == 0 && zDisplacement == 0)
                    {
                        continue;
                    }

                    /// <summary>
                    /// Skip if position out of bound
                    /// </summary>
                    if (currentXPosition + xDisplacement < 0 || currentXPosition + xDisplacement > 7
                        || currentZPosition + zDisplacement < 0 || currentZPosition + zDisplacement > 7)
                    {
                        continue;
                    }

                    /// <summary>
                    /// Skip if player's piece in position
                    /// </summary>
                    GameObject pieceAtLocation = board[currentZPosition + zDisplacement, currentXPosition + xDisplacement];
                    if (pieceAtLocation != null)
                    {
                        PieceInformation pieceInformation = pieceAtLocation.GetComponent<PieceInformation>();
                        if ((int)pieceInformation.colour == colour)
                        {
                            continue;
                        }
                    }

                    /// <summary>
                    /// Check if king can move to this position without getting compromised
                    /// </summary>
                    if (Rules.KingMove(currentXPosition + xDisplacement, currentZPosition + zDisplacement, colour, board))
                    {
                        StorePosition(currentXPosition + xDisplacement, currentZPosition + zDisplacement);
                    }
                }
            }

            /// <summary>
            /// Add castling to valid move if allowed
            /// </summary>
            if (!check && !piece.HasMoved())
            {
                /// <summary>
                /// Check if king can castle left
                /// </summary>
                Vector3 left = new Vector3(-1, 0, 0);
                if (Rules.Castling(position, left, colour))
                {
                    /// <summary>
                    /// Check if king's new position is controlled by the opponent
                    /// If not, add to list of valid moves
                    /// </summary>
                    if (Rules.KingMove(currentXPosition - 2, currentZPosition, colour, board))
                    {
                        StorePosition(currentXPosition - 2, currentZPosition); 
                    }
                }

                /// <summary>
                /// Check if king can castle right
                /// </summary>
                Vector3 right = new Vector3(1, 0, 0);
                if (Rules.Castling(position, right, colour))
                {
                    /// <summary>
                    /// Check if king's new position is controlled by the opponent
                    /// If not, add to list of valid moves
                    /// </summary>
                    if (Rules.KingMove(currentXPosition + 2, currentZPosition, colour, board))
                    {
                        StorePosition(currentXPosition + 2, currentZPosition);
                    }
                }

            }

            return validPositions;
        }

        /// <summary>
        /// Checks if the king can move to the x and z position. 
        /// Store location in list if allowed, that is, position is empty or has an enemy piece
        /// </summary>
        /// <returns> true if king can keep moving in this direction </returns>
        static bool StorePosition(int x, int z)
        {
            /// <summary>
            /// Empty position
            /// </summary>
            string position = x.ToString() + " " + z.ToString();
            if (board[z, x] == null)
            {
                validPositions.Add(position);
                return true;
            }

            /// <summary>
            /// Position has a piece. Valid move if opponent's piece. Invalid if player's piece.
            /// </summary>
            GameObject piece = board[z, x];
            PieceInformation pieceInformation = piece.GetComponent<PieceInformation>();

            if (colour != (int)pieceInformation.colour)
            {
                validPositions.Add(position);
            }

            return false;
        }
    }
}