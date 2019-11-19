// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class Bishop : MonoBehaviour
    {
        /// <summary>
        /// Colour of the bishop. 
        /// 0 if White, 1 if Black
        /// </summary>
        static int colour;

        /// <summary>
        /// Reference to the board 2D array
        /// </summary>
        static GameObject[,] board;

        /// <summary>
        /// List storing valid move positions for the bishop
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
        public static List<string> RuleMove(Vector3 globalPosition, GameObject pieceObject, GameObject[,] boardState)
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
            /// Check if king is compromised if moving top-right and bottom-left
            /// Or top-left and bottom-right
            /// </summary>
            bool forwards = Rules.DiagonalCheckBackward(globalPosition, colour);
            bool backwards = Rules.DiagonalCheckForward(globalPosition, colour);

            /// <summary>
            /// King will be left compromised if piece moves in any valid direction
            /// return empty list
            /// </summary>
            if (Rules.ColumnCheck(globalPosition, colour) || Rules.RowCheck(globalPosition, colour))
            {
                return validPositions;
            }

            /// <summary>
            /// Moving up-right or down-left will not leave the king compromised to a check
            /// </summary>
            if (!forwards)
            {
                /// <summary>
                /// Possible moves up-right
                /// </summary>
                for (int right = currentXPosition + 1, upwards = currentZPosition + 1; upwards <= 7 && right <= 7; upwards++, right++)
                {
                    if (!StorePosition(right, upwards))
                    {
                        break;
                    }
                }

                /// <summary>
                /// Possible moves bottom-left
                /// </summary>
                for (int left = currentXPosition - 1, downwards = currentZPosition - 1; downwards >= 0 && left >= 0; downwards--, left--)
                {
                    if (!StorePosition(left, downwards))
                    {
                        break;
                    }
                }
            }

            /// <summary>
            /// Moving up-left or down-right will not leave the king compromised to a check
            /// </summary>
            if (!backwards)
            {
                /// <summary>
                /// Possible moves up-left
                /// </summary>
                for (int left = currentXPosition - 1, upwards = currentZPosition + 1; upwards <= 7 && left >= 0; upwards++, left--)
                {
                    if (!StorePosition(left, upwards))
                    {
                        break;
                    }
                }

                /// <summary>
                /// Possible moves bottom-right
                /// </summary>
                for (int right = currentXPosition + 1, downwards = currentZPosition - 1; downwards >= 0 && right <= 7; downwards--, right++)
                {
                    if (!StorePosition(right, downwards))
                    {
                        break;
                    }
                }
            }           

            return validPositions;
        }

        /// <summary>
        /// Checks if the bishop can move to the x and z position. 
        /// Store location in list if allowed, that is, position is empty or has an enemy piece
        /// </summary>
        /// <returns> true if bishop can keep moving in this direction </returns>
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

            /// <summary>
            /// If position has an opponent's piece, position is valid but bishop cannot further move in this direction
            /// </summary>
            if (colour != (int)pieceInformation.colour)
            {
                validPositions.Add(position);
            }

            return false;
        }
    }
}