// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class Pawn
    {
        /// <summary>
        /// Colour of the pawn. 
        /// 0 if White, 1 if Black
        /// </summary>
        private int colour;

        /// <summary>
        /// Reference to the board 2D array
        /// </summary>
        private GameObject[,] board;

        /// <summary>
        /// List storing valid move positions for the pawn
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
        private static Pawn instance;
        public static Pawn Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Pawn();
                }
                return instance;
            }
        }

        private Pawn() { }

        /// <summary>
        /// Returns a list of positions the pawn can move.
        /// list contain strings => "xPosition yPosition"
        /// </summary>
        public List<string> RuleMove(Vector3 globalPosition, GameObject pieceObject, GameObject[,] boardState)
        {
            PieceInformation piece = pieceObject.GetComponent<PieceInformation>();
            colour = (int)piece.colour;
            currentZPosition = piece.GetZPosition();
            currentXPosition = piece.GetXPosition();

            board = boardState;

            // Initialise new list
            validPositions = new List<string>();

            // Check if king is compromised if pawn is moved up/down
            // Check if king is compromised if pawn moves top-left/ top-right
            // Check if king is compromised if pawn moves, from top-left or top-right
            // (substitute top with bottom if black)
            bool columnMovement = Rules.RowCheck(globalPosition, colour);
            bool rowMovement = Rules.ColumnCheck(globalPosition, colour);
            bool topLeftMovement = Rules.DiagonalCheckForward(globalPosition, colour);
            bool topRightMovement = Rules.DiagonalCheckBackward(globalPosition, colour);

            // King will be left compromised if pawn moves in the upwards direction
            // return empty list
            if (columnMovement)
            {
                return validPositions;
            }

            // Z position pawn can move to.
            // If black, move down, -1
            // If white, move up, +1
            int change = 1;
            if (colour == 1) { change = -1; }
            int displacement = currentZPosition + change;

            // Check if no piece ahead of the pawn.
            // Check if moving the piece will not leave the king compromised to a check diagonally
            // Move allowed if true
            if (board[displacement, currentXPosition] == null && !(topRightMovement || topLeftMovement))
            {
                StorePosition(currentXPosition, displacement);

                // If pawn hasn't been moved, check if it can move two positions
                if (!piece.HasMoved() && board[displacement + change, currentXPosition] == null)
                {
                    StorePosition(currentXPosition, displacement + change);
                }
            }

            // King will be left compromised if pawn moves diagonally
            // return list
            if (rowMovement)
            {
                return validPositions;
            }

            // Taking a piece left or right will not leave the king compromised to a check
            if (!topRightMovement)
            {
                // Check if piece in top-right position && top-left position is not outside the bound
                int right = currentXPosition + 1;

                if (right <= 7 && board[displacement, right] != null)
                {
                    StorePosition(right, displacement);
                }

                // Check if en passant condition is met
                // Add position to allowed moves if true
                if (right <= 7 && Rules.EnPassant(globalPosition, new Vector3(1, 0, 0), colour))
                {
                    StorePosition(right, displacement);
                }
            }
            if (!topLeftMovement)
            {
                // Check if piece in top-left position && top-left position is not outside the bound
                int left = currentXPosition - 1;

                if (left >= 0 && board[displacement, left] != null)
                {
                    StorePosition(left, displacement);
                }

                // Check if en passant condition is met
                // Add position to allowed moves if true
                if (left >= 0 && Rules.EnPassant(globalPosition, new Vector3(-1, 0, 0), colour))
                {
                    StorePosition(left, displacement);
                }
            }

            return validPositions;
        }

        /// <summary>
        /// Checks if the pawn can move to the x and z position. 
        /// Store location in list if allowed, that is, position is empty or has an enemy piece
        /// </summary>
        /// <returns> true if pawn can keep moving in this direction </returns>
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

            // If position has an opponent's piece, position is valid but pawn cannot further move in this direction
            if (colour != (int)pieceInformation.colour)
            {
                validPositions.Add(position);
            }

            return false;
        }
    }
}