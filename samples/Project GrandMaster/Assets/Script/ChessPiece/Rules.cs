using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Microsoft.MixedReality.USYD.ChessPiece;

namespace Microsoft.MixedReality.USYD.ChessPiece
{
    public class Rules : MonoBehaviour
    {
        /// <summary>
        /// LayerMask to only detect chess pieces when using raycast
        /// </summary>
        public static LayerMask blackLayer;
        public static LayerMask whiteLayer;
        public LayerMask blackMask;
        public LayerMask whiteMask;

        /// <summary>
        /// Chessboard GameObject consisting of individual tiles
        /// </summary>
        public static GameObject chessboard;
        public GameObject boardObject;

        void Start()
        {
            blackLayer = blackMask;
            whiteLayer = whiteMask;
            chessboard = boardObject;
        }

        /// <summary>
        /// Checks if king is on the left or right side of the piece
        /// If so, check to see if the opposite side has the opponent's queen or rook
        /// Cannot move up or down if true
        /// </summary>
        /// <param name="position"> position of the piece picked up by the player </param>
        /// <param name="colour"> colour of the piece being manipulated </param>
        /// <returns> true if opponent's queen or rook is on the opposite side </returns>
        public static bool RowCheck(Vector3 position, int colour)
        {
            /// <summary> 
            /// Checking Left side of the piece
            /// </summary>
            if (KingHit(position, new Vector3(-1, 0, 0), false, colour)) { return true; }
            
            /// <summary>
            /// Checking right side of the piece
            /// </summary>
            if (KingHit(position, new Vector3(1, 0, 0), false, colour)) { return true; }

            /// <summary>
            /// Piece not blocking check, can move.
            /// </summary>
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
        public static bool ColumnCheck(Vector3 position, int colour)
        {
            /// <summary> 
            /// Checking the top side of the piece
            /// </summary>
            if (KingHit(position, new Vector3(0, 0, 1), false, colour)) { return true; }

            /// <summary> 
            /// Checking bottom side of the piece
            /// </summary>
            if (KingHit(position, new Vector3(0, 0, -1), false, colour)) { return true; }

            /// <summary>
            /// Piece not blocking check, can move.
            /// </summary>
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
        public static bool DiagonalCheckForward(Vector3 position, int colour)
        {
            /// <summary> 
            /// Checking Top-Right side of the piece
            /// </summary>
            if (KingHit(position, new Vector3(1, 0, 1), true, colour)) { return true; }

            /// <summary> 
            /// Checking Bottom-Left side of the piece
            /// </summary>
            if (KingHit(position, new Vector3(-1, 0, -1), true, colour)) { return true; }

            /// <summary>
            /// Piece not blocking check, can move.
            /// </summary>
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
        public static bool DiagonalCheckBackward(Vector3 position, int colour)
        {
            /// <summary> 
            /// Checking Top-Left side of the piece
            /// </summary>
            if (KingHit(position, new Vector3(-1, 0, 1), true, colour)) { return true; }

            /// <summary> 
            /// Checking Bottom-Right side of the piece
            /// </summary>
            if (KingHit(position, new Vector3(1, 0, -1), true, colour)) { return true; }

            /// <summary>
            /// Piece not blocking check, can move.
            /// </summary>
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
        static bool KingHit(Vector3 position, Vector3 direction, bool diagonal, int colour)
        {
            LayerMask layer = whiteLayer;
            if (colour == 0)
            {
                layer = blackLayer;
            }
            RaycastHit hit;
            /// <summary>
            /// Raycast onto chess pieces only - provided in the layer
            /// </summary>
            if (Physics.Raycast(position, direction, out hit, 1f, layer))
            {
                GameObject pieceCollided = hit.collider.gameObject;
                PieceInformation piece = pieceCollided.GetComponent<PieceInformation>();

                /// <summary>
                /// If piece in direction is player's king, check its opposite.
                /// </summary>
                if ((int)piece.type == 4 && (int)piece.colour == colour)
                {
                    return FindOpposite(position, new Vector3(direction.x * -1, direction.y, direction.z * -1), diagonal, colour);
                }
            }
            return false;
        } 

        static bool FindOpposite(Vector3 position, Vector3 direction, bool diagonal, int colour)
        {
            LayerMask layer = whiteLayer;
            if (colour == 0)
            {
                layer = blackLayer;
            }
            RaycastHit hit;
            if (Physics.Raycast(position, direction, out hit, 1f, layer))
            {
                GameObject pieceCollided = hit.collider.gameObject;
                PieceInformation piece = pieceCollided.GetComponent<PieceInformation>();

                /// <summary>
                /// Moving the piece will not result in check if opposite is either
                /// king, pawn, knight or player's piece
                /// </summary>
                if ((int)piece.type == 5 || (int)piece.type == 1 || (int)piece.type == 4 || (int)piece.colour == colour)
                {
                    return false;
                }

                /// <summary>
                /// Rook or queen directly opposite to the king
                /// Will compromise into a check if piece moved
                /// </summary>
                if (!diagonal && ((int)piece.type == 0 || (int)piece.type == 3))
                {
                    return true;
                }

                /// <summary>
                /// bishop or queen directly opposite to the king
                /// Will compromise into a check if piece moved
                /// </summary>
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
        public static bool EnPassant(Vector3 position, Vector3 direction, int colour)
        {
            LayerMask layer = whiteLayer;
            if (colour == 0)
            {
                layer = blackLayer;
            }
            RaycastHit hit;

            if (Physics.Raycast(position, direction, out hit, 0.055f, layer))
            {
                GameObject pieceCollided = hit.collider.gameObject;
                PieceInformation piece = pieceCollided.GetComponent<PieceInformation>();

                /// <summary>
                /// Check if opponent's pawn in the direction provided.
                /// </summary>
                if ((int)piece.type == 5 && (int)piece.colour != colour)
                {
                    /// <summary>
                    /// Check if the pawn is displaced two positions from its original position
                    /// </summary>
                    /// <returns> false if displacement != 2</returns>
                    if (Math.Abs(piece.GetZPosition() - piece.GetOriginalZ()) != 2)
                    {
                        return false;
                    }

                    /// <summary>
                    /// Check if the pawn has only moved once.
                    /// </summary>
                    /// <returns> false if total moves of the pawn is not 1 </returns>
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
        public static bool Castling(Vector3 position, Vector3 direction, int colour)
        {
            LayerMask layer = whiteLayer;
            if (colour == 0)
            {
                layer = blackLayer;
            }
            RaycastHit hit;

            if (Physics.Raycast(position, direction, out hit, 1f, layer))
            {
                GameObject pieceCollided = hit.collider.gameObject;
                PieceInformation piece = pieceCollided.GetComponent<PieceInformation>();

                /// <summary>
                /// Check if player's rook in the direction
                /// </summary>
                if ((int)piece.type == 0 && (int)piece.colour == colour)
                {
                    /// <summary>
                    /// Check if rook has been moved.
                    /// </summary>
                    /// <returns> true if it has not been moved </returns>
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
        /// <param name="tileX"> x position of the chessboard tile where king can move </param>
        /// <param name="tileZ"> z position of the chessboard tile where king can move</param>
        /// <param name="colour"> King's colour </param>
        /// <param name="board"> 2D chessboard layout </param>
        /// <returns> true if king won't be under check in new position </returns>
        public static bool KingMove(int tileX, int tileZ, int colour, GameObject[,] board)
        {
            /// <summary>
            /// Gets the global position of the tile in the game
            /// </summary>
            GameObject tile = chessboard.transform.GetChild(tileZ).gameObject.transform.GetChild(tileX).gameObject;
            Vector3 globalPosition = new Vector3(tile.transform.position.x, tile.transform.position.y + 0.025f, tile.transform.position.z);

            /// <summary>
            /// If under attack by rook or queen in new position, return false
            /// </summary>
            if (CheckByRookQueen(globalPosition, colour))
            {
                return false;
            }

            /// <summary>
            /// If under attack by queen or bishop in new position, return false
            /// </summary>
            if (CheckByQueenBishop(globalPosition, colour))
            {
                return false;
            }

            /// <summary>
            /// If under attack by knight in new position, return false
            /// </summary>
            if (CheckByKnight(tileX, tileZ, colour, board))
            {
                return false;
            }

            /// <summary>
            /// If under attack by pawn in new position, return false
            /// </summary>
            if (CheckByPawn(globalPosition, colour))
            {
                return false;
            }

            /// <summary>
            /// If under attack by opponent's king in new position, return false
            /// </summary>
            if (CheckByKing(globalPosition, colour))
            {
                return false;
            }

            /// <summary>
            /// Valid position for the king to move
            /// </summary>
            return true;
        }

        /// <summary>
        /// Checks if opponent's rook or queen has control over the new position tile
        /// Only checks up/down and left/right 
        /// </summary>
        /// <param name="position"> Position being checked </param>
        /// <param name="colour"> King's colour </param>
        /// <returns> true if king will be under attack by rook or queen in new position </returns>
        static bool CheckByRookQueen(Vector3 position, int colour)
        {
            /// <summary>
            /// Type = 0 for Rook and queen
            /// </summary>
            int type = 0;

            /// <summary>
            /// Check right side for opponent's rook or queen. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(1, 0, 0), colour, type)) {
                return true;
            }

            /// <summary>
            /// Check left side for opponent's rook or queen. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(-1, 0, 0), colour, type))
            {
                return true;
            }

            /// <summary>
            /// Check top side for opponent's rook or queen. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(0, 0, 1), colour, type))
            {
                return true;
            }

            /// <summary>
            /// Check bottom side for opponent's rook or queen. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(0, 0, -1), colour, type))
            {
                return true;
            }

            /// <summary>
            /// King will not be under attack by queen or rook in the new position
            /// </summary>
            return false;
        }

        /// <summary>
        /// Checks if opponent's queen or bishop has control over the new position tile
        /// Only checks diagonally
        /// </summary>
        /// <param name="position"> Position being checked </param>
        /// <param name="colour"> King's colour </param>
        /// <returns> true if king will be under attack by queen or bishop in new position </returns>
        static bool CheckByQueenBishop(Vector3 position, int colour)
        {
            /// <summary>
            /// Type = 1 for Queen and Bishop
            /// </summary>
            int type = 1;

            /// <summary>
            /// Check top-right side for opponent's queen or bishop. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(1, 0, 1), colour, type))
            {
                return true;
            }

            /// <summary>
            /// Check top-left side for opponent's queen or bishop. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(-1, 0, 1), colour, type))
            {
                return true;
            }

            /// <summary>
            /// Check bottom-right side for opponent's queen or bishop. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(1, 0, -1), colour, type))
            {
                return true;
            }

            /// <summary>
            /// Check bottom-left side for opponent's queen or bishop. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(-1, 0, -1), colour, type))
            {
                return true;
            }

            /// <summary>
            /// King will not be under attack by queen or bishop in the new position
            /// </summary>
            return false;
        }

        /// <summary>
        /// Checks if opponent's knight has control over the new position tile
        /// </summary>
        /// <param name="position"> Position being checked </param>
        /// <param name="colour"> King's colour </param>
        /// <param name="board"> 2D chessboard layout </param>
        /// <returns> true if king will be under attack by the knight in new position </returns>
        static bool CheckByKnight(int xPos, int zPos, int colour, GameObject[,] board)
        {
            /// <summary>
            /// Checking for cases: UUL, UUR, DDL, DDR
            /// </summary>
            int xDisplacement = 1;
            int zDisplacement = 2;

            /// <summary>
            /// Case: UUL
            /// </summary>
            if (KnightHelperMethod(xPos - xDisplacement, zPos + zDisplacement, board, colour))
            {
                return true;
            }

            /// <summary>
            /// Case: UUR
            /// </summary>
            if (KnightHelperMethod(xPos + xDisplacement, zPos + zDisplacement, board, colour))
            {
                return true;
            }

            /// <summary>
            /// Case: DDL
            /// </summary>
            if (KnightHelperMethod(xPos - xDisplacement, zPos - zDisplacement, board, colour))
            {
                return true;
            }

            /// <summary>
            /// Case: DDR
            /// </summary>
            if (KnightHelperMethod(xPos + xDisplacement, zPos - zDisplacement, board, colour))
            {
                return true;
            }

            /// <summary>
            /// Checking for cases: LLU, LLD, RRU, RRD
            /// </summary>
            xDisplacement = 2;
            zDisplacement = 1;

            /// <summary>
            /// Case: LLU
            /// </summary>
            if (KnightHelperMethod(xPos - xDisplacement, zPos + zDisplacement, board, colour))
            {
                return true;
            }

            /// <summary>
            /// Case: LLD
            /// </summary>
            if (KnightHelperMethod(xPos - xDisplacement, zPos - zDisplacement, board, colour))
            {
                return true;
            }

            /// <summary>
            /// Case: RRU
            /// </summary>
            if (KnightHelperMethod(xPos + xDisplacement, zPos + zDisplacement, board, colour))
            {
                return true;
            }

            /// <summary>
            /// Case: RRD
            /// </summary>
            if (KnightHelperMethod(xPos + xDisplacement, zPos - zDisplacement, board, colour))
            {
                return true;
            }

            /// <summary>
            /// Opponent's knight does not control the position
            /// </summary>
            return false;
        }

        /// <summary>
        /// Checks if opponent's knight has control over the tile
        /// </summary>
        /// <returns> true if knight found </returns>
        static bool KnightHelperMethod(int newXPosition, int newZPosition, GameObject[,] board, int colour)
        {
            /// <summary>
            /// Check if x and z positions are outside bounds
            /// </summary>
            if (newXPosition > 7 || newXPosition < 0 || newZPosition > 7 || newZPosition < 0)
            {
                return false;
            }

            GameObject piece = board[newZPosition, newXPosition];

            /// <summary>
            /// Check if no piece at location
            /// </summary>
            if (piece == null)
            {
                return false;
            }

            PieceInformation pieceInformation = piece.GetComponent<PieceInformation>();

            /// <summary>
            /// if player's piece, return false
            /// </summary>
            if ((int)pieceInformation.colour == colour)
            {
                return false;
            }

            /// <summary>
            /// If knight, opponent's knight has control over the tile. 
            /// King cannot move to this location.
            /// </summary>
            if ((int)pieceInformation.type == 1)
            {
                return true;
            }

            /// <summary>
            /// Knight not found. King is probable to move to this location
            /// </summary>
            return false;
        }

        /// <summary>
        /// Checks if opponent's pawn has control over the new position tile
        /// Only checks diagonally
        /// </summary>
        /// <param name="position"> Position being checked </param>
        /// <param name="colour"> King's colour </param>
        /// <returns> true if king will be under attack by pawn in new position </returns>
        static bool CheckByPawn(Vector3 position, int colour)
        {
            /// <summary>
            /// Type = 2 for Pawn
            /// </summary>
            int type = 2;

            /// <summary>
            /// If king is white, opponent's pawn can only move down. 
            /// If king is black, opponent's pawn can only move up.
            /// </summary>
            int displacement = 1;
            if (colour == 1) { displacement = -1; }

            /// <summary>
            /// Sets the raycast distance to look only one position away from the tile
            /// </summary>
            float range = 0.055f;

            /// <summary>
            /// Check if pawn can attack right (diagonally). If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(1, 0, displacement), colour, type, range))
            {
                return true;
            }

            /// <summary>
            /// Check if pawn can attack left (diagonally). If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(-1, 0, displacement), colour, type, range))
            {
                return true;
            }

            /// <summary>
            /// King will not be under attack by a pawn in the new position
            /// </summary>
            return false;
        }

        /// <summary>
        /// Checks if opponent's king has control over the new position tile
        /// </summary>
        /// <param name="position"> Position being checked </param>
        /// <param name="colour"> King's colour </param>
        /// <returns> true if king will be under attack by opponent's king in new position </returns>
        static bool CheckByKing(Vector3 position, int colour)
        {
            /// <summary>
            /// Type = 3 for King
            /// </summary>
            int type = 3;

            /// <summary>
            /// Sets the raycast distance to look only one position away from the tile
            /// </summary>
            float range = 0.055f;

            /// <summary>
            /// Check right side for opponent's king. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(1, 0, 0), colour, type, range))
            {
                return true;
            }

            /// <summary>
            /// Check left side for opponent's king. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(-1, 0, 0), colour, type, range))
            {
                return true;
            }

            /// <summary>
            /// Check top side for opponent's king. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(0, 0, 1), colour, type, range))
            {
                return true;
            }

            /// <summary>
            /// Check bottom side for opponent's king. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(0, 0, -1), colour, type, range))
            {
                return true;
            }

            /// <summary>
            /// Check top-right side for opponent's king. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(1, 0, 1), colour, type, range))
            {
                return true;
            }

            /// <summary>
            /// Check top-left side for opponent's king. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(-1, 0, 1), colour, type, range))
            {
                return true;
            }

            /// <summary>
            /// Check bottom-right side for opponent's king. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(1, 0, -1), colour, type, range))
            {
                return true;
            }

            /// <summary>
            /// Check bottom-left side for opponent's king. If not valid move, return true
            /// </summary>
            if (!ValidMove(position, new Vector3(-1, 0, -1), colour, type, range))
            {
                return true;
            }

            /// <summary>
            /// King will not be under attack by the opponent's king in the new position
            /// </summary>
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
        static bool ValidMove(Vector3 position, Vector3 direction, int colour, int type, float range = 1f)
        {
            LayerMask layer = whiteLayer;
            if (colour == 0)
            {
                layer = blackLayer;
            }
            RaycastHit hit;
            if (Physics.Raycast(position, direction, out hit, range, layer))
            {
                GameObject pieceCollided = hit.collider.gameObject;
                PieceInformation piece = pieceCollided.GetComponent<PieceInformation>();

                /// <summary>
                /// If player's piece is controlling the tile, moving into position is plausible. 
                /// Return true
                /// </summary>
                if ((int)piece.colour == colour)
                {
                    return true;
                }

                /// <summary>
                /// Obtains the piece type to check if the raycast piece was what was being checked
                /// </summary>
                int pieceType = (int)piece.type;

                /// <summary>
                /// Checks to see if the opponent's being checked is controlling the tile
                /// Not a valid location for king to move. Return false
                /// </summary>
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

            /// <summary>
            /// Piece being checked does not control the tile.
            /// </summary>
            return true;
        }
    }
}
