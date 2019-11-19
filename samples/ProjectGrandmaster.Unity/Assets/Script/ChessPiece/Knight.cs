using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.SpectatorView.ProjectGrandmaster;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    public class Knight : MonoBehaviour
    {
        /// <summary>
        /// Colour of the knight. 
        /// 0 if White, 1 if Black
        /// </summary>
        static int colour;

        /// <summary>
        /// Reference to the board 2D array
        /// </summary>
        static GameObject[,] board;

        /// <summary>
        /// List storing valid move positions for the knight
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
        public static List<string> RuleMove(GameObject pieceObject, GameObject[,] boardState)
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
            /// Cases: DDL, DDR, UUL, UUR 
            /// </summary>
            int zDisplacement = 2;
            int xDisplacement = 1;

            /// <summary>
            /// Check if valid position
            /// Case: UUL
            /// </summary>
            if (TwoStepsDisplacement(currentXPosition - xDisplacement, currentZPosition + zDisplacement))
            {
                StorePosition(currentXPosition - xDisplacement, currentZPosition + zDisplacement);
            }

            /// <summary>
            /// Case: UUR
            /// </summary>
            if (TwoStepsDisplacement(currentXPosition + xDisplacement, currentZPosition + zDisplacement))
            {
                StorePosition(currentXPosition + xDisplacement, currentZPosition + zDisplacement);
            }

            /// <summary>
            /// Case: DDL
            /// </summary>
            if (TwoStepsDisplacement(currentXPosition - xDisplacement, currentZPosition - zDisplacement))
            {
                StorePosition(currentXPosition - xDisplacement, currentZPosition - zDisplacement);
            }

            /// <summary>
            /// Case: DDR
            /// </summary>
            if (TwoStepsDisplacement(currentXPosition + xDisplacement, currentZPosition - zDisplacement))
            {
                StorePosition(currentXPosition + xDisplacement, currentZPosition - zDisplacement);
            }

            /// <summary>
            /// Cases: LLU, LLD, RRU, RRD
            /// </summary>
            zDisplacement = 1;
            xDisplacement = 2;

            /// <summary>
            /// Case: LLU
            /// </summary>
            if (TwoStepsDisplacement(currentXPosition - xDisplacement, currentZPosition + zDisplacement))
            {
                StorePosition(currentXPosition - xDisplacement, currentZPosition + zDisplacement);
            }

            /// <summary>
            /// Case: LLD
            /// </summary>
            if (TwoStepsDisplacement(currentXPosition - xDisplacement, currentZPosition - zDisplacement))
            {
                StorePosition(currentXPosition - xDisplacement, currentZPosition - zDisplacement);
            }

            /// <summary>
            /// Case: RRU
            /// </summary>
            if (TwoStepsDisplacement(currentXPosition + xDisplacement, currentZPosition + zDisplacement))
            {
                StorePosition(currentXPosition + xDisplacement, currentZPosition + zDisplacement);
            }

            /// <summary>
            /// Case: RRD
            /// </summary>
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
        static bool TwoStepsDisplacement(int newXPosition, int newZPosition)
        {
            /// <summary>
            /// Check if newXPosition and newZPosition are within bounds of the 2D board
            /// Invalid if outside the bounds, return false;
            /// </summary>
            if (newXPosition > 7 || newXPosition < 0 || newZPosition > 7 || newZPosition < 0)
            {
                return false;
            }

            /// <summary>
            /// If position is empty, knight can move to position
            /// </summary>
            if (board[newZPosition, newXPosition] == null)
            {
                return true;
            }

            /// <summary>
            /// Checks if piece in the new location is the player's piece
            /// If true, not a valid move, return false
            /// </summary>
            GameObject pieceObject = board[newZPosition, newXPosition];
            PieceInformation pieceInformation = pieceObject.GetComponent<PieceInformation>();
            if ((int)pieceInformation.colour == colour)
            {
                return false;
            }

            /// <summary>
            /// Opponent's piece in location. Valid move.
            /// </summary>
            return true;
        }

        /// <summary>
        /// Checks if the knight can move to the x and z position. 
        /// Store location in list if allowed, that is, position is empty or has an enemy piece
        /// </summary>
        /// <returns> true if knight can keep moving in this direction </returns>
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