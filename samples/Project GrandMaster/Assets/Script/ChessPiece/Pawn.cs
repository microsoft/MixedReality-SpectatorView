using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.USYD.ChessPiece;

namespace Microsoft.MixedReality.USYD.ChessPiece
{
    public class Pawn : MonoBehaviour
    {
        /// <summary>
        /// Colour of the pawn. 
        /// 0 if White, 1 if Black
        /// </summary>
        static int colour;

        /// <summary>
        /// Reference to the board 2D array
        /// </summary>
        static GameObject[,] board;

        /// <summary>
        /// List storing valid move positions for the pawn
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
            /// Check if king is compromised if pawn is moved up/down
            /// Check if king is compromised if pawn moves top-left/ top-right
            /// Check if king is compromised if pawn moves, from top-left or top-right
            /// (substitute top with bottom if black)
            /// </summary>
            bool columnMovement = Rules.RowCheck(globalPosition, colour);
            bool rowMovement = Rules.ColumnCheck(globalPosition, colour);
            bool topLeftMovement = Rules.DiagonalCheckForward(globalPosition, colour);
            bool topRightMovement = Rules.DiagonalCheckBackward(globalPosition, colour);

            /// <summary>
            /// King will be left compromised if pawn moves in the upwards direction
            /// return empty list
            /// </summary>
            if (columnMovement)
            {
                return validPositions;
            }

            /// <summary>
            /// Z position pawn can move to.
            /// If black, move down, -1
            /// If white, move up, +1
            /// </summary>
            int change = 1;
            if (colour == 1) { change = -1; }
            int displacement = currentZPosition + change;

            /// <summary>
            /// Check if no piece ahead of the pawn.
            /// Check if moving the piece will not leave the king compromised to a check diagonally
            /// Move allowed if true
            /// </summary>
            if (board[displacement, currentXPosition] == null && !(topRightMovement || topLeftMovement))
            {
                StorePosition(currentXPosition, displacement);

                /// <summary>
                /// If pawn hasn't been moved, check if it can move two positions
                /// </summary>
                if (!piece.HasMoved() && board[displacement + change, currentXPosition] == null)
                {
                    StorePosition(currentXPosition, displacement + change);
                }
            }

            /// <summary>
            /// King will be left compromised if pawn moves diagonally
            /// return list
            /// </summary>
            if (rowMovement)
            {
                return validPositions;
            }

            /// <summary>
            /// Taking a piece left or right will not leave the king compromised to a check
            /// </summary>
            if (!topRightMovement)
            {
                /// <summary>
                /// Check if piece in top-right position && top-left position is not outside the bound
                /// </summary>
                int right = currentXPosition + 1;

                if (right <= 7 && board[displacement, right] != null)
                {
                    StorePosition(right, displacement);
                }

                /// <summary>
                /// Check if en passant condition is met
                /// Add position to allowed moves if true
                /// </summary>
                if (right <= 7 && Rules.EnPassant(globalPosition, new Vector3(1, 0, 0), colour))
                {
                    StorePosition(right, displacement);
                }
            }
            if (!topLeftMovement)
            {
                /// <summary>
                /// Check if piece in top-left position && top-left position is not outside the bound
                /// </summary>
                int left = currentXPosition - 1;

                if (left >= 0 && board[displacement, left] != null)
                {
                    StorePosition(left, displacement);
                }

                /// <summary>
                /// Check if en passant condition is met
                /// Add position to allowed moves if true
                /// </summary>
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
            /// If position has an opponent's piece, position is valid but pawn cannot further move in this direction
            /// </summary>
            if (colour != (int)pieceInformation.colour)
            {
                validPositions.Add(position);
            }

            return false;
        }
    }
}