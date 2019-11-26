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
    /// <summary>
    /// Standard rules of chess 
    /// </summary>
    public class Rules : MonoBehaviour
    {
        /// <summary>
        /// The black mask
        /// </summary>
        public LayerMask blackMask;
        public LayerMask whiteMask;

        /// <summary>
        /// The board object
        /// </summary>
        public GameObject boardObject;

        /// <summary>
        /// Checks if king is on the left or right side of the piece
        /// If so, check to see if the opposite side has the opponent's queen or rook
        /// Cannot move up or down if true
        /// </summary>
        /// <param name="position"> position of the piece picked up by the player </param>
        /// <param name="colour"> colour of the piece being manipulated </param>
        /// <returns> true if opponent's queen or rook is on the opposite side </returns>
        public bool RowCheck(Vector3 position, int colour)
        {
            // Checking Left side of the piece
            if (KingHit(position, new Vector3(-1, 0, 0), false, colour)) { return true; }

            // Checking right side of the piece
            if (KingHit(position, new Vector3(1, 0, 0), false, colour)) { return true; }

            // Piece not blocking check, can move.
            return false;
        }

        /// <summary>
        /// Checks if king is on the top or bottom of the piece 
        /// If so, check to see if the opposite side has the opponent's queen or rook
        /// Cannot move left or right if true
        /// </summary>
        /// <param name="position"> position of the piece picked up by the player </param>
        /// <param name="colour"> colour of the piece being manipulated </param>
        /// <returns> true if opponent's queen or rook is on the opposite side </returns>
        public bool ColumnCheck(Vector3 position, int colour)
        {
            // Checking the top side of the piece
            if (KingHit(position, new Vector3(0, 0, 1), false, colour)) { return true; }

            // Checking bottom side of the piece
            if (KingHit(position, new Vector3(0, 0, -1), false, colour)) { return true; }

            // Piece not blocking check, can move.
            return false;
        }

        /// <summary>
        /// Checks if king is diagonal to the piece (bottom left - top right) (i.e. /)
        /// If so, check to see if the opposite side has the opponent's queen or bishop
        /// Cannot move in the top left - bottom right path (i.e. \) if true
        /// </summary>
        /// <param name="position"> position of the piece picked up by the player </param>
        /// <param name="colour"> colour of the piece being manipulated </param>
        /// <returns> true if opponent's queen or bishop is on the opposite side </returns>
        public bool DiagonalCheckForward(Vector3 position, int colour)
        {
             
            // Checking Top-Right side of the piece
            
            if (KingHit(position, new Vector3(1, 0, 1), true, colour)) { return true; }

             
            // Checking Bottom-Left side of the piece
            
            if (KingHit(position, new Vector3(-1, 0, -1), true, colour)) { return true; }

            
            // Piece not blocking check, can move.
            
            return false;
        }

        /// <summary>
        /// Checks if king is diagonal to the piece (top left - bottom right) (i.e. \)
        /// If so, check to see if the opposite side has the opponent's queen or bishop
        /// Cannot move in the bottom left - top right path (i.e. /) if true
        /// </summary>
        /// <param name="position"> position of the piece picked up by the player </param>
        /// <param name="colour"> colour of the piece being manipulated </param>
        /// <returns> true if opponent's queen or bishop is on the opposite side </returns>
        public bool DiagonalCheckBackward(Vector3 position, int colour)
        {
             
            // Checking Top-Left side of the piece
            
            if (KingHit(position, new Vector3(-1, 0, 1), true, colour)) { return true; }

             
            // Checking Bottom-Right side of the piece
            
            if (KingHit(position, new Vector3(1, 0, -1), true, colour)) { return true; }

            
            // Piece not blocking check, can move.
            
            return false;
        }

        /// <summary>
        /// Raycasts to the direction provided from the location of the piece being manipulated
        /// If king found, check it's opposite side for queen/rook/bishop
        /// </summary>
        /// <param name="position"> position of the piece being manipulated </param>
        /// <param name="direction"> direction that needs to be checked for king </param>
        /// <param name="diagonal"> 
        ///     diagonal = true if checking the opposite is queen or bishop (diagonal cases)
        ///     diagonal = false if checking the opposite is queen or rook (left/right, up/down cases)
        /// </param>
        /// <param name="colour"> colour of the piece being manipulated </param>
        /// <returns> true if moving the piece the player is manipulating will leave king compromised to a check </returns>
        bool KingHit(Vector3 position, Vector3 direction, bool diagonal, int colour)
        {
            LayerMask layer = whiteMask;
            if (colour == 0)
            {
                layer = blackMask;
            }
            RaycastHit hit;
            
            // Raycast onto chess pieces only - provided in the layer
            if (Physics.Raycast(position, direction, out hit, 1f, layer))
            {
                GameObject pieceCollided = hit.collider.gameObject;
                PieceInformation piece = pieceCollided.GetComponent<PieceInformation>();

                
                // If piece in direction is player's king, check its opposite.
                if ((int)piece.type == 4 && (int)piece.colour == colour)
                {
                    return FindOpposite(position, new Vector3(direction.x * -1, direction.y, direction.z * -1), diagonal, colour);
                }
            }
            return false;
        } 

        bool FindOpposite(Vector3 position, Vector3 direction, bool diagonal, int colour)
        {
            LayerMask layer = whiteMask;
            if (colour == 0)
            {
                layer = blackMask;
            }
            RaycastHit hit;
            if (Physics.Raycast(position, direction, out hit, 1f, layer))
            {
                GameObject pieceCollided = hit.collider.gameObject;
                PieceInformation piece = pieceCollided.GetComponent<PieceInformation>();

                
                // Moving the piece will not result in check if opposite is either
                // king, pawn, knight or player's piece
                if ((int)piece.type == 5 || (int)piece.type == 1 || (int)piece.type == 4 || (int)piece.colour == colour)
                {
                    return false;
                }

                
                // Rook or queen directly opposite to the king
                // Will compromise into a check if piece moved
                if (!diagonal && ((int)piece.type == 0 || (int)piece.type == 3))
                {
                    return true;
                }

                
                // bishop or queen directly opposite to the king
                // Will compromise into a check if piece moved
                if (diagonal && ((int)piece.type == 3 || (int)piece.type == 2))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if pawn can eliminate the opponent's pawn as per En Passant rule. 
        /// </summary>
        /// <param name="position"> Position of the pawn </param>
        /// <param name="colour"> Colour of the pawn </param>
        /// <returns> True if pawn can be eliminated </returns>
        public bool EnPassant(Vector3 position, Vector3 direction, int colour)
        {
            LayerMask layer = whiteMask;
            if (colour == 0)
            {
                layer = blackMask;
            }
            RaycastHit hit;

            if (Physics.Raycast(position, direction, out hit, 0.055f, layer))
            {
                GameObject pieceCollided = hit.collider.gameObject;
                PieceInformation piece = pieceCollided.GetComponent<PieceInformation>();

                
                // Check if opponent's pawn in the direction provided.
                if ((int)piece.type == 5 && (int)piece.colour != colour)
                {
                    
                    // Check if the pawn is displaced two positions from its original position
                    if (Math.Abs(piece.CurrentZPosition - piece.GetOriginalZ()) != 2)
                    {
                        return false;
                    }

                    
                    // Check if the pawn has only moved once.
                    if (piece.PieceMoves != 1)
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if king can be castled in the direction provided.
        /// Occurs  when neither the king nor the rook have moved
        /// </summary>
        /// <param name="position"> King's position </param>
        /// <param name="direction"> Direction we're checking for castling </param>
        /// <param name="colour"> Colour of the king </param>
        /// <returns> true if king can be castled in that direction </returns>
        public bool Castling(Vector3 position, Vector3 direction, int colour)
        {
            LayerMask layer = whiteMask;
            if (colour == 0)
            {
                layer = blackMask;
            }
            RaycastHit hit;

            if (Physics.Raycast(position, direction, out hit, 1f, layer))
            {
                GameObject pieceCollided = hit.collider.gameObject;
                PieceInformation piece = pieceCollided.GetComponent<PieceInformation>();

                
                // Check if player's rook in the direction
                if ((int)piece.type == 0 && (int)piece.colour == colour)
                {
                    
                    // Check if rook has been moved.
                    if (!piece.HasMoved())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if king can move to the position provided in parameter
        /// </summary>
        /// <param name="tileX"> x position of the boardObject tile where king can move </param>
        /// <param name="tileZ"> z position of the boardObject tile where king can move</param>
        /// <param name="colour"> King's colour </param>
        /// <param name="board"> 2D boardObject layout </param>
        /// <returns> true if king won't be under check in new position </returns>
        public bool KingMove(int tileX, int tileZ, int colour, GameObject[,] board)
        {
            
            // Gets the global position of the tile in the game
            GameObject tile = boardObject.transform.GetChild(tileZ).gameObject.transform.GetChild(tileX).gameObject;
            Vector3 globalPosition = new Vector3(tile.transform.position.x, tile.transform.position.y + 0.025f, tile.transform.position.z);

            
            // If under attack by rook or queen in new position, return false
            if (CheckByRookQueen(globalPosition, colour))
            {
                return false;
            }

            // If under attack by queen or bishop in new position, return false
            if (CheckByQueenBishop(globalPosition, colour))
            {
                return false;
            }
            
            // If under attack by knight in new position, return false
            if (CheckByKnight(tileX, tileZ, colour, board))
            {
                return false;
            }

            // If under attack by pawn in new position, return false
            if (CheckByPawn(globalPosition, colour))
            {
                return false;
            }

            // If under attack by opponent's king in new position, return false
            if (CheckByKing(globalPosition, colour))
            {
                return false;
            }
            
            // Valid position for the king to mov
            return true;
        }

        /// <summary>
        /// Checks if opponent's rook or queen has control over the new position tile
        /// Only checks up/down and left/right 
        /// </summary>
        /// <param name="position"> Position being checked </param>
        /// <param name="colour"> King's colour </param>
        /// <returns> true if king will be under attack by rook or queen in new position </returns>
        bool CheckByRookQueen(Vector3 position, int colour)
        {
            // Type = 0 for Rook and queen
            int type = 0;
            
            // Check right side for opponent's rook or queen. If not valid move, return true
            if (!ValidMove(position, new Vector3(1, 0, 0), colour, type)) {
                return true;
            }
            
            // Check left side for opponent's rook or queen. If not valid move, return true
            if (!ValidMove(position, new Vector3(-1, 0, 0), colour, type))
            {
                return true;
            }

            // Check top side for opponent's rook or queen. If not valid move, return true
            if (!ValidMove(position, new Vector3(0, 0, 1), colour, type))
            {
                return true;
            }
                        
            // Check bottom side for opponent's rook or queen. If not valid move, return true
            
            if (!ValidMove(position, new Vector3(0, 0, -1), colour, type))
            {
                return true;
            }

            // King will not be under attack by queen or rook in the new position
            return false;
        }

        /// <summary>
        /// Checks if opponent's queen or bishop has control over the new position tile
        /// Only checks diagonally
        /// </summary>
        /// <param name="position"> Position being checked </param>
        /// <param name="colour"> King's colour </param>
        /// <returns> true if king will be under attack by queen or bishop in new position </returns>
        bool CheckByQueenBishop(Vector3 position, int colour)
        {            
            // Type = 1 for Queen and Bishop            
            int type = 1;
            
            // Check top-right side for opponent's queen or bishop. If not valid move, return true            
            if (!ValidMove(position, new Vector3(1, 0, 1), colour, type))
            {
                return true;
            }
            
            // Check top-left side for opponent's queen or bishop. If not valid move, return true            
            if (!ValidMove(position, new Vector3(-1, 0, 1), colour, type))
            {
                return true;
            }
            
            // Check bottom-right side for opponent's queen or bishop. If not valid move, return true            
            if (!ValidMove(position, new Vector3(1, 0, -1), colour, type))
            {
                return true;
            }

            // Check bottom-left side for opponent's queen or bishop. If not valid move, return true
            if (!ValidMove(position, new Vector3(-1, 0, -1), colour, type))
            {
                return true;
            }
            
            // King will not be under attack by queen or bishop in the new position            
            return false;
        }

        /// <summary>
        /// Checks if opponent's knight has control over the new position tile
        /// </summary>
        /// <param name="position"> Position being checked </param>
        /// <param name="colour"> King's colour </param>
        /// <param name="board"> 2D boardObject layout </param>
        /// <returns> true if king will be under attack by the knight in new position </returns>
        bool CheckByKnight(int xPos, int zPos, int colour, GameObject[,] board)
        {
            // Checking for cases: UUL, UUR, DDL, DDR
            int xDisplacement = 1;
            int zDisplacement = 2;

            // Case: UUL            
            if (KnightHelperMethod(xPos - xDisplacement, zPos + zDisplacement, board, colour))
            {
                return true;
            }
            
            // Case: UUR            
            if (KnightHelperMethod(xPos + xDisplacement, zPos + zDisplacement, board, colour))
            {
                return true;
            }
            
            // Case: DDL            
            if (KnightHelperMethod(xPos - xDisplacement, zPos - zDisplacement, board, colour))
            {
                return true;
            }
            
            // Case: DDR            
            if (KnightHelperMethod(xPos + xDisplacement, zPos - zDisplacement, board, colour))
            {
                return true;
            }
            
            // Checking for cases: LLU, LLD, RRU, RRD            
            xDisplacement = 2;
            zDisplacement = 1;
                        
            // Case: LLU            
            if (KnightHelperMethod(xPos - xDisplacement, zPos + zDisplacement, board, colour))
            {
                return true;
            }
            
            // Case: LLD
            if (KnightHelperMethod(xPos - xDisplacement, zPos - zDisplacement, board, colour))
            {
                return true;
            }

            // Case: RRU            
            if (KnightHelperMethod(xPos + xDisplacement, zPos + zDisplacement, board, colour))
            {
                return true;
            }

            // Case: RRD            
            if (KnightHelperMethod(xPos + xDisplacement, zPos - zDisplacement, board, colour))
            {
                return true;
            }
            
            // Opponent's knight does not control the position            
            return false;
        }

        /// <summary>
        /// Checks if opponent's knight has control over the tile
        /// </summary>
        /// <returns> true if knight found </returns>
        bool KnightHelperMethod(int newXPosition, int newZPosition, GameObject[,] board, int colour)
        {
            
            // Check if x and z positions are outside bounds
            
            if (newXPosition > 7 || newXPosition < 0 || newZPosition > 7 || newZPosition < 0)
            {
                return false;
            }

            GameObject piece = board[newZPosition, newXPosition];

            // Check if no piece at location            
            if (piece == null)
            {
                return false;
            }

            PieceInformation pieceInformation = piece.GetComponent<PieceInformation>();
                        
            // if player's piece, return false            
            if ((int)pieceInformation.colour == colour)
            {
                return false;
            }
            
            // If knight, opponent's knight has control over the tile. 
            // King cannot move to this location.            
            if ((int)pieceInformation.type == 1)
            {
                return true;
            }
                        
            // Knight not found. King is probable to move to this location            
            return false;
        }

        /// <summary>
        /// Checks if opponent's pawn has control over the new position tile
        /// Only checks diagonally
        /// </summary>
        /// <param name="position"> Position being checked </param>
        /// <param name="colour"> King's colour </param>
        /// <returns> true if king will be under attack by pawn in new position </returns>
        bool CheckByPawn(Vector3 position, int colour)
        {
            // Type = 2 for Pawn
            int type = 2;
            
            // If king is white, opponent's pawn can only move down. 
            // If king is black, opponent's pawn can only move up.
            int displacement = 1;
            if (colour == 1)
            {
                displacement = -1;
            }
            
            // Sets the raycast distance to look only one position away from the tile
            float range = 0.055f;

            
            // Check if pawn can attack right (diagonally). If not valid move, return true
            if (!ValidMove(position, new Vector3(1, 0, displacement), colour, type, range))
            {
                return true;
            }

            // Check if pawn can attack left (diagonally). If not valid move, return true
            if (!ValidMove(position, new Vector3(-1, 0, displacement), colour, type, range))
            {
                return true;
            }

            // King will not be under attack by a pawn in the new position
            return false;
        }

        /// <summary>
        /// Checks if opponent's king has control over the new position tile
        /// </summary>
        /// <param name="position"> Position being checked </param>
        /// <param name="colour"> King's colour </param>
        /// <returns> true if king will be under attack by opponent's king in new position </returns>
        bool CheckByKing(Vector3 position, int colour)
        {
            // Type = 3 for King
            int type = 3;

            // Sets the raycast distance to look only one position away from the tile
            float range = 0.055f;
            
            // Check right side for opponent's king. If not valid move, return true
            if (!ValidMove(position, new Vector3(1, 0, 0), colour, type, range))
            {
                return true;
            }
            
            // Check left side for opponent's king. If not valid move, return true
            if (!ValidMove(position, new Vector3(-1, 0, 0), colour, type, range))
            {
                return true;
            }
            
            // Check top side for opponent's king. If not valid move, return true            
            if (!ValidMove(position, new Vector3(0, 0, 1), colour, type, range))
            {
                return true;
            }
            
            // Check bottom side for opponent's king. If not valid move, return true            
            if (!ValidMove(position, new Vector3(0, 0, -1), colour, type, range))
            {
                return true;
            }
            
            // Check top-right side for opponent's king. If not valid move, return true            
            if (!ValidMove(position, new Vector3(1, 0, 1), colour, type, range))
            {
                return true;
            }
            
            // Check top-left side for opponent's king. If not valid move, return true            
            if (!ValidMove(position, new Vector3(-1, 0, 1), colour, type, range))
            {
                return true;
            }
            
            // Check bottom-right side for opponent's king. If not valid move, return true            
            if (!ValidMove(position, new Vector3(1, 0, -1), colour, type, range))
            {
                return true;
            }
            
            // Check bottom-left side for opponent's king. If not valid move, return true            
            if (!ValidMove(position, new Vector3(-1, 0, -1), colour, type, range))
            {
                return true;
            }
            
            // King will not be under attack by the opponent's king in the new position            
            return false;
        }

        /// <summary>
        /// Checks if the tile is being controlled by the enemy
        /// </summary>
        /// <param name="position"> Position being checked </param>
        /// <param name="direction"> direction being checked for opponent's piece </param>
        /// <param name="colour"> King's colour </param>
        /// <param name="type">
        /// type being checked against.
        /// 0 if Rook and Queen
        /// 1 if Queen and Bishop
        /// 2 if Pawn
        /// 3 if King
        /// </param>
        /// <param name="range"> Optional parameter to change the raycast distance </param>
        /// <returns> true if tile is not controlled by the opponent's piece that is being checked </returns>
        bool ValidMove(Vector3 position, Vector3 direction, int colour, int type, float range = 1f)
        {
            LayerMask layer = whiteMask;
            if (colour == 0)
            {
                layer = blackMask;
            }
            RaycastHit hit;
            if (Physics.Raycast(position, direction, out hit, range, layer))
            {
                GameObject pieceCollided = hit.collider.gameObject;
                PieceInformation piece = pieceCollided.GetComponent<PieceInformation>();

                // If player's piece is controlling the tile, moving into position is plausible. 
                if ((int)piece.colour == colour)
                {
                    return true;
                }

                // Obtains the piece type to check if the raycast piece was what was being checked
                int pieceType = (int)piece.type;

                // Checks to see if the opponent's being checked is controlling the tile
                // Not a valid location for king to move. Return false
                if (type == 0 && (pieceType == 0 || pieceType == 3))
                {
                    return false;
                }

                if (type == 1 && (pieceType == 2 || pieceType == 3))
                {
                    return false;
                } 

                if (type == 2 && pieceType == 5)
                {
                    return false;
                }

                if (type == 3 && pieceType == 4)
                {
                    return false;
                }
            }

            // Piece being checked does not control the tile.
            return true;
        }
    }
}
