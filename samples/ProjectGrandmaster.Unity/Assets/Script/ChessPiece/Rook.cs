// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class Rook
    {
        /// <summary>
        /// Colour of the bishop. 
        /// 0 if White, 1 if Black
        /// </summary>
        private int colour;

        /// <summary>
        /// Reference to the board 2D array
        /// </summary>
        private GameObject[,] board;

        /// <summary>
        /// List storing valid move positions for the bishop
        /// </summary>
        private List<string> validPositions;

        /// <summary>
        /// Current position of the piece on the board
        /// </summary>
        private int currentZPosition;
        private int currentXPosition;

        /// <summary>
        /// The singleton instance
        /// </summary>
        private static Rook instance;
        public static Rook Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Rook();
                }
                return instance;
            }
        }

        private static GameObject gameManager;
        private static Rules rules;

        private Rook()
        {
            gameManager = GameObject.Find("GameManager");
            rules = gameManager.GetComponent<Rules>();
        }

        /// <summary>
        /// Returns a list of positions the pawn can move.
        /// list contain strings => "xPosition yPosition"
        /// </summary>
        public List<string> RuleMove(Vector3 globalPosition, GameObject pieceObject, GameObject[,] boardState)
        {
            PieceInformation piece = pieceObject.GetComponent<PieceInformation>();
            colour = (int)piece.colour;
            currentZPosition = piece.CurrentZPosition;
            currentXPosition = piece.CurrentXPosition;

            board = boardState;

            // Initialise new list
            validPositions = new List<string>();

            // Check if king is compromised if moving up and down
            // Or left and right
            bool rowMovement = rules.ColumnCheck(globalPosition, colour);
            bool columnMovement = rules.RowCheck(globalPosition, colour);

            // King will be left compromised if piece moves in any direction
            // return empty list
            if (rules.DiagonalCheckForward(globalPosition, colour) || rules.DiagonalCheckBackward(globalPosition, colour))
            {
                return validPositions;
            }

            // Moving up or down will not leave the king compromised to a check
            if (!columnMovement)
            {
                // Possible moves upwards
                for (int upwards = currentZPosition + 1; upwards <= 7; upwards++)
                {
                    if (!StorePosition(currentXPosition, upwards))
                    {
                        break;
                    }
                }

                // Possible moves downwards
                for (int downwards = currentZPosition - 1; downwards >= 0; downwards--)
                {
                    if (!StorePosition(currentXPosition, downwards))
                    {
                        break;
                    }
                }
            }

            // Moving left or right will not leave the king compromised to a check
            if (!rowMovement)
            {
                // Possible moves left side
                for (int left = currentXPosition - 1; left >= 0; left--)
                {
                    if (!StorePosition(left, currentZPosition))
                    {
                        break;
                    }
                }

                // Possible moves right side
                for (int right = currentXPosition + 1; right <= 7; right++)
                {
                    if (!StorePosition(right, currentZPosition))
                    {
                        break;
                    }
                }
            }

            // All possible moves for the rook added to the list
            return validPositions;
        }

        /// <summary>
        /// Checks if the rook can move to the x and z position. 
        /// Store location in list if allowed, that is, position is empty or has an enemy piece
        /// </summary>
        /// <returns> true if rook can keep moving in this direction </returns>
        bool StorePosition(int x, int z)
        {
            // Empty position
            string position = x.ToString() + " " + z.ToString();
            if (board[z, x] == null)
            {
                validPositions.Add(position);
                return true;
            }

            // Position has a piece. Valid move if opponent's piece. Invalid if player's piece.
            GameObject piece = board[z, x];
            PieceInformation pieceInformation = piece.GetComponent<PieceInformation>();

            // If position has an opponent's piece, position is valid but rook cannot further move in this direction
            if (colour != (int)pieceInformation.colour)
            {
                validPositions.Add(position);
            }

            return false;
        }

    }
}