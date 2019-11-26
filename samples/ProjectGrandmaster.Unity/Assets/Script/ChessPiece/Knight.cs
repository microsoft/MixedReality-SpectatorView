// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class Knight
    {
        /// <summary>
        /// Colour of the knight. 
        /// 0 if White, 1 if Black
        /// </summary>
        private int colour;

        /// <summary>
        /// Reference to the board 2D array
        /// </summary>
        private GameObject[,] board;

        /// <summary>
        /// List storing valid move positions for the knight
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
        private static Knight instance;
        public static Knight Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Knight();
                }
                return instance;
            }
        }

        private Knight() { }

        /// <summary>
        /// Returns a list of positions the pawn can move.
        /// list contain strings => "xPosition yPosition"
        /// </summary>
        /// <param name="check"> States if the board state is in check. </param>
        public List<string> RuleMove(GameObject pieceObject, GameObject[,] boardState)
        {
            PieceInformation piece = pieceObject.GetComponent<PieceInformation>();
            colour = (int)piece.colour;
            currentZPosition = piece.CurrentZPosition;
            currentXPosition = piece.CurrentXPosition;

            board = boardState;

            // Initialise new list
            validPositions = new List<string>();

            // Cases: DDL, DDR, UUL, UUR 
            int zDisplacement = 2;
            int xDisplacement = 1;

            // Check if valid position
            // Case: UUL
            if (TwoStepsDisplacement(currentXPosition - xDisplacement, currentZPosition + zDisplacement))
            {
                StorePosition(currentXPosition - xDisplacement, currentZPosition + zDisplacement);
            }

            // Case: UUR
            if (TwoStepsDisplacement(currentXPosition + xDisplacement, currentZPosition + zDisplacement))
            {
                StorePosition(currentXPosition + xDisplacement, currentZPosition + zDisplacement);
            }

            // Case: DDL
            if (TwoStepsDisplacement(currentXPosition - xDisplacement, currentZPosition - zDisplacement))
            {
                StorePosition(currentXPosition - xDisplacement, currentZPosition - zDisplacement);
            }

            // Case: DDR
            if (TwoStepsDisplacement(currentXPosition + xDisplacement, currentZPosition - zDisplacement))
            {
                StorePosition(currentXPosition + xDisplacement, currentZPosition - zDisplacement);
            }

            // Cases: LLU, LLD, RRU, RRD
            zDisplacement = 1;
            xDisplacement = 2;

            // Case: LLU
            if (TwoStepsDisplacement(currentXPosition - xDisplacement, currentZPosition + zDisplacement))
            {
                StorePosition(currentXPosition - xDisplacement, currentZPosition + zDisplacement);
            }

            // Case: LLD
            if (TwoStepsDisplacement(currentXPosition - xDisplacement, currentZPosition - zDisplacement))
            {
                StorePosition(currentXPosition - xDisplacement, currentZPosition - zDisplacement);
            }

            // Case: RRU
            if (TwoStepsDisplacement(currentXPosition + xDisplacement, currentZPosition + zDisplacement))
            {
                StorePosition(currentXPosition + xDisplacement, currentZPosition + zDisplacement);
            }

            // Case: RRD
            if (TwoStepsDisplacement(currentXPosition + xDisplacement, currentZPosition - zDisplacement))
            {
                StorePosition(currentXPosition + xDisplacement, currentZPosition - zDisplacement);
            }

            return validPositions;
        }

        /// <summary>
        /// Check's if knight can move to the new position given in the argument
        /// </summary>
        /// <param name="newXPosition"> x Position knight can move to </param>
        /// <param name="newZPosition"> z Position knight can move to </param>
        /// <returns></returns>
        bool TwoStepsDisplacement(int newXPosition, int newZPosition)
        {
            // Check if newXPosition and newZPosition are within bounds of the 2D board
            // Invalid if outside the bounds, return false;
            if (newXPosition > 7 || newXPosition < 0 || newZPosition > 7 || newZPosition < 0)
            {
                return false;
            }

            // If position is empty, knight can move to position
            if (board[newZPosition, newXPosition] == null)
            {
                return true;
            }

            // Checks if piece in the new location is the player's piece
            // If true, not a valid move, return false
            GameObject pieceObject = board[newZPosition, newXPosition];
            PieceInformation pieceInformation = pieceObject.GetComponent<PieceInformation>();
            if ((int)pieceInformation.colour == colour)
            {
                return false;
            }

            // Opponent's piece in location. Valid move.
            return true;
        }

        /// <summary>
        /// Checks if the knight can move to the x and z position. 
        /// Store location in list if allowed, that is, position is empty or has an enemy piece
        /// </summary>
        /// <returns> true if knight can keep moving in this direction </returns>
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

            if (colour != (int)pieceInformation.colour)
            {
                validPositions.Add(position);
            }

            return false;
        }
    }
}