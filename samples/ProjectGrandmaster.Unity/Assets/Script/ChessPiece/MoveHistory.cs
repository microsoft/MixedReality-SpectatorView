// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Contributed by USYD Team - Hrithvik (Jacob) Sood, John Tran, Tom Derrick, Aydin Ucan, Aayush Jindal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView.ProjectGrandmaster
{
    /// <summary>
    /// Tracks the piece movements throughout the game
    /// </summary>
    public class MoveHistory
    {
        private static MoveHistory instance;
        private int Count { get; set; }

        public int Index
        {
            get
            {
                return Count - 1;
            }
        }

        private MoveHistory()
        {
            Count = 0;
        }

        public static MoveHistory Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MoveHistory();
                }
                return instance;
            }
        }

        public List<bool> Eliminated { get; } = new List<bool>();

        public List<GameObject> EliminatedObjects { get; } = new List<GameObject>();

        public List<GameObject> PieceMoved { get; } = new List<GameObject>();

        public List<int> PreviousXPosition { get; } = new List<int>();

        public List<int> PreviousZPosition { get; } = new List<int>();

        public List<int> CurrentXPosition { get; } = new List<int>();

        public List<int> CurrentZPosition { get; } = new List<int>();

        public List<List<string>> CheckPath { get; } = new List<List<string>>();

        public List<bool> Check { get; } = new List<bool>();

        public List<bool> PawnPromoted { get; } = new List<bool>();

        /// <summary>
        /// Called when a move is made
        /// </summary>
        /// <param name="pieceEliminated"> True if the move eliminate's the opponent's piece </param>
        /// <param name="eliminatedObject"> Opponent's piece that has been eliminated, otherwise null </param>
        /// <param name="piece"> Piece that was moved </param>
        /// <param name="originalPos"> Original Position of the piece ("x z")</param>
        /// <param name="newPos"> New Position of the Piece ("x z")</param>
        public void Move(bool pieceEliminated, GameObject eliminatedObject, GameObject piece, string originalPos, string newPos)
        {
            Eliminated.Add(pieceEliminated);

            if (pieceEliminated)
            {
                EliminatedObjects.Add(eliminatedObject);
            }

            else
            {
                EliminatedObjects.Add(null);
            }

            PieceMoved.Add(piece);
            PreviousXPosition.Add((int)char.GetNumericValue(originalPos[0]));
            PreviousZPosition.Add((int)char.GetNumericValue(originalPos[2]));
            CurrentXPosition.Add((int)char.GetNumericValue(newPos[0]));
            CurrentZPosition.Add((int)char.GetNumericValue(newPos[2]));

            // by default, pawnPromoted = false
            PawnPromoted.Add(false);
            
            // by default, check = false
            Check.Add(false);

            Count++;
        }

        /// <summary>
        /// New game or 'reset'
        /// </summary>
        public void Clear()
        {
            instance = new MoveHistory();
        }

        /// <summary>
        /// When king is checked, store the path from the piece checking the king, to the king
        /// into CheckPath
        /// </summary>
        /// <param name="path"></param>
        public void AddCheckPath(List<string> path)
        {
            CheckPath.Add(path);
        }

        public List<string> GetCheckPath()
        {
            int index = CheckPath.Count - 1;
            return CheckPath[index];
        }

        public void RemoveLastPath()
        {
            int index = CheckPath.Count - 1;
            CheckPath.RemoveAt(index);
        }

        public ArrayList Undo()
        {
            ArrayList undo = new ArrayList();
            undo.Add(Eliminated[Index]);
            undo.Add(EliminatedObjects[Index]);
            undo.Add(PieceMoved[Index]);
            undo.Add(PreviousXPosition[Index]);
            undo.Add(PreviousZPosition[Index]);
            undo.Add(CurrentXPosition[Index]);
            undo.Add(CurrentZPosition[Index]);
            undo.Add(Check[Index]);
            undo.Add(PawnPromoted[Index]);

            Eliminated.RemoveAt(Index);
            EliminatedObjects.RemoveAt(Index);
            PieceMoved.RemoveAt(Index);
            PreviousXPosition.RemoveAt(Index);
            PreviousZPosition.RemoveAt(Index);
            CurrentXPosition.RemoveAt(Index);
            CurrentZPosition.RemoveAt(Index);
            Check.RemoveAt(Index);
            PawnPromoted.RemoveAt(Index);
            Count--;

            return undo;
        }

        public void KingInCheck()
        {
            Check[Index] = true;
        }

        public void Promoted()
        {
            PawnPromoted[Index] = true;
        }
    }
}